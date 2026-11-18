using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

// Reflection-based test harness for TaskbarPin.exe.
// Loads the shipped binary (no recompilation) and exercises the private surface.
static class Tests
{
    static Assembly asm;
    static Type formType;
    static Type win32Type;
    static int passed, failed;
    static readonly List<string> failures = new List<string>();

    [DllImport("user32.dll")]
    static extern uint GetGuiResources(IntPtr hProcess, uint uiFlags);
    [DllImport("kernel32.dll")]
    static extern IntPtr GetCurrentProcess();
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr FindWindow(string cls, string win);
    [DllImport("user32.dll")]
    static extern bool GetWindowRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")]
    static extern IntPtr GetParent(IntPtr h);
    [DllImport("user32.dll")]
    static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")]
    static extern bool SetProcessDPIAware();
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr FindWindowEx(IntPtr parent, IntPtr after, string cls, string win);

    [StructLayout(LayoutKind.Sequential)]
    struct RECT { public int left, top, right, bottom; }

    static uint UserHandles() { return GetGuiResources(GetCurrentProcess(), 1); }
    static uint GdiHandles() { return GetGuiResources(GetCurrentProcess(), 0); }

    static void Check(string name, bool ok, string detail = "")
    {
        if (ok) { passed++; Console.WriteLine("  PASS  " + name + (detail.Length > 0 ? "   [" + detail + "]" : "")); }
        else { failed++; failures.Add(name + " :: " + detail); Console.WriteLine("  FAIL  " + name + "   [" + detail + "]"); }
    }

    static object Form(object[] args)
    {
        return Activator.CreateInstance(formType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null, args, null);
    }

    static object Call(object o, string name, params object[] a)
    {
        return TestAccess.Call(o, name, a);
    }

    static object Static(string name, params object[] a)
    {
        return TestAccess.Call(formType, name, a);
    }

    static object Field(object o, string name)
    {
        return TestAccess.Get(o, name);
    }

    static void SetField(object o, string name, object v)
    {
        TestAccess.Set(o, name, v);
    }

    static void Quiet(object form)
    {
        TestAccess.Quiet((Form)form);
    }

    static int NonTransparentPixels(Bitmap bmp)
    {
        int n = 0;
        for (int y = 0; y < bmp.Height; y++)
            for (int x = 0; x < bmp.Width; x++)
                if (bmp.GetPixel(x, y).A > 0) n++;
        return n;
    }

    [STAThread]
    static int Main(string[] argv)
    {
        SetProcessDPIAware();
        Point originalCursor = Cursor.Position;
        try { return Run(argv); }
        finally { Cursor.Position = originalCursor; }
    }

    static int Run(string[] argv)
    {
        Console.OutputEncoding = Encoding.UTF8;
        string exe = argv.Length > 0 ? argv[0] : "TaskbarPin.exe";
        exe = Path.GetFullPath(exe);
        asm = Assembly.LoadFrom(exe);
        formType = asm.GetType(TestAccess.FormTypeName, true);
        win32Type = asm.GetType("TaskbarPin.Win32", false);

        Console.WriteLine("== TaskbarPin test harness ==");
        Console.WriteLine("binary: " + exe);
        Console.WriteLine();

        TestEnvironment();
        if (argv.Length > 1 && argv[1] == "attrib") { TestLeakAttribution(); return 0; }
        TestHandleLeak();
        TestDraw();
        TestAutoHideRoundTrip();
        TestStateMachine();
        TestSingleInstanceMutex();

        Console.WriteLine();
        Console.WriteLine("---- " + passed + " passed, " + failed + " failed ----");
        foreach (string f in failures) Console.WriteLine("FAILED: " + f);
        return failed == 0 ? 0 : 1;
    }

    static void TestEnvironment()
    {
        Console.WriteLine("[env]");
        IntPtr tray = FindWindow("Shell_TrayWnd", null);
        Check("Shell_TrayWnd exists", tray != IntPtr.Zero, "hwnd=0x" + tray.ToInt64().ToString("X"));
        RECT r; GetWindowRect(tray, out r);
        Console.WriteLine("  tray rect = " + r.left + "," + r.top + " " + (r.right - r.left) + "x" + (r.bottom - r.top));
        Console.WriteLine("  screen    = " + Screen.PrimaryScreen.Bounds);
        Console.WriteLine("  prog handles: USER=" + UserHandles() + " GDI=" + GdiHandles());
        Console.WriteLine();
    }

