using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

// Probe: inspects the live taskbar + TaskbarPin widget window.
static class Probe
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr FindWindow(string cls, string win);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr FindWindowEx(IntPtr parent, IntPtr after, string cls, string win);
    [DllImport("user32.dll")]
    static extern bool GetWindowRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")]
    static extern bool EnumChildWindows(IntPtr parent, EnumProc cb, IntPtr l);
    [DllImport("user32.dll")]
    static extern int GetWindowThreadProcessId(IntPtr h, out int pid);
    [DllImport("user32.dll")]
    static extern int GetWindowLong(IntPtr h, int idx);
    [DllImport("user32.dll")]
    static extern IntPtr GetParent(IntPtr h);
    [DllImport("user32.dll")]
    static extern IntPtr GetAncestor(IntPtr h, uint flags);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern int GetClassName(IntPtr h, StringBuilder sb, int max);
    [DllImport("user32.dll")]
    static extern bool IsWindowVisible(IntPtr h);
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr SHAppBarMessage(int msg, ref APPBARDATA d);

    delegate bool EnumProc(IntPtr h, IntPtr l);

    [StructLayout(LayoutKind.Sequential)]
    struct RECT { public int left, top, right, bottom; }

    [StructLayout(LayoutKind.Sequential)]
    struct APPBARDATA { public int cbSize; public IntPtr hWnd; public uint uCallbackMessage; public uint uEdge; public RECT rc; public IntPtr lParam; }

    static int Main(string[] argv)
    {
        Console.OutputEncoding = Encoding.UTF8;
        IntPtr tray = FindWindow("Shell_TrayWnd", null);
        RECT rt; GetWindowRect(tray, out rt);
        APPBARDATA abd = new APPBARDATA();
        abd.cbSize = Marshal.SizeOf(typeof(APPBARDATA));
        abd.hWnd = tray;
        uint state = (uint)SHAppBarMessage(4 /*ABM_GETSTATE*/, ref abd).ToInt64();

        Console.WriteLine("Shell_TrayWnd = 0x" + tray.ToInt64().ToString("X"));
        Console.WriteLine("tray rect     = " + rt.left + "," + rt.top + " - " + rt.right + "," + rt.bottom +
                          "  (" + (rt.right - rt.left) + "x" + (rt.bottom - rt.top) + ")");
        Console.WriteLine("ABM_GETSTATE  = 0x" + state.ToString("X") +
                          "  (AUTOHIDE=" + ((state & 1) != 0) + " ALWAYSONTOP=" + ((state & 2) != 0) + ")");

        int pid0 = argv.Length > 0 ? int.Parse(argv[0]) : -1;
        int found = 0;
        EnumChildWindows(tray, delegate(IntPtr h, IntPtr l)
        {
            int pid; GetWindowThreadProcessId(h, out pid);
            int ex = GetWindowLong(h, -20);
            StringBuilder sb = new StringBuilder(256); GetClassName(h, sb, 256);
            RECT r; GetWindowRect(h, out r);
            bool layered = (ex & 0x80000) != 0, topmost = (ex & 8) != 0, tool = (ex & 0x80) != 0,
                 transparent = (ex & 0x20) != 0, noactivate = (ex & 0x08000000) != 0;
            bool isWidget = (r.right - r.left) == 38 && (r.bottom - r.top) == 38;
            if (pid0 >= 0 ? (pid != pid0 && !isWidget) : !isWidget) return true;
            found++;
            Console.WriteLine("child pid=" + pid + " hwnd=0x" + h.ToInt64().ToString("X"));
            Console.WriteLine("   class   = " + sb);
            Console.WriteLine("   rect    = " + r.left + "," + r.top + " - " + r.right + "," + r.bottom +
                              "  (" + (r.right - r.left) + "x" + (r.bottom - r.top) + ")");
            Console.WriteLine("   visible = " + IsWindowVisible(h) + "  layered=" + layered + " topmost=" + topmost +
                              " toolwindow=" + tool + " transparent=" + transparent + " noactivate=" + noactivate);
            Console.WriteLine("   inside taskbar? " + (r.top >= rt.top && r.bottom <= rt.bottom + 2 ? "YES" : "NO  <-- misplaced"));
            Console.WriteLine("   parent=0x" + GetParent(h).ToInt64().ToString("X") +
                              " anc=0x" + GetAncestor(h, 1).ToInt64().ToString("X") +
                              " tray=0x" + tray.ToInt64().ToString("X") +
                              " (GetParent==tray? " + (GetParent(h) == tray) + ", ancestor==tray? " + (GetAncestor(h, 1) == tray) + ")");
            return true;
        }, IntPtr.Zero);
        Console.WriteLine("children printed: " + found);

        // clock area anchor for reference
        IntPtr notify = FindWindowEx(tray, IntPtr.Zero, "TrayNotifyWnd", null);
        if (notify != IntPtr.Zero)
        {
            RECT rn; GetWindowRect(notify, out rn);
            Console.WriteLine("TrayNotifyWnd = " + rn.left + "," + rn.top + " (" + (rn.right - rn.left) + " wide)");
        }
        return 0;
    }
}
