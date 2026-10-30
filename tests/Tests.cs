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

    static MethodInfo FindMethod(string name, int argCount)
    {
        // GetMethod(name) throws AmbiguousMatchException for overloads such as RenderLayeredBitmap().
        BindingFlags f = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        foreach (MethodInfo m in formType.GetMethods(f))
        {
            if (m.Name == name && m.GetParameters().Length == argCount) return m;
        }
        throw new MissingMethodException("PinWidgetForm." + name + "/" + argCount);
    }

    static object Call(object o, string name, params object[] a)
    {
        return FindMethod(name, a == null ? 0 : a.Length).Invoke(o, a);
    }

    static object Static(string name, params object[] a)
    {
        return FindMethod(name, a == null ? 0 : a.Length).Invoke(null, a);
    }

    static object Field(object o, string name)
    {
        FieldInfo fi = formType.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (fi == null) throw new MissingFieldException("PinWidgetForm." + name);
        return fi.GetValue(o);
    }

    static void SetField(object o, string name, object v)
    {
        FieldInfo fi = formType.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (fi == null) throw new MissingFieldException("PinWidgetForm." + name);
        fi.SetValue(o, v);
    }

    static void Quiet(object form)
    {
        // keep the test invisible: no tray icon, no polling
        try { ((NotifyIcon)Field(form, "_trayIcon")).Visible = false; } catch { }
        try { ((System.Windows.Forms.Timer)Field(form, "_pollTimer")).Stop(); } catch { }
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
        Console.OutputEncoding = Encoding.UTF8;
        string exe = argv.Length > 0 ? argv[0] : "TaskbarPin.exe";
        exe = Path.GetFullPath(exe);
        asm = Assembly.LoadFrom(exe);
        formType = asm.GetType("TaskbarPin.PinWidgetForm", true);
        win32Type = asm.GetType("TaskbarPin.Win32", false);

        Console.WriteLine("== TaskbarPin test harness ==");
        Console.WriteLine("binary: " + exe);
        Console.WriteLine();

        TestEnvironment();
        if (argv.Length > 1 && argv[1] == "attrib") { TestLeakAttribution(); return 0; }
        TestConfig();
        TestHandleLeak();
        TestDraw();
        TestAutoHideRoundTrip();
        TestPositioning();
        TestOffsetPipeline();
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
        Measure("RenderLayeredBitmap(100,100)", f, "RenderLayeredBitmap", N, 100, 100);
        Measure("UpdateVisualState()", f, "UpdateVisualState", N);
        Console.WriteLine("  note: UpdateVisualState = UpdateTrayIconImage + RenderLayeredBitmap + text updates");
        try { ((Form)f).Dispose(); } catch { }
    }

    static void TestConfig()
    {
        Console.WriteLine("[config]");
        string dir = Path.Combine(Path.GetTempPath(), "taskbarpin_tests");
        Directory.CreateDirectory(dir);
        string cfg = Path.Combine(dir, "config.ini");

        object f = Form(null);
        Quiet(f);
        SetField(f, "_configPath", cfg);

        // 1. no file -> default retained
        if (File.Exists(cfg)) File.Delete(cfg);
        SetField(f, "_offsetFromTrayNotifyLeft", -48);
        Call(f, "LoadConfig");
        Check("missing config keeps default offset", (int)Field(f, "_offsetFromTrayNotifyLeft") == -48,
            "offset=" + Field(f, "_offsetFromTrayNotifyLeft"));

        // 2. valid value
        File.WriteAllText(cfg, "OffsetX=-72\r\n");
        Call(f, "LoadConfig");
        Check("parses OffsetX=-72", (int)Field(f, "_offsetFromTrayNotifyLeft") == -72,
            "offset=" + Field(f, "_offsetFromTrayNotifyLeft"));

        // 3. whitespace + case insensitive key
        File.WriteAllText(cfg, "  offsetx =  -35  \r\n");
        Call(f, "LoadConfig");
        Check("parses 'offsetx = -35' (trim/case)",
            (int)Field(f, "_offsetFromTrayNotifyLeft") == -35, "offset=" + Field(f, "_offsetFromTrayNotifyLeft"));

        // 4. garbage -> no throw, value untouched
        File.WriteAllText(cfg, "OffsetX=abc\r\n");
        Call(f, "LoadConfig");
        Check("garbage value does not throw / mutate", (int)Field(f, "_offsetFromTrayNotifyLeft") == -35,
            "offset=" + Field(f, "_offsetFromTrayNotifyLeft"));

        // 5. binary garbage
        File.WriteAllBytes(cfg, new byte[] { 0x00, 0xFF, 0x13, 0x0A, 0x4F, 0x00 });
        bool threw = false;
        try { Call(f, "LoadConfig"); } catch (TargetInvocationException e) { threw = !(e.InnerException is IOException); }
        Check("binary config does not crash", !threw, "threw=" + threw);

        // 6. round trip
        File.Delete(cfg);
        SetField(f, "_offsetFromTrayNotifyLeft", -91);
        Call(f, "SaveConfig");
        string text = File.Exists(cfg) ? File.ReadAllText(cfg) : "<missing>";
        SetField(f, "_offsetFromTrayNotifyLeft", 0);
        Call(f, "LoadConfig");
        Check("save/load round trip -91", (int)Field(f, "_offsetFromTrayNotifyLeft") == -91,
            "offset=" + Field(f, "_offsetFromTrayNotifyLeft") + " file=" + text.Replace("\r\n", "|"));

        // 7. pin state is no longer persisted: ABM_GETSTATE is the single source of truth
        SetField(f, "_isPinnedByApp", true);
        Call(f, "SaveConfig");
        string t2 = File.ReadAllText(cfg);
        Check("config does not persist a stale pin state", !t2.Contains("Pinned="), t2.Replace("\r\n", "|"));

        // 8. hand-edited / legacy config is tolerated
        SetField(f, "_isPinnedByApp", false);
        SetField(f, "_offsetFromTrayNotifyLeft", 0);
        File.WriteAllText(cfg, "offsetx = -35\r\nPinned=True\r\n");
        Call(f, "LoadConfig");
        Check("hand-edited 'offsetx = -35' is read", (int)Field(f, "_offsetFromTrayNotifyLeft") == -35,
            "offset=" + Field(f, "_offsetFromTrayNotifyLeft"));
        Check("stale Pinned line no longer overrides live state", (bool)Field(f, "_isPinnedByApp") == false,
            "_isPinnedByApp=" + Field(f, "_isPinnedByApp"));

        // 8. config path unwritable -> SaveConfig swallows, no crash
        string bad = Path.Combine(dir, "no_such_dir", "config.ini");
        SetField(f, "_configPath", bad);
        bool saveThrew = false;
        try { Call(f, "SaveConfig"); } catch (TargetInvocationException) { saveThrew = true; }
        Check("save into missing directory is swallowed silently", !saveThrew, "threw=" + saveThrew);

        try { ((Form)f).Dispose(); } catch { }
        Directory.Delete(dir, true);
        Console.WriteLine();
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
        MethodInfo draw = formType.GetMethod("DrawPinBadge",
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        if (draw == null) { Check("DrawPinBadge exists", false, "missing"); Console.WriteLine(); return; }

        using (Bitmap a = new Bitmap(38, 38, PixelFormat.Format32bppArgb))
        using (Bitmap b = new Bitmap(38, 38, PixelFormat.Format32bppArgb))
        using (Bitmap c = new Bitmap(38, 38, PixelFormat.Format32bppArgb))
        {
            using (Graphics g = Graphics.FromImage(a)) { g.Clear(Color.Transparent); draw.Invoke(null, new object[] { g, 38, true, false }); }
            using (Graphics g = Graphics.FromImage(b)) { g.Clear(Color.Transparent); draw.Invoke(null, new object[] { g, 38, false, false }); }
            using (Graphics g = Graphics.FromImage(c)) { g.Clear(Color.Transparent); draw.Invoke(null, new object[] { g, 38, true, true }); }

            int pa = NonTransparentPixels(a), pb = NonTransparentPixels(b), pc = NonTransparentPixels(c);
            Check("pinned badge draws pixels", pa > 200, "pixels=" + pa);
            Check("unpinned badge draws pixels", pb > 200, "pixels=" + pb);
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

    static void TestPositioning()
    {
        Console.WriteLine("[positioning]");
        object f = Form(null);
        Quiet(f);
        try
        {
            SetField(f, "_isPinnedByApp", true);
            Call(f, "UpdateWidgetPositionAndVisibility", true);
            Application.DoEvents();

            Form form = (Form)f;
            RECT wr; GetWindowRect(form.Handle, out wr);
            IntPtr tray = FindWindow("Shell_TrayWnd", null);
            RECT rt; GetWindowRect(tray, out rt);

            // _lastRenderedX/Y are parent-client coordinates (the widget is a child of the taskbar),
            // so the expected on-screen position is tray origin + rendered offset.
            int expectedX = rt.left + (int)Field(f, "_lastRenderedX");
            int expectedY = rt.top + (int)Field(f, "_lastRenderedY");
            Console.WriteLine("  expected(screen) = " + expectedX + "," + expectedY);
            Console.WriteLine("  actual window    = " + wr.left + "," + wr.top + " " + (wr.right - wr.left) + "x" + (wr.bottom - wr.top));
            Console.WriteLine("  form.Location    = " + form.Location + " (parent=" +
                (GetParent(form.Handle) == tray ? "taskbar" : "other") + ")");

            Check("window left matches rendered x", Math.Abs(wr.left - expectedX) <= 2, "delta=" + (wr.left - expectedX));
            Check("window top matches rendered y", Math.Abs(wr.top - expectedY) <= 2, "delta=" + (wr.top - expectedY));
            Check("widget vertically inside taskbar",
                wr.top >= rt.top && wr.bottom <= rt.bottom + 2, "widget " + wr.top + "-" + wr.bottom + " vs tray " + rt.top + "-" + rt.bottom);

            RECT rn; IntPtr notify = FindWindowEx(tray, IntPtr.Zero, "TrayNotifyWnd", null);
            if (notify != IntPtr.Zero && GetWindowRect(notify, out rn))
                Console.WriteLine("  TrayNotifyWnd    = " + rn.left + "," + rn.top);

            // widget must not be covered by / overlap the clock area
            form.Hide();
        }
        catch (Exception e) { Check("positioning ran", false, e.ToString()); }
        try { ((Form)f).Dispose(); } catch { }
        Console.WriteLine();
    }

    static void TestOffsetPipeline()
    {
        Console.WriteLine("[offset / clamp / persistence]");
        object f = Form(null);
        Quiet(f);
        try
        {
            SetField(f, "_isPinnedByApp", true);
            Form form = (Form)f;
            IntPtr tray = FindWindow("Shell_TrayWnd", null);
            int screenW = Screen.PrimaryScreen.Bounds.Width;
            int size = (int)Field(f, "_widgetSize");
            int maxLeft = screenW - size - 12;

            SetField(f, "_offsetFromTrayNotifyLeft", -9999);
            Call(f, "UpdateWidgetPositionAndVisibility", true);
            RECT l; GetWindowRect(form.Handle, out l);
            Check("huge negative offset clamps to the left margin", Math.Abs(l.left - 12) <= 2, "left=" + l.left);

            SetField(f, "_offsetFromTrayNotifyLeft", 9999);
            Call(f, "UpdateWidgetPositionAndVisibility", true);
            RECT r; GetWindowRect(form.Handle, out r);
            Check("huge positive offset clamps to the right margin", Math.Abs(r.left - maxLeft) <= 2,
                "left=" + r.left + " expected=" + maxLeft);

            SetField(f, "_offsetFromTrayNotifyLeft", -48);
            Call(f, "UpdateWidgetPositionAndVisibility", true);
            RECT n; GetWindowRect(form.Handle, out n);
            int anchor = screenW - 220;
            IntPtr notify = FindWindowEx(tray, IntPtr.Zero, "TrayNotifyWnd", null);
            RECT rn;
            if (notify != IntPtr.Zero && GetWindowRect(notify, out rn) && rn.left > 100) anchor = rn.left;
            int expected = Math.Max(12, Math.Min(maxLeft, anchor - 48));
            Check("default offset renders at anchor - 48", Math.Abs(n.left - expected) <= 2,
                "left=" + n.left + " expected=" + expected + " anchor=" + anchor);

            string cfg = Path.Combine(Path.GetTempPath(), "tbp_offset_test.ini");
            if (File.Exists(cfg)) File.Delete(cfg);
            SetField(f, "_configPath", cfg);
            SetField(f, "_offsetFromTrayNotifyLeft", -120);
            Call(f, "SaveConfig");
            string text = File.Exists(cfg) ? File.ReadAllText(cfg) : "<missing>";
            Check("dragged offset is persisted", text.Contains("OffsetX=-120"), text.Replace("\r\n", "|"));
            Check("pin state is not persisted", !text.Contains("Pinned="), text.Replace("\r\n", "|"));

            RECT before, after;
            GetWindowRect(form.Handle, out before);
            Call(f, "RenderLayeredBitmap");
            GetWindowRect(form.Handle, out after);
            Check("hover re-render does not move the widget",
                before.left == after.left && before.top == after.top,
                before.left + "," + before.top + " -> " + after.left + "," + after.top);

            File.Delete(cfg);
        }
        finally
        {
            ((Form)f).Dispose();
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