    static void Measure(string label, object f, string method, int n, params object[] args)
    {
        MethodInfo mi = formType.GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        for (int i = 0; i < 5; i++) { SetField(f, "_isPinnedByApp", i % 2 == 0); mi.Invoke(f, args); }
        uint u0 = UserHandles(), g0 = GdiHandles();
        for (int i = 0; i < n; i++) { SetField(f, "_isPinnedByApp", i % 2 == 0); mi.Invoke(f, args); }
        uint u1 = UserHandles(), g1 = GdiHandles();
        Console.WriteLine("  " + label.PadRight(34) + " USER +" + ((int)u1 - (int)u0) + "  GDI +" + ((int)g1 - (int)g0) +
                          "   (" + ((double)((int)g1 - (int)g0) / n).ToString("F2") + " gdi/call)");
    }

    static void TestLeakAttribution()
    {
        Console.WriteLine("[leak attribution]");
        object f = Form(null);
        Quiet(f);
        SetField(f, "_isPinnedByApp", true);
        const int N = 100;
        Measure("UpdateTrayIconImage()", f, "UpdateTrayIconImage", N);
        Measure("UpdateVisualState()", f, "UpdateVisualState", N);
        Console.WriteLine("  note: UpdateVisualState updates the tray icon and menu");
        try { ((Form)f).Dispose(); } catch { }
    }

    static void TestHandleLeak()
    {
        Console.WriteLine("[handle leak: UpdateVisualState / UpdateTrayIconImage]");
        object f = Form(null);
        Quiet(f);

        // warm up so one-time allocations (fonts, tooltip, menus) are excluded
        for (int i = 0; i < 5; i++) { SetField(f, "_isPinnedByApp", i % 2 == 0); Call(f, "UpdateVisualState"); }

        uint u0 = UserHandles(), g0 = GdiHandles();
        const int N = 200;
        for (int i = 0; i < N; i++) { SetField(f, "_isPinnedByApp", i % 2 == 0); Call(f, "UpdateVisualState"); }
        uint u1 = UserHandles(), g1 = GdiHandles();

        int dUser = (int)u1 - (int)u0, dGdi = (int)g1 - (int)g0;
        Console.WriteLine("  USER " + u0 + " -> " + u1 + "  (delta " + dUser + " for " + N + " icon rebuilds)");
        Console.WriteLine("  GDI  " + g0 + " -> " + g1 + "  (delta " + dGdi + ")");
        Check("no USER handle leak per tray icon rebuild", dUser <= 2, "delta=" + dUser + " over " + N + " calls");
        Check("no GDI handle leak per tray icon rebuild", dGdi <= 2, "delta=" + dGdi + " over " + N + " calls");

        try { ((Form)f).Dispose(); } catch { }
        Console.WriteLine();
    }

