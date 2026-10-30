using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

// Samples CPU time and USER/GDI handle counts of another process.
static class Stats
{
    [DllImport("kernel32.dll")]
    static extern IntPtr OpenProcess(uint access, bool inherit, int pid);
    [DllImport("kernel32.dll")]
    static extern bool CloseHandle(IntPtr h);
    [DllImport("user32.dll")]
    static extern uint GetGuiResources(IntPtr hProcess, uint uiFlags);

    const uint PROCESS_QUERY_INFORMATION = 0x0400;

    static int Main(string[] argv)
    {
        int pid = int.Parse(argv[0]);
        int seconds = argv.Length > 1 ? int.Parse(argv[1]) : 10;
        using (Process p = Process.GetProcessById(pid))
        {
            IntPtr h = OpenProcess(PROCESS_QUERY_INFORMATION, false, pid);
            if (h == IntPtr.Zero) { Console.WriteLine("OpenProcess failed: " + Marshal.GetLastWin32Error()); return 1; }

            p.Refresh();
            TimeSpan t0 = p.TotalProcessorTime;
            uint u0 = GetGuiResources(h, 1), g0 = GetGuiResources(h, 0);
            Console.WriteLine("pid=" + pid + " start cpu=" + t0.TotalMilliseconds.ToString("F0") + "ms USER=" + u0 + " GDI=" + g0);
            Thread.Sleep(seconds * 1000);
            p.Refresh();
            TimeSpan t1 = p.TotalProcessorTime;
            uint u1 = GetGuiResources(h, 1), g1 = GetGuiResources(h, 0);
            double cpuMs = (t1 - t0).TotalMilliseconds;
            Console.WriteLine("after " + seconds + "s: cpu=" + cpuMs.ToString("F0") + "ms -> " +
                              (cpuMs / (seconds * 1000.0) * 100).ToString("F2") + "% of one core, " +
                              (cpuMs / (seconds * 1000.0) / Environment.ProcessorCount * 100).ToString("F2") + "% total");
            Console.WriteLine("handles: USER " + u0 + " -> " + u1 + " (" + ((long)u1 - u0) + "), GDI " + g0 + " -> " + g1 + " (" + ((long)g1 - g0) + ")");
            CloseHandle(h);
        }
        return 0;
    }
}
