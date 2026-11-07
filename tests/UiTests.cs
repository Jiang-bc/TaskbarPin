using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

// Exercises the shipped binary. Never writes registry values or changes Windows theme/taskbar state.
static class UiTests
{
    static Assembly assembly;
    static Type formType;
    static int passed, failed;
    const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)] static extern uint ExtractIconEx(string file, int index, IntPtr[] large, IntPtr[] small, uint count);
    [DllImport("user32.dll")] static extern uint GetGuiResources(IntPtr process, uint flags);
    [DllImport("kernel32.dll")] static extern IntPtr GetCurrentProcess();
    [DllImport("user32.dll")] static extern bool SetProcessDPIAware();

    static void Check(string name, bool condition)
    {
        Console.WriteLine((condition ? "PASS " : "FAIL ") + name);
        if (condition) passed++; else failed++;
    }
    static object Field(object instance, string name)
    {
        FieldInfo field = instance.GetType().GetField(name, Members);
        return field == null ? null : field.GetValue(instance);
    }
    static void Set(object instance, string name, object value) { instance.GetType().GetField(name, Members).SetValue(instance, value); }
    static object Call(object instance, string name, params object[] args)
    {
        return instance.GetType().GetMethod(name, Members).Invoke(instance, args);
    }
    static Form NewForm()
    {
        Form form = (Form)Activator.CreateInstance(formType, true);
        ((Timer)Field(form, "_pollTimer")).Stop();
        ((NotifyIcon)Field(form, "_trayIcon")).Visible = false;
        form.Hide();
        return form;
    }
    static void TestMenu()
    {
        using (Form form = NewForm())
        {
            ContextMenuStrip menu = (ContextMenuStrip)Field(form, "_contextMenu");
            ToolStripItem status = Field(form, "_menuStatus") as ToolStripItem;
            Check("menu exposes a persistent status row", status != null && !status.Enabled);
            Set(form, "_isPinnedByApp", true);
            Call(form, "UpdateVisualState");
            string pinned = status == null ? "" : status.Text;
            Set(form, "_isPinnedByApp", false);
            Call(form, "UpdateVisualState");
            Check("status distinguishes pinned and automatic hiding", status != null && pinned != status.Text && pinned.Length > 0 && status.Text.Length > 0);
            int actions = 0, illustrated = 0;
            foreach (ToolStripItem item in menu.Items)
                if (item is ToolStripMenuItem && item.Enabled) { actions++; if (item.Image != null) illustrated++; }
            Check("all four commands have glyphs", actions == 4 && illustrated == 4);
            Check("menu has a single shared icon gutter", menu.ShowImageMargin && !menu.ShowCheckMargin);
            foreach (ToolStripItem item in menu.Items)
                if (item is ToolStripMenuItem && item.Enabled)
                    Check("command height allows native text padding", item.Height >= menu.Font.Height + 10);
        }
    }
    static object Palette(bool light, bool contrast)
    {
        Type type = assembly.GetType("TaskbarPin.UiPalette");
        return type == null ? null : type.GetMethod("Create", Members).Invoke(null, new object[] { light, contrast });
    }
    static void TestThemes()
    {
        Type type = assembly.GetType("TaskbarPin.UiPalette");
        Check("theme palette supports deterministic rendering without global changes", type != null);
        if (type == null) return;
        object light = Palette(true, false), dark = Palette(false, false), hc = Palette(false, true);
        Check("light and dark menus have distinct backgrounds", ((Color)Field(light, "Background")).ToArgb() != ((Color)Field(dark, "Background")).ToArgb());
        Check("high contrast background uses system colors", ((Color)Field(hc, "Background")).ToArgb() == SystemColors.Menu.ToArgb());
        Check("high contrast text uses system colors", ((Color)Field(hc, "Foreground")).ToArgb() == SystemColors.MenuText.ToArgb());
        foreach (object palette in new object[] { light, dark })
        {
            Check("menu text contrast is at least 4.5:1", Contrast((Color)Field(palette, "Foreground"), (Color)Field(palette, "Background")) >= 4.5);
            Check("status text contrast is at least 4.5:1", Contrast((Color)Field(palette, "Muted"), (Color)Field(palette, "Background")) >= 4.5);
        }
    }
    static double Luminance(Color c)
    {
        double[] rgb = { c.R / 255.0, c.G / 255.0, c.B / 255.0 };
        for (int i = 0; i < 3; i++) rgb[i] = rgb[i] <= .04045 ? rgb[i] / 12.92 : Math.Pow((rgb[i] + .055) / 1.055, 2.4);
        return .2126 * rgb[0] + .7152 * rgb[1] + .0722 * rgb[2];
    }
    static double Contrast(Color a, Color b)
    {
        double x = Luminance(a), y = Luminance(b);
        return (Math.Max(x, y) + .05) / (Math.Min(x, y) + .05);
    }
    static void TestLifetime()
    {
        using (Form warmup = NewForm()) Call(warmup, "UpdateVisualState");
        GC.Collect(); GC.WaitForPendingFinalizers();
        uint user = GetGuiResources(GetCurrentProcess(), 1), gdi = GetGuiResources(GetCurrentProcess(), 0);
        for (int i = 0; i < 40; i++)
        {
            Form form = NewForm();
            for (int j = 0; j < 5; j++) Call(form, "UpdateVisualState");
            form.Dispose(); form.Dispose();
        }
        GC.Collect(); GC.WaitForPendingFinalizers();
        int du = (int)GetGuiResources(GetCurrentProcess(), 1) - (int)user;
        int dg = (int)GetGuiResources(GetCurrentProcess(), 0) - (int)gdi;
        Console.WriteLine("lifetime delta: USER " + du + ", GDI " + dg);
        Check("repeated construction/refresh/disposal bounds USER handles", du <= 6);
        Check("repeated construction/refresh/disposal bounds GDI handles", dg <= 8);
    }
    [STAThread]
    static int Main(string[] args)
    {
        SetProcessDPIAware();
        Application.EnableVisualStyles();
        assembly = Assembly.LoadFrom(Path.GetFullPath(args[0]));
        formType = assembly.GetType("TaskbarPin.PinWidgetForm", true);
        Check("EXE includes a Windows icon resource", ExtractIconEx(Path.GetFullPath(args[0]), -1, null, null, 0) > 0);
        TestMenu();
        TestThemes();
        TestLifetime();
        Console.WriteLine(passed + " passed, " + failed + " failed");
        return failed == 0 ? 0 : 1;
    }
}