    static void TestDraw()
    {
        Console.WriteLine("[render]");
        MethodInfo draw = asm.GetType("TaskbarPin.PinArtwork", true).GetMethod("DrawBadge",
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        object palette = TestAccess.Call(asm.GetType("TaskbarPin.UiPalette", true), "Create", true, false);
        if (draw == null) { Check("DrawBadge exists", false, "missing"); Console.WriteLine(); return; }

        foreach (int size in new[] { 16, 32 })
        using (Bitmap a = new Bitmap(size, size, PixelFormat.Format32bppArgb))
        using (Bitmap b = new Bitmap(size, size, PixelFormat.Format32bppArgb))
        using (Bitmap c = new Bitmap(size, size, PixelFormat.Format32bppArgb))
        {
            using (Graphics g = Graphics.FromImage(a)) { draw.Invoke(null, new object[] { g, size, true, false, palette }); }
            using (Graphics g = Graphics.FromImage(b)) { draw.Invoke(null, new object[] { g, size, false, false, palette }); }
            using (Graphics g = Graphics.FromImage(c)) { draw.Invoke(null, new object[] { g, size, true, true, palette }); }

            int pa = NonTransparentPixels(a), pb = NonTransparentPixels(b), pc = NonTransparentPixels(c);
            Check("pinned badge draws pixels at " + size, pa > 0, "pixels=" + pa);
            Check("unpinned badge draws pixels at " + size, pb > 0, "pixels=" + pb);
            Check("pinned != unpinned rendering", pa != pb || !SameColors(a, b), "pinned=" + pa + " unpinned=" + pb);

            // hover must be distinguishable
            Check("hover state changes rendering", !SameColors(a, c), "hover-vs-pinned identical");
        }
        Console.WriteLine();
    }

    static bool SameColors(Bitmap x, Bitmap y)
    {
        for (int j = 0; j < x.Height; j++)
            for (int i = 0; i < x.Width; i++)
                if (x.GetPixel(i, j).ToArgb() != y.GetPixel(i, j).ToArgb()) return false;
        return true;
    }

    static void TestAutoHideRoundTrip()
    {
        Console.WriteLine("[taskbar auto-hide]");
        bool before = (bool)Static("GetTaskbarAutoHide");
        Console.WriteLine("  initial auto-hide = " + before);
        try
        {
            Static("SetTaskbarAutoHide", false);
            bool afterOff = (bool)Static("GetTaskbarAutoHide");
            Check("SetTaskbarAutoHide(false) takes effect", afterOff == false, "observed=" + afterOff);

            Static("SetTaskbarAutoHide", true);
            bool afterOn = (bool)Static("GetTaskbarAutoHide");
            Check("SetTaskbarAutoHide(true) takes effect", afterOn == true, "observed=" + afterOn);
        }
        finally
        {
            Static("SetTaskbarAutoHide", before);
            bool restored = (bool)Static("GetTaskbarAutoHide");
            Check("taskbar state restored", restored == before, "restored=" + restored);
        }
        Console.WriteLine();
    }

    static void TestStateMachine()
    {
        Console.WriteLine("[state machine]");
        object f = Form(null);
        Quiet(f);
        try
        {
            bool before = (bool)Static("GetTaskbarAutoHide");
            try
            {
                SetField(f, "_isPinnedByApp", false);
                Call(f, "SetPinnedState", true);
                Check("pin -> _isPinnedByApp true", (bool)Field(f, "_isPinnedByApp"));
                Check("pin -> taskbar no longer auto-hides", (bool)Static("GetTaskbarAutoHide") == false, "");

                SetField(f, "_isPinnedByApp", true);
                Call(f, "SetPinnedState", false);
                Check("unpin -> _isPinnedByApp false", !(bool)Field(f, "_isPinnedByApp"));
                Check("unpin -> waiting for collapse", (bool)Field(f, "_waitingForCollapse"));
                Application.DoEvents();
                Check("unpin -> taskbar auto-hide restored", (bool)Static("GetTaskbarAutoHide") == true, "");
            }
            finally
            {
                Static("SetTaskbarAutoHide", before);
                RECT rr; GetWindowRect(FindWindow("Shell_TrayWnd", null), out rr);
                Console.WriteLine("  taskbar restored to autoHide=" + before + " top=" + rr.top);
            }
        }
        catch (Exception e) { Check("state machine ran", false, e.ToString()); }
        try { ((Form)f).Dispose(); } catch { }
        Console.WriteLine();
    }

    static void TestSingleInstanceMutex()
    {
        Console.WriteLine("[single instance]");
        // A live widget owns this mutex, so "first acquisition" can only be observed with none running.
        bool running = System.Diagnostics.Process.GetProcessesByName("TaskbarPin").Length > 0;
        if (running)
        {
            Console.WriteLine("  SKIP  a TaskbarPin instance is running; mutex ownership is held by it");
            Console.WriteLine();
            return;
        }
        bool created;
        using (System.Threading.Mutex m1 = new System.Threading.Mutex(true, "Local\\TaskbarPin_SingleInstance_Mutex", out created))
        {
            Check("first mutex acquisition succeeds", created, "created=" + created);
            bool created2;
            using (System.Threading.Mutex m2 = new System.Threading.Mutex(true, "Local\\TaskbarPin_SingleInstance_Mutex", out created2))
            {
                Check("second instance sees existing mutex", !created2, "createdNew=" + created2);
            }
        }
        Console.WriteLine();
    }
}
