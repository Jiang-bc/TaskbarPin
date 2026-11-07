using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

// Visual inspection harness: renders the real ContextMenuStrip and icon sizes for light/dark/high-contrast
// palettes to PNGs. It calls the private theme hook with explicit palettes; no Windows setting is changed.
static class Render
{
    const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    static Assembly asm;

    static object Palette(bool light, bool hc) { return asm.GetType("TaskbarPin.UiPalette").GetMethod("Create", All).Invoke(null, new object[] { light, hc }); }

    [STAThread]
    static int Main(string[] args)
    {
        Application.EnableVisualStyles();
        string exe = Path.GetFullPath(args[0]), outDir = Path.GetFullPath(args[1]);
        Directory.CreateDirectory(outDir);
        asm = Assembly.LoadFrom(exe);
        Type formType = asm.GetType("TaskbarPin.PinWidgetForm");
        Type art = asm.GetType("TaskbarPin.PinArtwork");

        string[] names = { "light", "dark", "hc" };
        object[] palettes = { Palette(true, false), Palette(false, false), Palette(false, true) };
        // One form for all variants: this also exercises the live theme-refresh path. (Constructing further forms in
        // the same process after a menu was shown returned an inflated SystemFonts.MenuFont, which skewed the renders.)
        Form form = (Form)Activator.CreateInstance(formType, true);
        ((Timer)formType.GetField("_pollTimer", All).GetValue(form)).Stop();
        ((NotifyIcon)formType.GetField("_trayIcon", All).GetValue(form)).Visible = false;
        form.Hide();
        ContextMenuStrip menu = (ContextMenuStrip)formType.GetField("_contextMenu", All).GetValue(form);
        for (int p = 0; p < 3; p++)
            for (int state = 0; state < 2; state++)
            {
                formType.GetField("_isPinnedByApp", All).SetValue(form, state == 0);
                formType.GetMethod("ApplyMenuTheme", All).Invoke(form, new object[] { palettes[p] });
                formType.GetMethod("UpdateVisualState", All).Invoke(form, null);
                menu.Show(new Point(120, 120));
                Application.DoEvents();
                // Show() raises Opening, which (correctly) re-reads the real pin state and theme. Inject the
                // synthetic variant afterwards so every screenshot is deterministic and no setting is touched.
                formType.GetField("_isPinnedByApp", All).SetValue(form, state == 0);
                formType.GetMethod("ApplyMenuTheme", All).Invoke(form, new object[] { palettes[p] });
                formType.GetMethod("UpdateVisualState", All).Invoke(form, null);
                menu.PerformLayout();
                menu.Invalidate(true);
                Application.DoEvents();
                // Highlight one command to inspect hover/selection contrast.
                menu.Items[3].Select();
                Application.DoEvents();
                foreach (ToolStripItem it in menu.Items) Console.WriteLine("   item " + it.Bounds + " '" + it.Text + "'");
                using (Bitmap bmp = new Bitmap(menu.Width, menu.Height))
                {
                    // DrawToBitmap renders the control itself; CopyFromScreen captured unrelated windows.
                    menu.DrawToBitmap(bmp, new Rectangle(0, 0, menu.Width, menu.Height));
                    bmp.Save(Path.Combine(outDir, "menu-" + names[p] + (state == 0 ? "-pinned" : "-unpinned") + ".png"), ImageFormat.Png);
                    Console.WriteLine("menu " + names[p] + " " + (state == 0 ? "pinned" : "unpinned") + " " + menu.Width + "x" + menu.Height + " font " + menu.Font.SizeInPoints);
                }
                menu.Close();
            }
        form.Dispose();

        // Contact sheet: every shipped icon size, 1:1 and nearest-neighbour enlarged, on light/dark/white/black.
        int[] sizes = { 16, 20, 24, 32, 40, 48, 64, 128, 256 };
        Color[] back = { Color.FromArgb(243, 243, 243), Color.FromArgb(32, 32, 32), Color.White, Color.Black };
        MethodInfo draw = art.GetMethod("DrawBadge", All);
        using (Bitmap sheet = new Bitmap(1180, 1100))
        using (Graphics sg = Graphics.FromImage(sheet))
        {
            sg.Clear(Color.Gray);
            for (int b = 0; b < back.Length; b++)
            {
                int x0 = 10, y0 = 10 + b * 270;
                using (Brush br = new SolidBrush(back[b])) sg.FillRectangle(br, 0, y0 - 10, 1180, 270);
                int x = x0;
                foreach (int s in sizes)
                {
                    foreach (bool pinned in new[] { true, false })
                    {
                        using (Bitmap icon = new Bitmap(s, s, PixelFormat.Format32bppArgb))
                        {
                            using (Graphics g = Graphics.FromImage(icon)) draw.Invoke(null, new object[] { g, s, pinned, false, Palette(b != 1 && b != 3, false) });
                            if (s <= 64)
                            {
                                sg.DrawImageUnscaled(icon, x, y0 + (pinned ? 0 : 66));
                                int z = Math.Max(1, 128 / s);
                                sg.InterpolationMode = InterpolationMode.NearestNeighbor; sg.PixelOffsetMode = PixelOffsetMode.Half;
                                if (s <= 32) sg.DrawImage(icon, new Rectangle(x, y0 + 134 + (pinned ? 0 : 0), s * z / 2 > 0 ? s * 2 : s, s * 2), 0, 0, s, s, GraphicsUnit.Pixel);
                            }
                        }
                    }
                    x += s <= 32 ? 76 : 100;
                }
            }
            sheet.Save(Path.Combine(outDir, "icons-contact-sheet.png"), ImageFormat.Png);
        }
        return 0;
    }
}
