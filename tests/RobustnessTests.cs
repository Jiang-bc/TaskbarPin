using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

static class RobustnessTests
{
    const BindingFlags Members = TestAccess.Members;
    static Assembly assembly;
    static Type formType;
    static int passed, failed;
    [DllImport("user32.dll")] static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] static extern IntPtr GetAncestor(IntPtr handle, uint flags);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern IntPtr FindWindow(string className, string title);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern uint RegisterWindowMessage(string message);
    [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr handle, uint message, IntPtr wParam, IntPtr lParam);

    static void Check(string name, bool condition)
    {
        Console.WriteLine((condition ? "PASS " : "FAIL ") + name);
        if (condition) passed++; else failed++;
    }
    static object Field(object instance, string name) { return TestAccess.Get(instance, name); }
    static void Set(object instance, string name, object value) { TestAccess.Set(instance, name, value); }
    static object Call(object instance, string name, params object[] arguments)
    {
        return TestAccess.Call(instance, name, arguments);
    }
    static Form NewForm()
    {
        Form form = (Form)Activator.CreateInstance(formType);
        TestAccess.Quiet(form);
        return form;
    }
    static object Geometry(Rectangle monitor, Rectangle tray)
    {
        Type type = assembly.GetType("TaskbarPin.TaskbarGeometry");
        return type == null ? null : Activator.CreateInstance(type, Members, null, new object[] { monitor, tray }, null);
    }
    static void TestGeometry()
    {
        Rectangle monitor = new Rectangle(0, 0, 1920, 1080);
        Rectangle[] trays = { new Rectangle(0, 1040, 1920, 40), new Rectangle(0, 0, 1920, 40), new Rectangle(0, 0, 40, 1080), new Rectangle(1880, 0, 40, 1080) };
        Point[] cursors = { new Point(400, 1060), new Point(400, 20), new Point(20, 400), new Point(1900, 400) };
        Point[] exits = { new Point(400, 1025), new Point(400, 55), new Point(55, 400), new Point(1865, 400) };
        for (int edgeIndex = 0; edgeIndex < trays.Length; edgeIndex++)
        {
            object geometry = Geometry(monitor, trays[edgeIndex]);
            if (geometry == null) { Check("collapse exits the correct edge " + edgeIndex, false); continue; }
            object[] arguments = { cursors[edgeIndex], Point.Empty, Point.Empty };
            bool expanded = (bool)Call(geometry, "GetCollapsePoints", arguments);
            Check("collapse exits the correct edge " + edgeIndex, expanded && (Point)arguments[2] == exits[edgeIndex] && trays[edgeIndex].Contains((Point)arguments[1]));
            object[] outside = { new Point(-500, -100), Point.Empty, Point.Empty };
            Call(geometry, "GetCollapsePoints", outside);
            Check("collapse restores a cursor on another monitor " + edgeIndex, (Point)outside[2] == new Point(-500, -100) && trays[edgeIndex].Contains((Point)outside[1]));
        }
        Rectangle[] hidden = { new Rectangle(0, 1078, 1920, 40), new Rectangle(0, -38, 1920, 40), new Rectangle(-38, 0, 40, 1080), new Rectangle(1918, 0, 40, 1080) };
        for (int edgeIndex = 0; edgeIndex < hidden.Length; edgeIndex++)
        {
            object geometry = Geometry(monitor, hidden[edgeIndex]);
            if (geometry == null) { Check("collapsed taskbar is hidden " + edgeIndex, false); continue; }
            Check("collapsed taskbar is hidden " + edgeIndex, !(bool)geometry.GetType().GetProperty("IsShown", Members).GetValue(geometry, null));
            object[] arguments = { new Point(400, 400), Point.Empty, Point.Empty };
            Check("collapsed taskbar is not touched again " + edgeIndex, !(bool)Call(geometry, "GetCollapsePoints", arguments));
        }
        Type geometryType = assembly.GetType("TaskbarPin.TaskbarGeometry");
        MethodInfo create = geometryType == null ? null : geometryType.GetMethod("Create", Members);
        if (create == null) { Check("taskbar ownership uses docked bounds on adjoining monitors", false); return; }
        Rectangle[] monitors = { monitor, new Rectangle(0, 1080, 1920, 1080), new Rectangle(1920, 0, 1920, 1080), new Rectangle(-1920, 0, 1920, 1080), new Rectangle(0, -1080, 1920, 1080) };
        for (int edgeIndex = 0; edgeIndex < hidden.Length; edgeIndex++)
        {
            object geometry = create.Invoke(null, new object[] { hidden[edgeIndex], trays[edgeIndex], monitors });
            Check("animation does not move taskbar ownership to an adjoining monitor " + edgeIndex, (Rectangle)geometryType.GetProperty("MonitorBounds", Members).GetValue(geometry, null) == monitor && !(bool)geometryType.GetProperty("IsShown", Members).GetValue(geometry, null));
        }
    }
    static void TestLifecycle()
    {
        Type contextType = assembly.GetType("TaskbarPin.PinApplicationContext", true);
        using (ApplicationContext context = (ApplicationContext)Activator.CreateInstance(contextType, true))
        using (System.Windows.Forms.Timer checkTimer = new System.Windows.Forms.Timer())
        {
            Form host = (Form)Field(context, "_widget");
            TestAccess.Quiet(host);
            host.Show();
            Check("message host stays invisible when shown", !host.Visible);
            Check("message host is not attached to the taskbar", GetAncestor(host.Handle, 1) != FindWindow("Shell_TrayWnd", null));
            IntPtr original = host.Handle;
            Set(host, "_readAutoHide", new Func<bool?>(delegate { return true; }));
            Set(host, "_isPinnedByApp", true);
            Set(host, "_trayHwnd", new IntPtr(1));
            TestAccess.Set(formType, "_positionTrayHwnd", new IntPtr(2));
            TestAccess.Set(formType, "_dockedTaskbarBounds", new Rectangle(1, 2, 3, 4));
            SendMessage(original, RegisterWindowMessage("TaskbarCreated"), IntPtr.Zero, IntPtr.Zero);
            Check("taskbar recreation preserves the message host", host.IsHandleCreated && host.Handle == original && !host.Visible);
            Check("taskbar recreation restores the tray icon", ((NotifyIcon)Field(host, TestAccess.TrayIcon)).Visible);
            Check("taskbar recreation resets shell caches and syncs state", (IntPtr)Field(host, "_trayHwnd") == IntPtr.Zero &&
                (IntPtr)TestAccess.Get(formType, "_positionTrayHwnd") == IntPtr.Zero &&
                (Rectangle)TestAccess.Get(formType, "_dockedTaskbarBounds") == Rectangle.Empty && !(bool)Field(host, "_isPinnedByApp"));
            using (Form foreground = new Form())
            {
                foreground.Show();
                Application.DoEvents();
                Check("own window is excluded from foreground tracking", !(bool)TestAccess.Call(formType, "IsForeignAppWindow", foreground.Handle));
                foreground.Close();
            }
            bool closed = false;
            checkTimer.Interval = 50;
            checkTimer.Tick += delegate
            {
                checkTimer.Stop();
                closed = true;
                host.Close();
            };
            checkTimer.Start();
            Application.Run(context);
            Check("message loop exits after an intentional close", closed);
        }
    }
    static void TestTrayClicks()
    {
        object[] state = { false };
        if (!(bool)TestAccess.Call(formType, "TryGetTaskbarAutoHide", state))
            throw new InvalidOperationException("Cannot snapshot taskbar auto-hide state.");
        bool previous = (bool)state[0];
        try
        {
            using (Form form = NewForm())
            {
                NotifyIcon tray = (NotifyIcon)Field(form, TestAccess.TrayIcon);
                bool pinned = (bool)Field(form, "_isPinnedByApp");
                MouseEventArgs click = new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 0);
                TestAccess.Call(tray, "OnMouseClick", click);
                Check("tray click toggles the pin state", (bool)Field(form, "_isPinnedByApp") != pinned);
                TestAccess.Call(tray, "OnMouseClick", click);
                Check("second tray click is debounced", (bool)Field(form, "_isPinnedByApp") != pinned);
            }
        }
        finally { TestAccess.Call(formType, "SetTaskbarAutoHide", previous); }
    }
    static void TestMenuExit()
    {
        object[] state = { false };
        if (!(bool)TestAccess.Call(formType, "TryGetTaskbarAutoHide", state))
            throw new InvalidOperationException("Cannot snapshot taskbar auto-hide state.");
        bool previous = (bool)state[0];
        try
        {
            using (ApplicationContext context = (ApplicationContext)Activator.CreateInstance(assembly.GetType("TaskbarPin.PinApplicationContext", true), true))
            using (System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer())
            {
                Form host = (Form)Field(context, "_widget");
                TestAccess.Quiet(host);
                TestAccess.Call(formType, "SetTaskbarAutoHide", false);
                Set(host, "_isPinnedByApp", true);
                bool invoked = false;
                timer.Interval = 50;
                timer.Tick += delegate
                {
                    timer.Stop();
                    invoked = true;
                    Call(host, "OnMenuExit", null, EventArgs.Empty);
                };
                timer.Start();
                Application.Run(context);
                Check("menu exit restores auto-hide and ends the message loop", invoked && host.IsDisposed && (bool)TestAccess.Call(formType, "GetTaskbarAutoHide"));
            }
        }
        finally { TestAccess.Call(formType, "SetTaskbarAutoHide", previous); }
    }
    [STAThread]
    static int Main(string[] arguments)
    {
        SetProcessDPIAware();
        Application.EnableVisualStyles();
        assembly = Assembly.LoadFrom(Path.GetFullPath(arguments[0]));
        formType = assembly.GetType(TestAccess.FormTypeName, true);
        Point originalCursor = Cursor.Position;
        try
        {
            TestGeometry();
            TestLifecycle();
            TestTrayClicks();
            TestMenuExit();
            Console.WriteLine(passed + " passed, " + failed + " failed");
            return failed == 0 ? 0 : 1;
        }
        finally { Cursor.Position = originalCursor; }
    }
}
