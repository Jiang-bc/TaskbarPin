using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

static class RepairTests
{
    static Assembly assembly;
    static int passed, failed;

    [DllImport("user32.dll")] static extern uint GetGuiResources(IntPtr process, uint flags);
    [DllImport("kernel32.dll")] static extern IntPtr GetCurrentProcess();
    [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);

    static void Check(string name, bool condition)
    {
        Console.WriteLine((condition ? "PASS " : "FAIL ") + name);
        if (condition) passed++; else failed++;
    }

    static void Run(Action test)
    {
        try { test(); }
        catch (Exception exception) { Check(test.Method.Name + ": " + exception, false); }
    }

    static Form NewForm()
    {
        Form form = (Form)Activator.CreateInstance(assembly.GetType(TestAccess.FormTypeName, true));
        TestAccess.Quiet(form);
        return form;
    }

    static void TestPollingFailure()
    {
        using (Form form = NewForm())
        {
            System.Windows.Forms.Timer timer = (System.Windows.Forms.Timer)TestAccess.Get(form, TestAccess.PollTimer);
            object readAutoHide = TestAccess.Get(form, "_readAutoHide");
            TestAccess.Set(form, "_readAutoHide", new Func<bool?>(delegate { throw new IOException("Injected shell probe failure"); }));
            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug.log");
            int start = File.Exists(logPath) ? File.ReadAllText(logPath).Length : 0;
            int ticks = 0;
            timer.Tick += delegate { ticks++; };
            try
            {
                timer.Start();
                Stopwatch elapsed = Stopwatch.StartNew();
                while (elapsed.ElapsedMilliseconds < 5000)
                {
                    Application.DoEvents();
                    Thread.Sleep(10);
                }
            }
            finally { TestAccess.Set(form, "_readAutoHide", readAutoHide); }
            Check("poll timer continues ticking through injected shell failures", ticks > 20 && timer.Enabled);
            string log = File.Exists(logPath) ? File.ReadAllText(logPath) : "";
            int first = log.IndexOf("[PollTimer]", log.Length > start ? start : 0, StringComparison.Ordinal);
            Check("a recurring poll exception is logged once", first >= 0 && log.IndexOf("[PollTimer]", first + 1, StringComparison.Ordinal) < 0);
            TestAccess.Call(form, "PollTimer_Tick", null, EventArgs.Empty);
            Check("polling is enabled and can recover", timer.Enabled && timer.Interval == 50);
        }
    }

    static void TestLogLimit()
    {
        string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug.log");
        File.WriteAllText(logPath, new string('x', 256 * 1024));
        TestAccess.Call(assembly.GetType("TaskbarPin.CrashLog", true), "Write", "RotationTest", new IOException("test"));
        Check("log rotation keeps the active file bounded", new FileInfo(logPath).Length <= 256 * 1024 && File.ReadAllText(logPath).Contains("[RotationTest]") && File.Exists(logPath + ".1"));
    }

    static void TestUnknownAutoHide()
    {
        using (Form form = NewForm())
        {
            bool? observed = null;
            TestAccess.Set(form, "_readAutoHide", new Func<bool?>(delegate { return observed; }));
            foreach (bool pinned in new[] { false, true })
            {
                TestAccess.Set(form, "_isPinnedByApp", pinned);
                TestAccess.Set(form, "_lastAutoHideProbeAt", DateTime.MinValue);
                TestAccess.Call(form, "PollTimer_Tick", null, EventArgs.Empty);
                Check("unknown shell state preserves pin state " + pinned, (bool)TestAccess.Get(form, "_isPinnedByApp") == pinned);
            }
            observed = true;
            TestAccess.Set(form, "_lastAutoHideProbeAt", DateTime.MinValue);
            TestAccess.Call(form, "PollTimer_Tick", null, EventArgs.Empty);
            Check("known state resynchronizes after shell recovery", !(bool)TestAccess.Get(form, "_isPinnedByApp"));
        }
    }

    static void TestLegacyConfigIgnored()
    {
        string config = Path.Combine(Path.GetDirectoryName(assembly.Location), "config.ini");
        if (File.Exists(config)) throw new InvalidOperationException("Test directory unexpectedly contains config.ini.");
        try
        {
            File.WriteAllText(config, "OffsetX=invalid\r\nPinned=True\r\n");
            using (Form form = NewForm())
            {
                Check("legacy configuration is ignored", File.ReadAllText(config) == "OffsetX=invalid\r\nPinned=True\r\n");
            }
            File.Delete(config);
            using (Form form = NewForm())
                Check("starting the tray host does not create configuration", !File.Exists(config));
        }
        finally { File.Delete(config); }
    }

