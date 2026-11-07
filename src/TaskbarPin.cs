using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace TaskbarPin
{
    static class Win32
    {
        public const int GWL_EXSTYLE = -20;
        public const int WS_EX_LAYERED = 0x00080000;
        public const int WS_EX_TOOLWINDOW = 0x00000080;
        public const int WS_EX_TOPMOST = 0x00000008;
        public const int WS_EX_NOACTIVATE = 0x08000000;
        public const int WS_EX_TRANSPARENT = 0x00000020;

        public const int WM_MOUSEACTIVATE = 0x0021;
        public const int MA_NOACTIVATE = 3;

        public const uint ABM_GETSTATE = 0x00000004;
        public const uint ABM_SETSTATE = 0x0000000A;
        public const int ABS_AUTOHIDE = 0x00000001;
        public const int ABS_ALWAYSONTOP = 0x00000002;

        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        [DllImport("user32.dll")] public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);
        [DllImport("user32.dll")] public static extern IntPtr GetParent(IntPtr hWnd);
        public const uint GA_PARENT = 1;
        [DllImport("user32.dll")] public static extern IntPtr GetAncestor(IntPtr hWnd, uint gaFlags);
        public const uint SWP_NOSIZE = 0x0001;
        public const uint SWP_NOMOVE = 0x0002;
        public const uint SWP_NOACTIVATE = 0x0010;
        public const uint SWP_SHOWWINDOW = 0x0040;

        public const uint MOUSEEVENTF_MOVE = 0x0001;

        public const byte AC_SRC_OVER = 0x00;
        public const byte AC_SRC_ALPHA = 0x01;
        public const int ULW_ALPHA = 0x00000002;

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
            public POINT(int x, int y) { this.x = x; this.y = y; }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SIZE
        {
            public int cx;
            public int cy;
            public SIZE(int cx, int cy) { this.cx = cx; this.cy = cy; }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct BLENDFUNCTION
        {
            public byte BlendOp;
            public byte BlendFlags;
            public byte SourceConstantAlpha;
            public byte AlphaFormat;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct APPBARDATA
        {
            public uint cbSize;
            public IntPtr hWnd;
            public uint uCallbackMessage;
            public uint uEdge;
            public RECT rc;
            public IntPtr lParam;
        }

        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        public static extern bool SetProcessDPIAware();

        [DllImport("shell32.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string lclassName, string windowTitle);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        public static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        public static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr GetShellWindow();

        [DllImport("user32.dll")]
        public static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentThreadId();

        [DllImport("user32.dll")]
        public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("user32.dll")]
        public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        public static extern bool UpdateLayeredWindow(
            IntPtr hwnd,
            IntPtr hdcDst,
            ref POINT pptDst,
            ref SIZE psize,
            IntPtr hdcSrc,
            ref POINT pptSrc,
            uint crKey,
            ref BLENDFUNCTION pblend,
            uint dwFlags);

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll", ExactSpelling = true)]
        public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr CreateCompatibleDC(IntPtr hDC);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        public static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll", ExactSpelling = true)]
        public static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        public static extern bool DeleteObject(IntPtr hObject);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool DestroyIcon(IntPtr handle);
    }

    // Taskbar and application themes can differ. No registry handles or global event subscriptions
    // are retained: Windows theme/settings messages and menu opening refresh the palettes.
    sealed class UiPalette
    {
        public Color Background, Foreground, Muted, Border, Selection, Badge, BadgeText;
        public bool HighContrast;

        public static UiPalette Create(bool light, bool highContrast)
        {
            if (highContrast)
                return new UiPalette { HighContrast = true, Background = SystemColors.Menu,
                    Foreground = SystemColors.MenuText, Muted = SystemColors.MenuText,
                    Border = SystemColors.WindowText, Selection = SystemColors.Highlight,
                    Badge = SystemColors.Window, BadgeText = SystemColors.WindowText };
            return light
                ? new UiPalette { Background = Color.FromArgb(250, 250, 250), Foreground = Color.FromArgb(30, 33, 38),
                    Muted = Color.FromArgb(89, 96, 107), Border = Color.FromArgb(215, 219, 225),
                    Selection = Color.FromArgb(232, 237, 244), Badge = Color.FromArgb(242, 245, 249), BadgeText = Color.FromArgb(62, 76, 96) }
                : new UiPalette { Background = Color.FromArgb(37, 39, 43), Foreground = Color.FromArgb(245, 246, 248),
                    Muted = Color.FromArgb(183, 190, 202), Border = Color.FromArgb(76, 81, 90),
                    Selection = Color.FromArgb(58, 63, 72), Badge = Color.FromArgb(47, 53, 63), BadgeText = Color.FromArgb(231, 236, 244) };
        }

        public static UiPalette Read(bool taskbar)
        {
            bool light = true;
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    object value = key == null ? null : key.GetValue(taskbar ? "SystemUsesLightTheme" : "AppsUseLightTheme");
                    if (value is int) light = (int)value != 0;
                }
            }
            catch (System.Security.SecurityException) { }
            catch (UnauthorizedAccessException) { }
            return Create(light, SystemInformation.HighContrast);
        }
    }

    // Shared vector source for the Windows asset, widget, tray and menu pin, on a 32px grid.
    // Small sizes receive a slightly broader head and needle for optical clarity.
    static class PinArtwork
    {
        public static GraphicsPath RoundedRect(RectangleF r, float radius)
        {
            float d = radius * 2;
            GraphicsPath path = new GraphicsPath();
            path.AddArc(r.Left, r.Top, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Top, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static void DrawBadge(Graphics g, int size, bool pinned, bool hovered, UiPalette palette)
        {
            GraphicsState state = g.Save();
            try
            {
                g.Clear(Color.Transparent);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                float scale = size / 32f;
                g.ScaleTransform(scale, scale);
                Color fill = pinned ? (palette.HighContrast ? SystemColors.Highlight : Color.FromArgb(29, 96, 188)) : palette.Badge;
                Color ink = pinned ? (palette.HighContrast ? SystemColors.HighlightText : Color.White) : palette.BadgeText;
                Color border = palette.HighContrast ? ink : (pinned ? Color.FromArgb(22, 77, 155) : palette.Border);
                if (hovered)
                {
                    if (palette.HighContrast) { fill = SystemColors.Highlight; ink = SystemColors.HighlightText; border = ink; }
                    else fill = pinned ? Color.FromArgb(45, 116, 210) : palette.Selection;
                }
                float pad = size <= 20 ? 1.5f : 2f;
                using (GraphicsPath badge = RoundedRect(new RectangleF(pad, pad, 32 - pad * 2, 32 - pad * 2), 7f))
                using (Brush brush = new SolidBrush(fill))
                using (Pen pen = new Pen(border, 1f / scale))
                {
                    g.FillPath(brush, badge);
                    g.DrawPath(pen, badge);
                }
                DrawPin(g, ink, pinned, size <= 20);
            }
            finally { g.Restore(state); }
        }

        private static void DrawPin(Graphics g, Color color, bool upright, bool small)
        {
            GraphicsState state = g.Save();
            try
            {
                if (!upright) { g.TranslateTransform(16, 16); g.RotateTransform(38); g.TranslateTransform(-16, -16); }
                float left = small ? 11 : 11.5f, right = 32 - left;
                using (Brush brush = new SolidBrush(color))
                using (GraphicsPath body = new GraphicsPath())
                using (Pen needle = new Pen(color, small ? 2.1f : 1.8f))
                {
                    body.AddPolygon(new PointF[] { new PointF(left, 7), new PointF(right, 7),
                        new PointF(right, 10), new PointF(19, 10), new PointF(19, 14.5f),
                        new PointF(22, 18.5f), new PointF(22, 20), new PointF(10, 20),
                        new PointF(10, 18.5f), new PointF(13, 14.5f), new PointF(13, 10), new PointF(left, 10) });
                    g.FillPath(brush, body);
                    needle.StartCap = LineCap.Flat; needle.EndCap = LineCap.Round;
                    g.DrawLine(needle, 16, 19, 16, 25);
                }
            }
            finally { g.Restore(state); }
        }

        public static Bitmap MenuGlyph(string kind, int size, Color color, bool active)
        {
            Bitmap bmp = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            try
            {
                using (Graphics g = Graphics.FromImage(bmp))
                using (Pen pen = new Pen(color, 1.8f))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.ScaleTransform(size / 24f, size / 24f);
                    pen.StartCap = pen.EndCap = LineCap.Round;
                    pen.LineJoin = LineJoin.Round;
                    if (kind == "pin")
                    {
                        g.ScaleTransform(.75f, .75f);
                        DrawPin(g, color, active, false);
                    }
                    else if (kind == "reset")
                    {
                        g.DrawArc(pen, 5, 5, 14, 14, -70, 290);
                        g.DrawLines(pen, new PointF[] { new PointF(4, 9), new PointF(4, 4), new PointF(9, 4) });
                    }
                    else if (kind == "start")
                    {
                        if (active) g.DrawLines(pen, new PointF[] { new PointF(5, 12), new PointF(10, 17), new PointF(19, 7) });
                        else { g.DrawArc(pen, 5, 5, 14, 14, -50, 280); g.DrawLine(pen, 12, 3, 12, 11); }
                    }
                    else if (kind == "exit")
                    {
                        g.DrawLines(pen, new PointF[] { new PointF(10, 4), new PointF(5, 4), new PointF(5, 20), new PointF(10, 20) });
                        g.DrawLine(pen, 10, 12, 21, 12);
                        g.DrawLines(pen, new PointF[] { new PointF(17, 8), new PointF(21, 12), new PointF(17, 16) });
                    }
                }
                return bmp;
            }
            catch { bmp.Dispose(); throw; }
        }
    }

    sealed class PinMenuRenderer : ToolStripProfessionalRenderer
    {
        private readonly UiPalette _palette;
        public PinMenuRenderer(UiPalette palette) { _palette = palette; RoundedEdges = false; }
        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            using (Brush b = new SolidBrush(_palette.Background)) e.Graphics.FillRectangle(b, e.AffectedBounds);
        }
        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            using (Pen p = new Pen(_palette.Border)) e.Graphics.DrawRectangle(p, 0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
        }
        protected override void OnRenderImageMargin(ToolStripRenderEventArgs e) { }
        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e) { } // glyphs carry state in the shared gutter
        protected override void OnRenderItemImage(ToolStripItemImageRenderEventArgs e)
        {
            // The stock professional renderer paints Checked items with fixed system colours, which made the
            // light glyph vanish on the dark menu. Draw an "on" chip from the palette instead.
            ToolStripMenuItem mi = e.Item as ToolStripMenuItem;
            if (mi != null && mi.Checked)
            {
                Rectangle chip = Rectangle.Inflate(e.ImageRectangle, 3, 3);
                using (Brush b = new SolidBrush(_palette.Selection)) e.Graphics.FillRectangle(b, chip);
                using (Pen p = new Pen(_palette.Muted)) e.Graphics.DrawRectangle(p, chip.X, chip.Y, chip.Width - 1, chip.Height - 1);
            }
            if (e.Image != null) e.Graphics.DrawImage(e.Image, e.ImageRectangle);
        }
        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (e.Item.Enabled && e.Item.Selected)
                using (Brush b = new SolidBrush(_palette.Selection))
                    e.Graphics.FillRectangle(b, new Rectangle(2, 0, e.Item.Width - 4, e.Item.Height));
        }
        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            // The base renderer substitutes SystemColors.GrayText for disabled items.
            // Paint the status row with our accessible palette instead.
            Color color = e.Item.Enabled ? _palette.Foreground : _palette.Muted;
            TextRenderer.DrawText(e.Graphics, e.Text, e.TextFont, e.TextRectangle, color, e.TextFormat);
        }
        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            using (Pen p = new Pen(_palette.Border))
                e.Graphics.DrawLine(p, 10, e.Item.Height / 2, e.Item.Width - 10, e.Item.Height / 2);
        }
    }

    // Use Windows' own background, focus and selection rendering in high contrast. Repaint
    // monochrome glyphs with HighlightText on selection, so a bitmap cannot disappear there.
    sealed class PinHighContrastMenuRenderer : ToolStripSystemRenderer
    {
        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e) { }
        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            Color color = e.Item.Enabled && e.Item.Selected ? SystemColors.HighlightText : SystemColors.MenuText;
            TextRenderer.DrawText(e.Graphics, e.Text, e.TextFont, e.TextRectangle, color, e.TextFormat);
        }
        protected override void OnRenderItemImage(ToolStripItemImageRenderEventArgs e)
        {
            string kind = e.Item.Tag as string;
            if (kind == null) { base.OnRenderItemImage(e); return; }
            ToolStripMenuItem item = e.Item as ToolStripMenuItem;
            Color color = e.Item.Selected ? SystemColors.HighlightText : SystemColors.MenuText;
            using (Bitmap glyph = PinArtwork.MenuGlyph(kind, e.ImageRectangle.Width, color, item != null && item.Checked))
                e.Graphics.DrawImageUnscaled(glyph, e.ImageRectangle.Location);
        }
    }

    public class PinWidgetForm : Form
    {
        private readonly System.Windows.Forms.Timer _pollTimer;
        private readonly NotifyIcon _trayIcon;
        private readonly ContextMenuStrip _contextMenu;
        private readonly ToolStripMenuItem _menuTogglePin;
        private readonly ToolStripMenuItem _menuAutoStart;
        private readonly ToolStripMenuItem _menuStatus;
        private readonly ToolStripMenuItem _menuResetPos;
        private readonly ToolStripMenuItem _menuExit;
        private readonly Font _menuFont;
        private readonly float _uiScale;
        private UiPalette _menuPalette;
        private UiPalette _taskbarPalette;
        private bool _resourcesDisposed;
        private Icon _applicationIcon;
        private readonly string _configPath;

        private bool _isPinnedByApp;
        private bool _isHovered;
        private bool _isDragging;
        private Point _dragStartCursor;
        private int _dragStartOffsetX;
        private bool _hasMovedDuringDrag;

        // Horizontal offset from the left edge of TrayNotifyWnd (system tray area).
        private int _offsetFromTrayNotifyLeft = -48;

        private readonly int _widgetSize = 38;
        private ToolTip _toolTip;

        // State tracking for instant, bounce-free taskbar collapse
        private IntPtr _lastNonTrayForeground = IntPtr.Zero;
        private bool _waitingForCollapse = false;
        private DateTime _unpinRequestedAt = DateTime.MinValue;
        private int _collapseStage = 0;
        private bool _isMouseThrough = false;
        private int _lastRenderedX = int.MinValue;
        private int _lastRenderedY = int.MinValue;

        // Native icon currently shown in the tray. Bitmap.GetHicon() allocates 1 USER + 3 GDI
        // handles that only DestroyIcon() frees, so every replaced icon must be destroyed.
        private IntPtr _trayIconHandle = IntPtr.Zero;

        // Cached Shell_TrayWnd handle; FindWindow at 60 Hz is wasted work.
        private IntPtr _trayHwnd = IntPtr.Zero;

        // Last known system auto-hide state. SHAppBarMessage is a cross-process round trip
        // to the shell, so it is probed a few times per second instead of every tick.
        private bool _cachedAutoHide;
        private DateTime _lastAutoHideProbeAt = DateTime.MinValue;

        // NotifyIcon raises MouseClick twice for a double click; debounce the toggle.
        private DateTime _lastTrayClickUtc = DateTime.MinValue;

        public PinWidgetForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = false;
            StartPosition = FormStartPosition.Manual;
            Size = new Size(_widgetSize, _widgetSize);

            _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini");
            LoadConfig();

            _toolTip = new ToolTip();
            _toolTip.InitialDelay = 200;
            _toolTip.ReshowDelay = 100;

            using (Graphics screen = Graphics.FromHwnd(IntPtr.Zero)) _uiScale = screen.DpiX / 96f;
            _taskbarPalette = UiPalette.Read(true);
            _menuFont = (Font)SystemFonts.MenuFont.Clone();
            _contextMenu = new ContextMenuStrip();
            _contextMenu.Font = _menuFont;
            _contextMenu.ShowImageMargin = true;
            _contextMenu.ShowCheckMargin = false;
            _contextMenu.ImageScalingSize = new Size(Scale(18), Scale(18));
            _contextMenu.Padding = new Padding(Scale(4), Scale(6), Scale(4), Scale(6));
            _menuStatus = new ToolStripMenuItem { Enabled = false, AccessibleRole = AccessibleRole.StaticText };
            _menuTogglePin = new ToolStripMenuItem("钉住任务栏(&P)", null, OnMenuTogglePin) { Tag = "pin" };
            _menuResetPos = new ToolStripMenuItem("重置图标位置(&R)", null, (s, e) =>
            {
                _offsetFromTrayNotifyLeft = -48;
                SaveConfig();
                UpdateWidgetPositionAndVisibility(true);
            }) { Tag = "reset" };
            _menuAutoStart = new ToolStripMenuItem("开机启动(&S)", null, OnMenuAutoStart)
            {
                Checked = IsAutoStartEnabled(), Tag = "start"
            };
            _menuExit = new ToolStripMenuItem("退出并恢复自动隐藏(&X)", null, OnMenuExit) { Tag = "exit" };

            _contextMenu.Items.Add(_menuStatus);
            _contextMenu.Items.Add(new ToolStripSeparator());
            _contextMenu.Items.Add(_menuTogglePin);
            _contextMenu.Items.Add(_menuResetPos);
            _contextMenu.Items.Add(_menuAutoStart);
            _contextMenu.Items.Add(new ToolStripSeparator());
            _contextMenu.Items.Add(_menuExit);
            foreach (ToolStripItem item in _contextMenu.Items)
                if (item is ToolStripMenuItem) item.Padding = new Padding(Scale(6), Scale(7), Scale(12), Scale(7));
            _contextMenu.Opening += delegate
            {
                // The menu may have been closed while Windows Settings or another instance changed state.
                _isPinnedByApp = !GetTaskbarAutoHide();
                _menuAutoStart.Checked = IsAutoStartEnabled();
                RefreshAppearance();
                UpdateVisualState();
            };
            ApplyMenuTheme(UiPalette.Read(false));
            _applicationIcon = Icon.ExtractAssociatedIcon(typeof(PinWidgetForm).Assembly.Location);
            Icon = _applicationIcon;

            _trayIcon = new NotifyIcon();
            _trayIcon.ContextMenuStrip = _contextMenu;
            _trayIcon.Visible = true;
            _trayIcon.MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    // A double click arrives as two MouseClick events; without this debounce the
                    // widget pins and immediately unpins again.
                    if ((DateTime.UtcNow - _lastTrayClickUtc).TotalMilliseconds < 350) return;
                    _lastTrayClickUtc = DateTime.UtcNow;
                    TogglePinState();
                }
            };

            // Initialize state: if taskbar is currently NOT auto-hide, treat as pinned
            _cachedAutoHide = GetTaskbarAutoHide();
            _lastAutoHideProbeAt = DateTime.UtcNow;
            _isPinnedByApp = !_cachedAutoHide;

            // Force handle creation immediately even if taskbar starts collapsed
            IntPtr dummyHandle = this.Handle;

            UpdateVisualState();

            _pollTimer = new System.Windows.Forms.Timer();
            _pollTimer.Interval = 16;
            _pollTimer.Tick += PollTimer_Tick;
            _pollTimer.Start();
        }

        private void LoadConfig()
        {
            try
            {
                if (!File.Exists(_configPath)) return;
                foreach (string rawLine in File.ReadAllLines(_configPath))
                {
                    // Tolerate hand-edited files: "OffsetX = -35", "  offsetx=-35", tabs, ...
                    string line = rawLine.Trim();
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    if (!string.Equals(line.Substring(0, eq).Trim(), "OffsetX", StringComparison.OrdinalIgnoreCase)) continue;

                    int val;
                    if (int.TryParse(line.Substring(eq + 1).Trim(), out val))
                    {
                        _offsetFromTrayNotifyLeft = val;
                    }
                }
            }
            catch (Exception ex) { CrashLog.Write("LoadConfig", ex); }
        }

        private void SaveConfig()
        {
            try
            {
                // Only the offset is persisted. The pin state is not stored on purpose: the taskbar's
                // own auto-hide flag (ABM_GETSTATE) is the single source of truth, and a stale copy
                // here would fight it at startup.
                File.WriteAllText(_configPath, string.Format("OffsetX={0}\r\n", _offsetFromTrayNotifyLeft));
            }
            catch (Exception ex) { CrashLog.Write("SaveConfig", ex); }
        }

        private bool IsAutoStartEnabled()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false))
                {
                    return key != null && key.GetValue("TaskbarPin") != null;
                }
            }
            catch { return false; }
        }

        private void OnMenuAutoStart(object sender, EventArgs e)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key == null) return;
                    if (IsAutoStartEnabled())
                    {
                        key.DeleteValue("TaskbarPin", false);
                        _menuAutoStart.Checked = false;
                    }
                    else
                    {
                        key.SetValue("TaskbarPin", "\"" + Application.ExecutablePath + "\"");
                        _menuAutoStart.Checked = true;
                    }
                }
                UpdateVisualState();
            }
            catch (Exception ex) { CrashLog.Write("Autostart", ex); }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= Win32.WS_EX_LAYERED | Win32.WS_EX_TOOLWINDOW | Win32.WS_EX_TOPMOST | Win32.WS_EX_NOACTIVATE;
                return cp;
            }
        }

        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Win32.WM_MOUSEACTIVATE)
            {
                m.Result = (IntPtr)Win32.MA_NOACTIVATE;
                return;
            }
            base.WndProc(ref m);
            if (m.Msg == 0x031A || m.Msg == 0x001A || m.Msg == 0x007E) // theme, settings, display
                RefreshAppearance();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            RenderLayeredBitmap();
            UpdateWidgetPositionAndVisibility(true);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _isHovered = true;
            Cursor = Cursors.Hand;
            RenderLayeredBitmap();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _isHovered = false;
            RenderLayeredBitmap();

            // If taskbar is unpinned (auto-hide mode) and user hovered the widget then moved cursor
            // directly upward above the taskbar without touching Shell_TrayWnd, trigger native collapse
            if (!_isPinnedByApp && !_waitingForCollapse)
            {
                IntPtr tray = Win32.FindWindow("Shell_TrayWnd", null);
                Win32.RECT rTray;
                Win32.POINT pt;
                if (tray != IntPtr.Zero && Win32.GetWindowRect(tray, out rTray) && Win32.GetCursorPos(out pt))
                {
                    if (pt.y < rTray.top - 2)
                    {
                        _waitingForCollapse = true;
                        _unpinRequestedAt = DateTime.UtcNow;
                        _collapseStage = 1;
                        SetMouseThrough(true);
                        TriggerNativeTaskbarCollapse(_lastNonTrayForeground, false);
                    }
                }
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                _isDragging = true;
                _hasMovedDuringDrag = false;
                _dragStartCursor = Cursor.Position;
                _dragStartOffsetX = _offsetFromTrayNotifyLeft;
            }
            else if (e.Button == MouseButtons.Right)
            {
                _contextMenu.Show(Cursor.Position);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_isDragging)
            {
                Point cur = Cursor.Position;
                int dx = cur.X - _dragStartCursor.X;
                int dy = cur.Y - _dragStartCursor.Y;
                if (Math.Abs(dx) >= 5 || Math.Abs(dy) >= 5)
                {
                    _hasMovedDuringDrag = true;
                    _offsetFromTrayNotifyLeft = _dragStartOffsetX + dx;
                    UpdateWidgetPositionAndVisibility(true);
                }
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left && _isDragging)
            {
                _isDragging = false;
                this.Capture = false;
                if (!_hasMovedDuringDrag)
                {
                    TogglePinState();
                }
                else
                {
                    SaveConfig();
                }
            }
        }

        private int Scale(int value) { return Math.Max(1, (int)Math.Round(value * _uiScale)); }

        private void ApplyMenuTheme(UiPalette palette)
        {
            _menuPalette = palette;
            _contextMenu.BackColor = palette.Background;
            _contextMenu.ForeColor = palette.Foreground;
            _contextMenu.Renderer = palette.HighContrast
                ? (ToolStripRenderer)new PinHighContrastMenuRenderer()
                : new PinMenuRenderer(palette);
            UpdateMenuImages();
        }

        private void RefreshAppearance()
        {
            if (_resourcesDisposed || _trayIcon == null) return;
            _taskbarPalette = UiPalette.Read(true);
            ApplyMenuTheme(UiPalette.Read(false));
            UpdateTrayIconImage();
            if (IsHandleCreated) RenderLayeredBitmap();
        }

        private void UpdateMenuImages()
        {
            foreach (ToolStripItem item in _contextMenu.Items)
            {
                string kind = item.Tag as string;
                if (kind == null) continue;
                bool active = kind == "pin" ? _isPinnedByApp : kind == "start" && _menuAutoStart.Checked;
                Image replacement = PinArtwork.MenuGlyph(kind, Scale(18), _menuPalette.Foreground, active);
                Image previous = item.Image;
                item.Image = replacement;
                if (previous != null) previous.Dispose(); // ToolStripItem does not own assigned Image objects.
            }
        }

        private void OnMenuTogglePin(object sender, EventArgs e)
        {
            TogglePinState();
        }

        private void OnMenuExit(object sender, EventArgs e)
        {
            _pollTimer.Stop();
            if (_isPinnedByApp)
            {
                if (this.Visible) this.Hide();
                ApplyTaskbarAutoHide(true);
                TriggerNativeTaskbarCollapse(_lastNonTrayForeground, true);
            }
            _trayIcon.Visible = false;
            Close(); // The form's Dispose method owns all icon/image resources.
            Application.Exit();
        }

        public void TogglePinState()
        {
            SetPinnedState(!_isPinnedByApp);
        }

        public void SetPinnedState(bool pinTaskbar)
        {
            _isPinnedByApp = pinTaskbar;
            SaveConfig();
            if (_isPinnedByApp)
            {
                _waitingForCollapse = false;
                _collapseStage = 0;
                // Pin taskbar: disable auto-hide (ABS_ALWAYSONTOP)
                ApplyTaskbarAutoHide(false);
                UpdateVisualState();
                UpdateWidgetPositionAndVisibility(true);
            }
            else
            {
                // Unpin taskbar: enable auto-hide (ABS_AUTOHIDE) and immediately trigger collapse
                UpdateVisualState();
                _waitingForCollapse = true;
                _unpinRequestedAt = DateTime.UtcNow;
                _collapseStage = 0;

                // Release capture and set mouse-through so taskbar receives native mouse events,
                // while the widget stays visible and follows taskbar drop animation.
                this.Capture = false;
                SetMouseThrough(true);

                ApplyTaskbarAutoHide(true);
                IntPtr fallbackFg = _lastNonTrayForeground;
                this.BeginInvoke((MethodInvoker)delegate
                {
                    _collapseStage = 1;
                    TriggerNativeTaskbarCollapse(fallbackFg, true);
                });
            }
        }

        public static void ForceForegroundWindow(IntPtr targetHwnd)
        {
            if (targetHwnd == IntPtr.Zero || !Win32.IsWindow(targetHwnd)) return;
            IntPtr fg = Win32.GetForegroundWindow();
            if (fg == targetHwnd) return;

            uint dummy;
            uint fgThread = Win32.GetWindowThreadProcessId(fg, out dummy);
            uint curThread = Win32.GetCurrentThreadId();
            if (fgThread != 0 && fgThread != curThread)
            {
                Win32.AttachThreadInput(curThread, fgThread, true);
                Win32.keybd_event(0, 0, 0, UIntPtr.Zero);
                Win32.SetForegroundWindow(targetHwnd);
                Win32.AttachThreadInput(curThread, fgThread, false);
            }
            else
            {
                Win32.keybd_event(0, 0, 0, UIntPtr.Zero);
                Win32.SetForegroundWindow(targetHwnd);
            }
        }

        public static void ReleaseTrayFocusIfNeeded(IntPtr tray, IntPtr preferredFallback)
        {
            IntPtr fg = Win32.GetForegroundWindow();
            if (fg != tray && fg != IntPtr.Zero) return;

            IntPtr targetFg = preferredFallback;
            if (targetFg == IntPtr.Zero || targetFg == tray || !Win32.IsWindow(targetFg) || !Win32.IsWindowVisible(targetFg))
            {
                targetFg = Win32.GetShellWindow();
            }
            if (targetFg != IntPtr.Zero)
            {
                ForceForegroundWindow(targetFg);
            }
        }

        public static void TriggerNativeTaskbarCollapse(IntPtr preferredFallbackFg, bool includeFocusPulse)
        {
            IntPtr tray = Win32.FindWindow("Shell_TrayWnd", null);
            if (tray == IntPtr.Zero) return;

            Win32.RECT rTray;
            if (!Win32.GetWindowRect(tray, out rTray)) return;

            int screenH = Screen.PrimaryScreen.Bounds.Height;
            int trayHeight = Math.Max(32, rTray.bottom - rTray.top);
            int expandedTop = screenH - trayHeight;

            // CRITICAL: Only trigger if taskbar is still completely stationary at its fully expanded top position!
            // If rTray.top > expandedTop + 2, it is already collapsing or collapsed - touching it would bounce it back up!
            if (rTray.top > expandedTop + 2) return;

            Win32.POINT origPt;
            if (!Win32.GetCursorPos(out origPt)) return;

            int touchX = origPt.x;
            int touchY = Math.Min(rTray.bottom - 4, rTray.top + 10);
            int exitY = (origPt.y < rTray.top - 15) ? origPt.y : Math.Max(0, rTray.top - 15);

            // 1. Touch uncovered Shell_TrayWnd so it registers mouse-enter, then move above taskbar top edge
            Win32.SetCursorPos(touchX, touchY);
            Win32.mouse_event(Win32.MOUSEEVENTF_MOVE, 0, 1, 0, UIntPtr.Zero);
            Thread.Sleep(20);
            Win32.SetCursorPos(origPt.x, exitY);
            Win32.mouse_event(Win32.MOUSEEVENTF_MOVE, 0, -1, 0, UIntPtr.Zero);

            // 2. Pulse foreground focus away from Shell_TrayWnd if requested and still stationary at expandedTop
            if (includeFocusPulse)
            {
                IntPtr targetFg = Win32.GetForegroundWindow();
                if (targetFg == IntPtr.Zero || targetFg == tray || !Win32.IsWindowVisible(targetFg))
                {
                    targetFg = (preferredFallbackFg != IntPtr.Zero && Win32.IsWindow(preferredFallbackFg) && Win32.IsWindowVisible(preferredFallbackFg) && preferredFallbackFg != tray)
                        ? preferredFallbackFg
                        : Win32.GetShellWindow();
                }
                ForceForegroundWindow(tray);
                ForceForegroundWindow(targetFg);
            }
            else
            {
                ReleaseTrayFocusIfNeeded(tray, preferredFallbackFg);
            }
        }

        private void PollTimer_Tick(object sender, EventArgs e)
        {
            IntPtr tray = TrayHwnd();
            if (tray != IntPtr.Zero)
            {
                IntPtr fg = Win32.GetForegroundWindow();
                if (fg != IntPtr.Zero && fg != tray && fg != this.Handle && Win32.IsWindowVisible(fg))
                {
                    _lastNonTrayForeground = fg;
                }
            }

            // SHAppBarMessage(ABM_GETSTATE) is a cross-process round trip to the shell. At 60 Hz
            // that was the biggest single cost of an idle instance, for a value that only changes
            // when the user edits taskbar settings, so it is probed twice per second.
            bool systemAutoHide = _cachedAutoHide;
            if ((DateTime.UtcNow - _lastAutoHideProbeAt).TotalMilliseconds >= 500)
            {
                systemAutoHide = GetTaskbarAutoHide();
                _cachedAutoHide = systemAutoHide;
                _lastAutoHideProbeAt = DateTime.UtcNow;
            }
            double msSinceUnpin = (DateTime.UtcNow - _unpinRequestedAt).TotalMilliseconds;

            // Sync state if user changed taskbar auto-hide in Windows Settings
            if (msSinceUnpin > 1000)
            {
                bool expectedAutoHide = !_isPinnedByApp;
                if (systemAutoHide != expectedAutoHide)
                {
                    _isPinnedByApp = !systemAutoHide;
                    _waitingForCollapse = false;
                    UpdateVisualState();
                }
            }

            UpdateWidgetPositionAndVisibility(false);

            // Only the collapse/drag animation needs 60 Hz. While nothing is moving a slower tick
            // keeps the widget glued to the taskbar (it is a child window, so it already follows
            // the taskbar's own motion) at a fraction of the CPU cost.
            int desiredInterval = (_waitingForCollapse || _isDragging) ? 16 : 50;
            if (_pollTimer.Interval != desiredInterval)
            {
                _pollTimer.Interval = desiredInterval;
            }
        }

        private void SetMouseThrough(bool enable)
        {
            if (_isMouseThrough == enable) return;
            _isMouseThrough = enable;
            if (!IsHandleCreated) return;
            int exStyle = Win32.GetWindowLong(this.Handle, Win32.GWL_EXSTYLE);
            if (enable)
                exStyle |= Win32.WS_EX_TRANSPARENT;
            else
                exStyle &= ~Win32.WS_EX_TRANSPARENT;
            Win32.SetWindowLong(this.Handle, Win32.GWL_EXSTYLE, exStyle);
        }

        private IntPtr TrayHwnd()
        {
            // Cache the handle: FindWindow() on every 16 ms tick is wasted work. IsWindow() keeps
            // this correct across an Explorer restart, and is far cheaper than a by-class search.
            if (_trayHwnd != IntPtr.Zero && Win32.IsWindow(_trayHwnd)) return _trayHwnd;
            _trayHwnd = Win32.FindWindow("Shell_TrayWnd", null);
            return _trayHwnd;
        }

        private void UpdateWidgetPositionAndVisibility(bool forceTopMost)
        {
            IntPtr tray = TrayHwnd();
            if (tray == IntPtr.Zero) return;

            // Parent the widget into the taskbar so it shares the taskbar's z-order:
            // covered by fullscreen apps exactly when the taskbar is covered, and slides with it.
            //
            // GetParent() is wrong here: the widget has no WS_CHILD style, so GetParent reports its
            // OWNER (always 0) and the comparison never matched. That made this call SetParent() on
            // every 16 ms tick, which relinks z-order/DWM state and was the top idle cost.
            if (IsHandleCreated && Win32.GetAncestor(this.Handle, Win32.GA_PARENT) != tray)
            {
                Win32.SetParent(this.Handle, tray);
            }

            Win32.RECT rTray;
            if (!Win32.GetWindowRect(tray, out rTray)) return;

            int screenW = Screen.PrimaryScreen.Bounds.Width;
            int screenH = Screen.PrimaryScreen.Bounds.Height;
            int trayHeight = Math.Max(32, rTray.bottom - rTray.top);

            IntPtr notify = Win32.FindWindowEx(tray, IntPtr.Zero, "TrayNotifyWnd", null);
            Win32.RECT rNotify;
            int anchorX = screenW - 220;
            if (notify != IntPtr.Zero && Win32.GetWindowRect(notify, out rNotify) && rNotify.left > 100)
            {
                anchorX = rNotify.left;
            }

            int targetX = anchorX + _offsetFromTrayNotifyLeft;
            if (targetX < 12) targetX = 12;
            if (targetX > screenW - _widgetSize - 12) targetX = screenW - _widgetSize - 12;

            // Taskbar is on-screen while its top edge is meaningfully above the screen bottom.
            bool taskbarCurrentlyShown = (rTray.top < screenH - 4);

            Win32.POINT cursorPt;
            Win32.GetCursorPos(out cursorPt);
            bool cursorAtBottomEdge = (cursorPt.y >= screenH - 3);

            if (!_isPinnedByApp && _waitingForCollapse)
            {
                double msSinceUnpin = (DateTime.UtcNow - _unpinRequestedAt).TotalMilliseconds;
                if (_collapseStage == 0 && msSinceUnpin >= 25)
                {
                    _collapseStage = 1;
                    // Stage 1: pure mouse touch-and-leave without focus-activating Shell_TrayWnd (fast ~100ms collapse)
                    TriggerNativeTaskbarCollapse(_lastNonTrayForeground, false);
                }
                else if (_collapseStage == 1 && msSinceUnpin >= 260)
                {
                    _collapseStage = 2;
                    // Stage 2 fallback: only if taskbar is STILL completely stationary at expandedTop after 260ms,
                    // pulse focus to guarantee collapse. (TriggerNativeTaskbarCollapse checks rTray.top <= expandedTop + 2)
                    TriggerNativeTaskbarCollapse(_lastNonTrayForeground, true);
                }

                if (!taskbarCurrentlyShown || (msSinceUnpin > 300 && cursorAtBottomEdge) || msSinceUnpin > 1100)
                {
                    _waitingForCollapse = false;
                }
            }

            // Show widget when pinned OR when taskbar is currently raised in auto-hide mode
            bool shouldShow = _isPinnedByApp || taskbarCurrentlyShown;

            if (!shouldShow)
            {
                if (this.Visible)
                {
                    this.Hide();
                }
                return;
            }

            // Follow real taskbar top position so the widget slides down/up in sync with the taskbar.
            int targetY = rTray.top + (trayHeight - _widgetSize) / 2;

            // Widget is mouse-through while unpinned/collapsing, interactive when pinned.
            SetMouseThrough(!_isPinnedByApp);

            // The widget lives inside Shell_TrayWnd as a child window, and a child's Location is
            // interpreted in the PARENT's client space, not on screen. Feeding it the screen-space
            // target offset it a second time by the taskbar rect, which on a bottom taskbar meant
            // the pin was rendered entirely off-screen (measured: y = 2049 on a 1600 px display).
            Point clientPos = ScreenToParentClient(targetX, targetY, tray);

            bool posChanged = (_lastRenderedX != clientPos.X || _lastRenderedY != clientPos.Y);

            if (!this.Visible)
            {
                this.Location = clientPos;
                this.Show();
                forceTopMost = true;
            }

            if (forceTopMost || posChanged)
            {
                RenderLayeredBitmap(clientPos.X, clientPos.Y);
            }
        }

        private static Point ScreenToParentClient(int screenX, int screenY, IntPtr parent)
        {
            Win32.POINT pt = new Win32.POINT();
            pt.x = screenX;
            pt.y = screenY;
            if (!Win32.ScreenToClient(parent, ref pt))
            {
                // The parent can disappear mid-tick (Explorer restart); fall back to the raw value
                // so the next tick, with a fresh handle, can place the widget correctly.
                return new Point(screenX, screenY);
            }
            return new Point(pt.x, pt.y);
        }

        public static bool GetTaskbarAutoHide()
        {
            Win32.APPBARDATA abd = new Win32.APPBARDATA();
            abd.cbSize = (uint)Marshal.SizeOf(typeof(Win32.APPBARDATA));
            abd.hWnd = Win32.FindWindow("Shell_TrayWnd", null);
            IntPtr state = Win32.SHAppBarMessage(Win32.ABM_GETSTATE, ref abd);
            return ((int)state & Win32.ABS_AUTOHIDE) != 0;
        }

        public static void SetTaskbarAutoHide(bool enableAutoHide)
        {
            Win32.APPBARDATA abd = new Win32.APPBARDATA();
            abd.cbSize = (uint)Marshal.SizeOf(typeof(Win32.APPBARDATA));
            abd.hWnd = Win32.FindWindow("Shell_TrayWnd", null);
            abd.lParam = (IntPtr)(enableAutoHide ? Win32.ABS_AUTOHIDE : Win32.ABS_ALWAYSONTOP);
            Win32.SHAppBarMessage(Win32.ABM_SETSTATE, ref abd);
        }

        private void ApplyTaskbarAutoHide(bool enableAutoHide)
        {
            SetTaskbarAutoHide(enableAutoHide);
            // Keep the cached copy in sync so the next poll does not undo a change we just made.
            _cachedAutoHide = enableAutoHide;
            _lastAutoHideProbeAt = DateTime.UtcNow;
        }

        private void UpdateVisualState()
        {
            string tip = _isPinnedByApp
                ? "任务栏已钉住 · 保持显示\n单击：恢复自动隐藏\n拖动：调整图标位置"
                : "任务栏自动隐藏\n单击系统托盘中的图钉：钉住任务栏";

            if (_toolTip != null) _toolTip.SetToolTip(this, tip);
            _menuStatus.Text = _isPinnedByApp ? "已钉住 · 任务栏保持显示" : "自动隐藏 · 随系统收起";
            _menuTogglePin.Checked = _isPinnedByApp;
            _menuTogglePin.Text = _isPinnedByApp ? "恢复自动隐藏(&P)" : "钉住任务栏(&P)";
            _menuAutoStart.Text = _menuAutoStart.Checked ? "开机启动（已开启）(&S)" : "开机启动(&S)";
            UpdateMenuImages();

            _trayIcon.Text = _isPinnedByApp ? "任务栏钉子：已钉住 (点击释放)" : "任务栏钉子：自动隐藏中 (点击钉住)";
            UpdateTrayIconImage();

            if (IsHandleCreated)
            {
                RenderLayeredBitmap();
            }
        }

        private void UpdateTrayIconImage()
        {
            int size = Scale(16); // Draw at the actual tray size, rather than letting Explorer shrink 32px.
            using (Bitmap bmp = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                    PinArtwork.DrawBadge(g, size, _isPinnedByApp, false, _taskbarPalette);

                // FromHandle borrows the HICON. Transfer the pair only after NotifyIcon accepts it;
                // on failure the old icon stays valid and the newly allocated handle is reclaimed.
                IntPtr handle = bmp.GetHicon();
                Icon replacement = null;
                try
                {
                    replacement = Icon.FromHandle(handle);
                    Icon previous = _trayIcon.Icon;
                    IntPtr previousHandle = _trayIconHandle;
                    _trayIcon.Icon = replacement;
                    _trayIconHandle = handle;
                    handle = IntPtr.Zero;
                    replacement = null;
                    if (previous != null) previous.Dispose();
                    if (previousHandle != IntPtr.Zero) Win32.DestroyIcon(previousHandle);
                }
                finally
                {
                    if (replacement != null) replacement.Dispose();
                    if (handle != IntPtr.Zero) Win32.DestroyIcon(handle);
                }
            }
        }

        private void ReleaseTrayIconHandle()
        {
            IntPtr handle = _trayIconHandle;
            _trayIconHandle = IntPtr.Zero;
            if (_trayIcon != null)
            {
                Icon image = _trayIcon.Icon;
                _trayIcon.Icon = null;
                if (image != null) image.Dispose();
            }
            if (handle != IntPtr.Zero) Win32.DestroyIcon(handle);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_resourcesDisposed)
            {
                _resourcesDisposed = true;
                if (_pollTimer != null)
                {
                    _pollTimer.Stop();
                    _pollTimer.Dispose();
                }
                if (_trayIcon != null)
                {
                    _trayIcon.Visible = false;
                    ReleaseTrayIconHandle();
                    _trayIcon.Dispose();
                }
                if (_contextMenu != null)
                {
                    foreach (ToolStripItem item in _contextMenu.Items)
                    {
                        Image image = item.Image;
                        item.Image = null;
                        if (image != null) image.Dispose();
                    }
                    _contextMenu.Dispose();
                }
                if (_menuFont != null) _menuFont.Dispose();
                if (_toolTip != null) _toolTip.Dispose();
                Icon = null;
                if (_applicationIcon != null) { _applicationIcon.Dispose(); _applicationIcon = null; }
            }
            base.Dispose(disposing);
        }

        private void RenderLayeredBitmap()
        {
            // Re-blit in place (hover state changed). Left/Top must NOT be used here: the widget is
            // re-parented into the taskbar but still carries popup styles, so Form.Left reports the
            // requested screen position while the layered blit uses parent-client coordinates -
            // mixing them threw the pin one taskbar height off screen. _lastRendered* already holds
            // the client-space origin of the last successful blit.
            if (_lastRenderedX == int.MinValue || _lastRenderedY == int.MinValue)
            {
                RenderLayeredBitmap(Left, Top);
            }
            else
            {
                RenderLayeredBitmap(_lastRenderedX, _lastRenderedY);
            }
        }

        private void RenderLayeredBitmap(int renderX, int renderY)
        {
            if (!IsHandleCreated) return;
            _lastRenderedX = renderX;
            _lastRenderedY = renderY;

            using (Bitmap bmp = new Bitmap(_widgetSize, _widgetSize, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    PinArtwork.DrawBadge(g, _widgetSize, _isPinnedByApp, _isHovered, _taskbarPalette);
                }

                IntPtr screenDc = Win32.GetDC(IntPtr.Zero);
                IntPtr memDc = Win32.CreateCompatibleDC(screenDc);
                IntPtr hBitmap = IntPtr.Zero;
                IntPtr oldBitmap = IntPtr.Zero;

                try
                {
                    hBitmap = bmp.GetHbitmap(Color.FromArgb(0));
                    oldBitmap = Win32.SelectObject(memDc, hBitmap);

                    Win32.SIZE size = new Win32.SIZE(_widgetSize, _widgetSize);
                    Win32.POINT pointSource = new Win32.POINT(0, 0);
                    Win32.POINT topPos = new Win32.POINT(renderX, renderY);
                    Win32.BLENDFUNCTION blend = new Win32.BLENDFUNCTION();
                    blend.BlendOp = Win32.AC_SRC_OVER;
                    blend.BlendFlags = 0;
                    blend.SourceConstantAlpha = 255;
                    blend.AlphaFormat = Win32.AC_SRC_ALPHA;

                    Win32.UpdateLayeredWindow(
                        this.Handle,
                        screenDc,
                        ref topPos,
                        ref size,
                        memDc,
                        ref pointSource,
                        0,
                        ref blend,
                        Win32.ULW_ALPHA);
                }
                finally
                {
                    if (oldBitmap != IntPtr.Zero) Win32.SelectObject(memDc, oldBitmap);
                    if (hBitmap != IntPtr.Zero) Win32.DeleteObject(hBitmap);
                    if (memDc != IntPtr.Zero) Win32.DeleteDC(memDc);
                    if (screenDc != IntPtr.Zero) Win32.ReleaseDC(IntPtr.Zero, screenDc);
                }
            }
        }

        // Kept as a stable entry point for the reflection tests; all artwork lives in PinArtwork.
        private static void DrawPinBadge(Graphics g, int size, bool isPinned, bool isHovered)
        {
            PinArtwork.DrawBadge(g, size, isPinned, isHovered, UiPalette.Read(true));
        }
    }

    static class Program
    {
        private static Mutex _mutex;

        [STAThread]
        static void Main(string[] args)
        {
            Win32.SetProcessDPIAware();

            // winexe has no console, so an unhandled exception used to make the widget vanish
            // silently. Route both handlers to debug.log (.gitignore already excludes it).
            AppDomain.CurrentDomain.UnhandledException += delegate(object s, UnhandledExceptionEventArgs e)
            {
                CrashLog.Write("AppDomain", e.ExceptionObject as Exception);
            };
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += delegate(object s, System.Threading.ThreadExceptionEventArgs e)
            {
                CrashLog.Write("UI thread", e.Exception);
            };

            if (args.Length > 0)
            {
                string cmd = args[0].ToLowerInvariant();
                if (cmd == "--pin")
                {
                    PinWidgetForm.SetTaskbarAutoHide(false);
                    return;
                }
                if (cmd == "--unpin")
                {
                    PinWidgetForm.SetTaskbarAutoHide(true);
                    PinWidgetForm.TriggerNativeTaskbarCollapse(Win32.GetShellWindow(), true);
                    return;
                }
            }

            bool createdNew;
            _mutex = new Mutex(true, "Local\\TaskbarPin_SingleInstance_Mutex", out createdNew);
            if (!createdNew)
            {
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new PinWidgetForm());
        }
    }

    /// <summary>
    /// Minimal crash/diagnostic log. The app is a winexe, so anything swallowed by an empty
    /// catch block used to disappear without a trace.
    /// </summary>
    static class CrashLog
    {
        private static readonly object Gate = new object();

        private static string LogPath()
        {
            return System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug.log");
        }

        public static void Write(string context, Exception ex)
        {
            try
            {
                string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " [" + context + "] "
                    + (ex == null ? "(no exception object)" : ex.ToString())
                    + Environment.NewLine;
                lock (Gate)
                {
                    System.IO.File.AppendAllText(LogPath(), line);
                }
            }
            catch
            {
                // Logging must never take the app down.
            }
        }
    }
}
