using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

// DragTest: drives a real left-button drag across the live widget, then reports the new position
// and the persisted offset. Usage: DragTest.exe <pid> <dx>   (dx < 0 drags left)
static class DragTest
{
    [DllImport("user32.dll")] static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] static extern void mouse_event(uint flags, int dx, int dy, uint data, UIntPtr extra);
    [DllImport("user32.dll")] static extern bool EnumChildWindows(IntPtr parent, EnumProc cb, IntPtr l);
    [DllImport("user32.dll")] static extern int GetWindowThreadProcessId(IntPtr h, out int pid);
    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr h, out RECT r);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern IntPtr FindWindow(string cls, string win);
    [DllImport("user32.dll")] static extern IntPtr WindowFromPoint(POINT p);
    [DllImport("user32.dll")] static extern bool GetCursorPos(out POINT p);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetClassName(IntPtr h, System.Text.StringBuilder sb, int max);
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)] static extern IntPtr SHAppBarMessage(int msg, ref APPBARDATA d);

    [StructLayout(LayoutKind.Sequential)] struct POINT { public int x, y; }
    [StructLayout(LayoutKind.Sequential)]
    struct APPBARDATA { public int cbSize; public IntPtr hWnd; public uint uCallbackMessage; public uint uEdge; public RECT rc; public IntPtr lParam; }

    delegate bool EnumProc(IntPtr h, IntPtr l);
    [StructLayout(LayoutKind.Sequential)] struct RECT { public int left, top, right, bottom; }

    const uint LEFTDOWN = 0x0002, LEFTUP = 0x0004;

    static IntPtr widget;
    static int widgetPid;

    static uint AppBarState()
    {
        APPBARDATA d = new APPBARDATA();
        d.cbSize = Marshal.SizeOf(typeof(APPBARDATA));
        return (uint)SHAppBarMessage(4, ref d).ToInt64(); // ABM_GETSTATE
    }

    static int Main(string[] argv)
    {
        SetProcessDPIAware();
        int pid = int.Parse(argv[0]);
        int dx = int.Parse(argv[1]);
        widgetPid = pid;

        IntPtr tray = FindWindow("Shell_TrayWnd", null);
        EnumChildWindows(tray, delegate(IntPtr h, IntPtr l)
        {
            int p; GetWindowThreadProcessId(h, out p);
            if (p == widgetPid) { widget = h; return false; }
            return true;
        }, IntPtr.Zero);

        if (widget == IntPtr.Zero) { Console.WriteLine("widget not found for pid " + pid); return 1; }

        RECT r; GetWindowRect(widget, out r);
        int cx = (r.left + r.right) / 2, cy = (r.top + r.bottom) / 2;
        Console.WriteLine("widget at " + r.left + "," + r.top + " - " + r.right + "," + r.bottom + "  center " + cx + "," + cy);

        SetCursorPos(cx, cy);
        Thread.Sleep(120);
        POINT cp; GetCursorPos(out cp);
        POINT hp; hp.x = cx; hp.y = cy;
        IntPtr hit = WindowFromPoint(hp);
        int hitPid; GetWindowThreadProcessId(hit, out hitPid);
        System.Text.StringBuilder cn = new System.Text.StringBuilder(256);
        GetClassName(hit, cn, 256);
        Console.WriteLine("cursor now " + cp.x + "," + cp.y + " ; WindowFromPoint(" + cx + "," + cy + ") = 0x" + hit.ToString("X") + " class=" + cn + " pid=" + hitPid + " (widget 0x" + widget.ToString("X") + ")");
        Console.WriteLine("appbar state = " + AppBarState());
        mouse_event(LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
        Thread.Sleep(120);
        // move in a few steps so the widget's 5 px drag threshold is crossed naturally
        int steps = 8;
        for (int i = 1; i <= steps; i++)
        {
            SetCursorPos(cx + dx * i / steps, cy);
            Thread.Sleep(30);
        }
        Thread.Sleep(150);
        mouse_event(LEFTUP, 0, 0, 0, UIntPtr.Zero);
        Thread.Sleep(400);

        RECT r2; GetWindowRect(widget, out r2);
        Console.WriteLine("after drag: " + r2.left + "," + r2.top + "  (moved " + (r2.left - r.left) + " px, asked " + dx + ")");
        return 0;
    }
}