    static void TestStorage()
    {
        string directory = AppDomain.CurrentDomain.BaseDirectory;
        string portable = Path.Combine(directory, "portable");
        string fallback = Path.Combine(directory, "fallback");
        File.WriteAllText(portable, "fixture");
        Type storage = assembly.GetType("TaskbarPin.AppStorage");
        Check("storage resolves an unwritable portable directory to fallback", storage != null && (string)TestAccess.Call(storage, "ResolveDirectory", portable, fallback) == fallback);
        File.Delete(portable);
        Directory.CreateDirectory(portable);
        Check("storage prefers a writable portable directory", storage != null && (string)TestAccess.Call(storage, "ResolveDirectory", portable, fallback) == portable);
        string log = Path.Combine(portable, "debug.log");
        File.WriteAllText(log, "fixture");
        try
        {
            File.SetAttributes(log, File.GetAttributes(log) | FileAttributes.ReadOnly);
            Check("read-only portable log selects fallback", (string)TestAccess.Call(storage, "ResolveDirectory", portable, fallback) == fallback);
        }
        finally { File.SetAttributes(log, FileAttributes.Normal); File.Delete(log); }
        TestAccess.Call(assembly.GetType("TaskbarPin.CrashLog", true), "Write", "StorageTest", new IOException("test"));
        Check("diagnostic log is written", File.ReadAllText(Path.Combine(directory, "debug.log")).Contains("[StorageTest]"));
        Directory.Delete(portable);
        if (Directory.Exists(fallback)) Directory.Delete(fallback);
    }

    static void TestAppearance()
    {
        using (Form form = NewForm())
        {
            ContextMenuStrip menu = (ContextMenuStrip)TestAccess.Get(form, "_contextMenu");
            NotifyIcon tray = (NotifyIcon)TestAccess.Get(form, TestAccess.TrayIcon);
            float scale = (float)TestAccess.Get(form, "_uiScale");
            Image previousGlyph = menu.Items[2].Image;
            Icon previousIcon = tray.Icon;
            SendMessage(form.Handle, 0x001A, IntPtr.Zero, IntPtr.Zero);
            uint user = GetGuiResources(GetCurrentProcess(), 1), gdi = GetGuiResources(GetCurrentProcess(), 0);
            for (int repeat = 0; repeat < 100; repeat++)
            {
                SendMessage(form.Handle, 0x001A, IntPtr.Zero, IntPtr.Zero);
                TestAccess.Call(form, "UpdateVisualState");
            }
            Check("unchanged settings retain the tray icon and menu artwork", tray.Icon == previousIcon && menu.Items[2].Image == previousGlyph);
            Check("repeated settings messages leave USER and GDI handles stable", user == GetGuiResources(GetCurrentProcess(), 1) && gdi == GetGuiResources(GetCurrentProcess(), 0));
            TestAccess.Set(form, "_readScale", new Func<float>(delegate { return scale == 2f ? 1.5f : 2f; }));
            TestAccess.Call(form, "RefreshAppearance");
            int imageSize = (int)TestAccess.Call(form, "Scale", 18);
            int traySize = (int)TestAccess.Call(form, "Scale", 16);
            Check("a scale change rebuilds menu metrics and tray artwork", scale != (float)TestAccess.Get(form, "_uiScale") && menu.ImageScalingSize.Width == imageSize && menu.Items[2].Image.Width == imageSize && tray.Icon.Width == traySize);

        }
    }

