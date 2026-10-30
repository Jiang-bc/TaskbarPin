using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

// Micro-test: what does Bitmap.GetHicon() leak, and does DestroyIcon reclaim it?
static class GdiTest
{
    [DllImport("user32.dll")]
    static extern uint GetGuiResources(IntPtr hProcess, uint uiFlags);
    [DllImport("kernel32.dll")]
    static extern IntPtr GetCurrentProcess();
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern bool DestroyIcon(IntPtr hIcon);
    [DllImport("user32.dll")]
    static extern IntPtr GetIconInfo(IntPtr hIcon, ref ICONINFO ii);
    [DllImport("gdi32.dll")]
    static extern bool DeleteObject(IntPtr h);

    [StructLayout(LayoutKind.Sequential)]
    struct ICONINFO { public bool fIcon; public int xHotspot; public int yHotspot; public IntPtr hbmMask; public IntPtr hbmColor; }

    static uint U() { return GetGuiResources(GetCurrentProcess(), 1); }
    static uint G() { return GetGuiResources(GetCurrentProcess(), 0); }

    [STAThread]
    static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        const int N = 100;
        using (Bitmap bmp = new Bitmap(32, 32, PixelFormat.Format32bppArgb))
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.Red);

            for (int i = 0; i < 5; i++) if (DestroyIcon(bmp.GetHicon())) { }

            uint u0 = U(), g0 = G();
            IntPtr[] icons = new IntPtr[N];
            for (int i = 0; i < N; i++) icons[i] = bmp.GetHicon();
            uint u1 = U(), g1 = G();
            Console.WriteLine("GetHicon x" + N + ": USER +" + ((int)u1 - (int)u0) + "  GDI +" + ((int)g1 - (int)g0));

            ICONINFO ii = new ICONINFO();
            GetIconInfo(icons[0], ref ii);
            Console.WriteLine("icon owns hbmMask=0x" + ii.hbmMask.ToInt64().ToString("X") + " hbmColor=0x" + ii.hbmColor.ToInt64().ToString("X"));

            for (int i = 0; i < N; i++) DestroyIcon(icons[i]);
            uint u2 = U(), g2 = G();
            Console.WriteLine("DestroyIcon x" + N + ": USER " + u1 + " -> " + u2 + "  GDI " + g1 + " -> " + g2);
            Console.WriteLine("=> per icon: USER " + ((int)u1 - (int)u0) + "  GDI " + ((int)g1 - (int)g0) +
                              "   reclaimed after DestroyIcon: USER " + ((int)u1 - (int)u2) + " GDI " + ((int)g1 - (int)g2));
        }
    }
}
