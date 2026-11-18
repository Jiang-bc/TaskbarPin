using System;
using System.Runtime.InteropServices;

static class Probe
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr FindWindow(string className, string title);
    [DllImport("user32.dll")]
    static extern bool EnumChildWindows(IntPtr parent, EnumProc callback, IntPtr data);
    [DllImport("user32.dll")]
    static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
    [DllImport("shell32.dll")]
    static extern IntPtr SHAppBarMessage(uint message, ref AppBarData data);

    delegate bool EnumProc(IntPtr window, IntPtr data);
    [StructLayout(LayoutKind.Sequential)]
    struct Rect { public int left, top, right, bottom; }
    [StructLayout(LayoutKind.Sequential)]
    struct AppBarData { public uint size; public IntPtr window; public uint callback, edge; public Rect rect; public IntPtr parameter; }

    static int Main(string[] args)
    {
        uint processId;
        if (args.Length != 1 || !uint.TryParse(args[0], out processId))
        {
            Console.Error.WriteLine("Usage: Probe.exe <TaskbarPin PID>");
            return 2;
        }
        IntPtr tray = FindWindow("Shell_TrayWnd", null);
        if (tray == IntPtr.Zero) { Console.Error.WriteLine("Shell_TrayWnd not found"); return 1; }
        AppBarData data = new AppBarData();
        data.size = (uint)Marshal.SizeOf(typeof(AppBarData));
        data.window = tray;
        long state = SHAppBarMessage(4, ref data).ToInt64();
        int children = 0;
        bool enumerated = EnumChildWindows(tray, delegate(IntPtr window, IntPtr ignored)
        {
            uint owner;
            GetWindowThreadProcessId(window, out owner);
            if (owner == processId) children++;
            return true;
        }, IntPtr.Zero);
        Console.WriteLine("Shell_TrayWnd=0x{0:X} ABM_GETSTATE=0x{1:X} AUTOHIDE={2}", tray.ToInt64(), state, (state & 1) != 0);
        Console.WriteLine("TaskbarPin child windows: " + children);
        return enumerated && children == 0 ? 0 : 1;
    }
}