    static void TestAutoStartPath()
    {
        Type formType = assembly.GetType(TestAccess.FormTypeName, true);
        string current = @"C:\Portable Tools\TaskbarPin.exe";
        Check("autostart accepts the quoted current path case-insensitively", (bool)TestAccess.Call(formType, "MatchesAutoStartPath", "\"c:\\portable tools\\taskbarpin.exe\"", current));
        Check("autostart marks a moved executable or a CLI command as stale", !(bool)TestAccess.Call(formType, "MatchesAutoStartPath", @"C:\Old\TaskbarPin.exe", current) && !(bool)TestAccess.Call(formType, "MatchesAutoStartPath", "\"" + current + "\" --unpin", current));
        string keyName = "Software\\TaskbarPinRepairTests-" + Guid.NewGuid().ToString("N");
        try
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(keyName, RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryOptions.Volatile))
            {
                key.SetValue("TaskbarPin", @"C:\Old\TaskbarPin.exe");
                bool enabled = (bool)TestAccess.Call(formType, "ToggleAutoStart", key, current);
                Check("toggling a stale autostart entry re-registers the current executable", enabled && Equals(key.GetValue("TaskbarPin"), "\"" + current + "\""));
                Check("toggling the current autostart entry removes it", !(bool)TestAccess.Call(formType, "ToggleAutoStart", key, current) && key.GetValue("TaskbarPin") == null);
            }
        }
        finally { Registry.CurrentUser.DeleteSubKey(keyName, false); }
    }

    static void TestCollapseSafety()
    {
        using (Form form = NewForm())
        {
            TestAccess.Set(form, "_isPinnedByApp", false);
            TestAccess.Set(form, "_waitingForCollapse", true);
            TestAccess.Set(form, "_collapseStage", 1);
            TestAccess.Set(form, "_unpinRequestedAt", DateTime.UtcNow.AddSeconds(-4));
            TestAccess.Set(form, "_readAutoHide", new Func<bool?>(delegate { throw new IOException("geometry unavailable"); }));
            TestAccess.Set(form, "_lastAutoHideProbeAt", DateTime.MinValue);
            TestAccess.Call(form, "PollTimer_Tick", null, EventArgs.Empty);
            Check("collapse times out independently of taskbar geometry", !(bool)TestAccess.Get(form, "_waitingForCollapse"));
        }

        Type formType = assembly.GetType(TestAccess.FormTypeName, true);
        IntPtr tray = new IntPtr(1), target = new IntPtr(2);
        int pulses = 0;
        Action<IntPtr> focus = delegate { pulses++; };
        TestAccess.Call(formType, "PulseTrayFocus", tray, target, focus, new Func<IntPtr, bool>(delegate(IntPtr window) { return window == target; }));
        Check("full-screen foreground skips the focus pulse", pulses == 0);
        TestAccess.Call(formType, "PulseTrayFocus", tray, target, focus, new Func<IntPtr, bool>(delegate { return false; }));
        Check("windowed foreground still receives the normal focus pulse", pulses == 2);
    }

    static int RunCommand(string arguments)
    {
        var start = new ProcessStartInfo(assembly.Location, arguments) { UseShellExecute = false, WindowStyle = ProcessWindowStyle.Hidden };
        using (var process = Process.Start(start))
        {
            if (process.WaitForExit(3000)) return process.ExitCode;
            process.Kill();
            process.WaitForExit();
            return -1;
        }
    }

    static void TestCommandLine()
    {
        Check("invalid command-line argument exits without starting a widget", RunCommand("--bogus") == 2);
        Type program = assembly.GetType("TaskbarPin.Program", true);
        bool? autoHide = null;
        int collapses = 0;
        Action<bool> setAutoHide = delegate(bool enabled) { autoHide = enabled; };
        Action collapse = delegate { collapses++; };
        Check("--UnPiN enables auto-hide and requests native collapse", Equals(0, TestAccess.Call(program, "RunCommandLine", new[] { "--UnPiN" }, setAutoHide, collapse)) && autoHide == true && collapses == 1);
        Check("extra arguments leave taskbar state and focus untouched", Equals(2, TestAccess.Call(program, "RunCommandLine", new[] { "--pin", "bad" }, setAutoHide, collapse)) && autoHide == true && collapses == 1);
        Type formType = assembly.GetType(TestAccess.FormTypeName, true);
        object[] state = { false };
        if (!(bool)TestAccess.Call(formType, "TryGetTaskbarAutoHide", state))
            throw new InvalidOperationException("Cannot snapshot taskbar auto-hide state.");
        bool previous = (bool)state[0];
        try
        {
            Check("--PIN disables taskbar auto-hide", RunCommand("--PIN") == 0 && !(bool)TestAccess.Call(formType, "GetTaskbarAutoHide"));
            Check("--unpin enables taskbar auto-hide", RunCommand("--unpin") == 0 && (bool)TestAccess.Call(formType, "GetTaskbarAutoHide"));
        }
        finally { TestAccess.Call(formType, "SetTaskbarAutoHide", previous); }
        Check("CLI test restores the original taskbar state", (bool)TestAccess.Call(formType, "TryGetTaskbarAutoHide", state) && (bool)state[0] == previous);
    }

    [STAThread]
    static int Main(string[] args)
    {
        assembly = Assembly.LoadFrom(Path.GetFullPath(args[0]));
        TestAccess.Call(assembly.GetType("TaskbarPin.Win32", true), "SetProcessDPIAware");
        Application.EnableVisualStyles();
        Point originalCursor = Cursor.Position;
        try
        {
            Run(TestLogLimit);
            Run(TestPollingFailure);
            Run(TestUnknownAutoHide);
            Run(TestLegacyConfigIgnored);
            Run(TestStorage);
            Run(TestAppearance);
            Run(TestAutoStartPath);
            Run(TestCollapseSafety);
            Run(TestCommandLine);
            Console.WriteLine(passed + " passed, " + failed + " failed");
            return failed == 0 ? 0 : 1;
        }
        finally { if (Cursor.Position != originalCursor) Cursor.Position = originalCursor; }
    }
}
