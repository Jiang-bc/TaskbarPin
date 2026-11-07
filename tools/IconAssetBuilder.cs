using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using TaskbarPin;

// Offline asset generator. Compiled together with src/TaskbarPin.cs (see build.ps1) so the .ico/.png
// are drawn by exactly the same PinArtwork vector code as the running widget.
static class IconAssetBuilder
{
    static readonly int[] Sizes = { 16, 20, 24, 32, 40, 48, 64, 128, 256 };

    static int Main(string[] args)
    {
        if (args.Length != 2) { Console.Error.WriteLine("usage: IconAssetBuilder <ico> <png>"); return 2; }
        string icoPath = Path.GetFullPath(args[0]), pngPath = Path.GetFullPath(args[1]);
        Directory.CreateDirectory(Path.GetDirectoryName(icoPath));
        Directory.CreateDirectory(Path.GetDirectoryName(pngPath));
        UiPalette light = UiPalette.Create(true, false);

        byte[][] frames = new byte[Sizes.Length][];
        for (int i = 0; i < Sizes.Length; i++)
        {
            using (Bitmap bmp = Render(Sizes[i], light))
            {
                if (Sizes[i] == 256) bmp.Save(pngPath, ImageFormat.Png);
                frames[i] = Sizes[i] >= 256 ? Png(bmp) : Dib(bmp);
            }
        }

        using (FileStream fs = new FileStream(icoPath, FileMode.Create, FileAccess.Write))
        using (BinaryWriter w = new BinaryWriter(fs))
        {
            w.Write((short)0); w.Write((short)1); w.Write((short)Sizes.Length);
            int offset = 6 + 16 * Sizes.Length;
            for (int i = 0; i < Sizes.Length; i++)
            {
                w.Write((byte)(Sizes[i] >= 256 ? 0 : Sizes[i])); w.Write((byte)(Sizes[i] >= 256 ? 0 : Sizes[i]));
                w.Write((byte)0); w.Write((byte)0); w.Write((short)1); w.Write((short)32);
                w.Write(frames[i].Length); w.Write(offset);
                offset += frames[i].Length;
            }
            foreach (byte[] frame in frames) w.Write(frame);
        }
        Console.WriteLine("Wrote " + icoPath + " and " + pngPath);
        return 0;
    }

    static Bitmap Render(int size, UiPalette palette)
    {
        Bitmap bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using (Graphics g = Graphics.FromImage(bmp)) PinArtwork.DrawBadge(g, size, true, false, palette);
        return bmp;
    }

    static byte[] Png(Bitmap bmp)
    {
        using (MemoryStream ms = new MemoryStream()) { bmp.Save(ms, ImageFormat.Png); return ms.ToArray(); }
    }

    // Classic ICO DIB frame: BITMAPINFOHEADER (double height), bottom-up BGRA, then 1bpp AND mask.
    static byte[] Dib(Bitmap bmp)
    {
        int n = bmp.Width, maskStride = ((n + 31) / 32) * 4;
        using (MemoryStream ms = new MemoryStream())
        using (BinaryWriter w = new BinaryWriter(ms))
        {
            w.Write(40); w.Write(n); w.Write(n * 2); w.Write((short)1); w.Write((short)32);
            w.Write(0); w.Write(n * n * 4 + maskStride * n); w.Write(0); w.Write(0); w.Write(0); w.Write(0);
            for (int y = n - 1; y >= 0; y--)
                for (int x = 0; x < n; x++)
                {
                    Color c = bmp.GetPixel(x, y);
                    w.Write(c.B); w.Write(c.G); w.Write(c.R); w.Write(c.A);
                }
            w.Write(new byte[maskStride * n]); // alpha channel carries transparency
            return ms.ToArray();
        }
    }
}
