using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        public const int WS_EX_TOOLWINDOW = 0x00000080;
        public const int WS_EX_NOACTIVATE = 0x08000000;

        public const uint ABM_GETSTATE = 0x00000004;
        public const uint ABM_GETTASKBARPOS = 0x00000005;
        public const uint ABM_SETSTATE = 0x0000000A;
        public const int ABS_AUTOHIDE = 0x00000001;
        public const int ABS_ALWAYSONTOP = 0x00000002;

        public const uint MOUSEEVENTF_MOVE = 0x0001;

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
            public POINT(int x, int y) { this.x = x; this.y = y; }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
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
        public static extern bool SetProcessDPIAware();

        [DllImport("shell32.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        public static extern IntPtr GetCapture();

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
        public static extern bool IsHungAppWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder className, int maxCount);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern uint RegisterWindowMessage(string message);

        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentThreadId();

        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentProcessId();

        [DllImport("user32.dll")]
        public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("user32.dll")]
        public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

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
                return new UiPalette { HighContrast = true, Background = Color.FromArgb(SystemColors.Menu.ToArgb()),
                    Foreground = Color.FromArgb(SystemColors.MenuText.ToArgb()), Muted = Color.FromArgb(SystemColors.MenuText.ToArgb()),
                    Border = Color.FromArgb(SystemColors.WindowText.ToArgb()), Selection = Color.FromArgb(SystemColors.Highlight.ToArgb()),
                    Badge = Color.FromArgb(SystemColors.Window.ToArgb()), BadgeText = Color.FromArgb(SystemColors.WindowText.ToArgb()) };
            return light
                ? new UiPalette { Background = Color.FromArgb(250, 250, 250), Foreground = Color.FromArgb(30, 33, 38),
                    Muted = Color.FromArgb(89, 96, 107), Border = Color.FromArgb(215, 219, 225),
                    Selection = Color.FromArgb(232, 237, 244), Badge = Color.FromArgb(242, 245, 249), BadgeText = Color.FromArgb(62, 76, 96) }
                : new UiPalette { Background = Color.FromArgb(37, 39, 43), Foreground = Color.FromArgb(245, 246, 248),
                    Muted = Color.FromArgb(183, 190, 202), Border = Color.FromArgb(76, 81, 90),
                    Selection = Color.FromArgb(58, 63, 72), Badge = Color.FromArgb(47, 53, 63), BadgeText = Color.FromArgb(231, 236, 244) };
        }

        public bool Matches(UiPalette other)
        {
            return other != null && HighContrast == other.HighContrast && Background == other.Background &&
                Foreground == other.Foreground && Muted == other.Muted && Border == other.Border &&
                Selection == other.Selection && Badge == other.Badge && BadgeText == other.BadgeText;
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

    // Shared vector source for the Windows asset, tray and menu pin, on a 32px grid.
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

    enum TaskbarEdge { Left, Top, Right, Bottom }

    sealed class TaskbarGeometry
    {
        public Rectangle MonitorBounds { get; private set; }
        public Rectangle Bounds { get; private set; }
        public bool IsHorizontal { get; private set; }
        public TaskbarEdge Edge { get; private set; }
        private int Thickness { get { return IsHorizontal ? Bounds.Height : Bounds.Width; } }
        private int VisibleThickness
        {
            get
            {
                switch (Edge)
                {
                    case TaskbarEdge.Left: return Bounds.Right - MonitorBounds.Left;
                    case TaskbarEdge.Top: return Bounds.Bottom - MonitorBounds.Top;
                    case TaskbarEdge.Right: return MonitorBounds.Right - Bounds.Left;
                    default: return MonitorBounds.Bottom - Bounds.Top;
                }
            }
        }
        public bool IsShown { get { return VisibleThickness > Math.Min(4, Thickness - 1); } }

        public TaskbarGeometry(Rectangle monitor, Rectangle tray)
        {
            MonitorBounds = monitor;
            Bounds = tray;
            IsHorizontal = tray.Width >= tray.Height;
            Edge = IsHorizontal
                ? (Math.Abs(tray.Top - monitor.Top) <= Math.Abs(monitor.Bottom - tray.Bottom) ? TaskbarEdge.Top : TaskbarEdge.Bottom)
                : (Math.Abs(tray.Left - monitor.Left) <= Math.Abs(monitor.Right - tray.Right) ? TaskbarEdge.Left : TaskbarEdge.Right);
        }

        public static TaskbarGeometry Create(Rectangle animated, Rectangle docked, Rectangle[] monitors)
        {
            if (monitors == null || monitors.Length == 0) throw new ArgumentException("No display bounds available.", "monitors");
            Rectangle owner = monitors[0];
            long largestArea = -1;
            double nearestDistance = double.MaxValue;
            foreach (Rectangle monitor in monitors)
            {
                Rectangle overlap = Rectangle.Intersect(docked, monitor);
                long area = (long)Math.Max(0, overlap.Width) * Math.Max(0, overlap.Height);
                double centerX = docked.Left + docked.Width / 2.0;
                double centerY = docked.Top + docked.Height / 2.0;
                double distanceX = Math.Max(monitor.Left - centerX, Math.Max(0, centerX - monitor.Right));
                double distanceY = Math.Max(monitor.Top - centerY, Math.Max(0, centerY - monitor.Bottom));
                double distance = distanceX * distanceX + distanceY * distanceY;
                if (area > largestArea || (area == largestArea && distance < nearestDistance))
                {
                    largestArea = area;
                    nearestDistance = distance;
                    owner = monitor;
                }
            }
            return new TaskbarGeometry(owner, animated);
        }

        public bool IsCursorAtEdge(Point cursor)
        {
            if (!MonitorBounds.Contains(cursor)) return false;
            switch (Edge)
            {
                case TaskbarEdge.Left: return cursor.X < MonitorBounds.Left + 3;
                case TaskbarEdge.Top: return cursor.Y < MonitorBounds.Top + 3;
                case TaskbarEdge.Right: return cursor.X >= MonitorBounds.Right - 3;
                default: return cursor.Y >= MonitorBounds.Bottom - 3;
            }
        }

        public bool GetCollapsePoints(Point cursor, out Point touch, out Point exit)
        {
            touch = exit = cursor;
            Rectangle visible = Rectangle.Intersect(Bounds, MonitorBounds);
            if (VisibleThickness < Thickness - 2 || visible.Width <= 0 || visible.Height <= 0) return false;
            int touchX = Math.Max(visible.Left, Math.Min(visible.Right - 1, cursor.X));
            int touchY = Math.Max(visible.Top, Math.Min(visible.Bottom - 1, cursor.Y));
            switch (Edge)
            {
                case TaskbarEdge.Left:
                    touchX = Math.Max(visible.Left, visible.Right - 11);
                    if (Bounds.Contains(cursor)) exit.X = Math.Min(MonitorBounds.Right - 1, Bounds.Right + 15);
                    break;
                case TaskbarEdge.Top:
                    touchY = Math.Max(visible.Top, visible.Bottom - 11);
                    if (Bounds.Contains(cursor)) exit.Y = Math.Min(MonitorBounds.Bottom - 1, Bounds.Bottom + 15);
                    break;
                case TaskbarEdge.Right:
                    touchX = Math.Min(visible.Right - 1, visible.Left + 10);
                    if (Bounds.Contains(cursor)) exit.X = Math.Max(MonitorBounds.Left, Bounds.Left - 15);
                    break;
                default:
                    touchY = Math.Min(visible.Bottom - 1, visible.Top + 10);
                    if (Bounds.Contains(cursor)) exit.Y = Math.Max(MonitorBounds.Top, Bounds.Top - 15);
                    break;
            }
            touch = new Point(touchX, touchY);
            return true;
        }
    }

    public class PinTrayForm : Form
    {
        private readonly System.Windows.Forms.Timer _pollTimer;
        private readonly NotifyIcon _trayIcon;
        private readonly ContextMenuStrip _contextMenu;
        private readonly ToolStripMenuItem _menuTogglePin;
        private readonly ToolStripMenuItem _menuAutoStart;
        private readonly ToolStripMenuItem _menuStatus;
        private readonly ToolStripMenuItem _menuExit;
        private Font _menuFont;
        private float _uiScale;
        private readonly Func<float> _readScale = ReadScale;
        private UiPalette _menuPalette;
        private UiPalette _taskbarPalette;
        private bool? _visualPinned;
        private bool _visualAutoStart;
        private bool _resourcesDisposed;

        private bool _isPinnedByApp;

        // State tracking for instant, bounce-free taskbar collapse
        private IntPtr _lastNonTrayForeground = IntPtr.Zero;
        private bool _waitingForCollapse = false;
        private DateTime _unpinRequestedAt = DateTime.MinValue;
        private int _collapseStage = 0;

        // Native icon currently shown in the tray. Bitmap.GetHicon() allocates 1 USER + 3 GDI
        // handles that only DestroyIcon() frees, so every replaced icon must be destroyed.
        private IntPtr _trayIconHandle = IntPtr.Zero;

        // Cached Shell_TrayWnd handle; FindWindow at 60 Hz is wasted work.
        private IntPtr _trayHwnd = IntPtr.Zero;
        private static IntPtr _positionTrayHwnd;
        private static Rectangle _dockedTaskbarBounds;
        private static DateTime _lastTaskbarPositionProbeAt = DateTime.MinValue;

        private readonly Func<bool?> _readAutoHide = ReadAutoHide;
        private DateTime _lastAutoHideProbeAt = DateTime.MinValue;

        private static readonly uint TaskbarCreatedMessage = Win32.RegisterWindowMessage("TaskbarCreated");

        // NotifyIcon raises MouseClick twice for a double click; debounce the toggle.
        private DateTime _lastTrayClickUtc = DateTime.MinValue;

        public PinTrayForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;

            _taskbarPalette = UiPalette.Read(true);
            _contextMenu = new ContextMenuStrip();
            _contextMenu.ShowImageMargin = true;
            _contextMenu.ShowCheckMargin = false;
            _menuStatus = new ToolStripMenuItem { Enabled = false, AccessibleRole = AccessibleRole.StaticText };
            _menuTogglePin = new ToolStripMenuItem("钉住任务栏(&P)", null, OnMenuTogglePin) { Tag = "pin" };
            _menuAutoStart = new ToolStripMenuItem("开机启动(&S)", null, OnMenuAutoStart)
            {
                Checked = IsAutoStartEnabled(), Tag = "start"
            };
            _menuExit = new ToolStripMenuItem("退出并恢复自动隐藏(&X)", null, OnMenuExit) { Tag = "exit" };

            _contextMenu.Items.Add(_menuStatus);
            _contextMenu.Items.Add(new ToolStripSeparator());
            _contextMenu.Items.Add(_menuTogglePin);
            _contextMenu.Items.Add(_menuAutoStart);
            _contextMenu.Items.Add(new ToolStripSeparator());
            _contextMenu.Items.Add(_menuExit);
            RefreshScale();
            _contextMenu.Opening += delegate
            {
                // The menu may have been closed while Windows Settings or another instance changed state.
                SyncAutoHide();
                _menuAutoStart.Checked = IsAutoStartEnabled();
                RefreshAppearance();
                UpdateVisualState();
            };
            ApplyMenuTheme(UiPalette.Read(false));

            _trayIcon = new NotifyIcon();
            _trayIcon.ContextMenuStrip = _contextMenu;
            _trayIcon.Visible = true;
            _trayIcon.MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    // A double click arrives as two MouseClick events; without this debounce the
                    // taskbar pins and immediately unpins again.
                    if ((DateTime.UtcNow - _lastTrayClickUtc).TotalMilliseconds < 350) return;
                    _lastTrayClickUtc = DateTime.UtcNow;
                    TogglePinState();
                }
            };

            bool? initialAutoHide = _readAutoHide() ?? _readAutoHide();
            _lastAutoHideProbeAt = DateTime.UtcNow;
            _isPinnedByApp = initialAutoHide == false;

            // Create the handle now so BeginInvoke and system broadcasts work while the form stays hidden.
            IntPtr dummyHandle = this.Handle;

            UpdateVisualState();

            _pollTimer = new System.Windows.Forms.Timer();
            _pollTimer.Interval = 50;
            _pollTimer.Tick += PollTimer_Tick;
            _pollTimer.Start();
        }

        private bool IsAutoStartEnabled()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false))
                {
                    return key != null && MatchesAutoStartPath(key.GetValue("TaskbarPin") as string, Application.ExecutablePath);
                }
            }
            catch (Exception ex) { CrashLog.Write("ReadAutostart", ex); return false; }
        }

        private static bool MatchesAutoStartPath(string command, string executable)
        {
            if (string.IsNullOrWhiteSpace(command)) return false;
            command = command.Trim();
            if (command.Length >= 2 && command[0] == '"' && command[command.Length - 1] == '"')
                command = command.Substring(1, command.Length - 2);
            return string.Equals(command, executable, StringComparison.OrdinalIgnoreCase);
        }

        private void OnMenuAutoStart(object sender, EventArgs e)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))
                {
                    if (key == null) return;
                    _menuAutoStart.Checked = ToggleAutoStart(key, Application.ExecutablePath);
                }
                UpdateVisualState();
            }
            catch (Exception ex) { CrashLog.Write("Autostart", ex); }
        }

        private static bool ToggleAutoStart(RegistryKey key, string executable)
        {
            if (MatchesAutoStartPath(key.GetValue("TaskbarPin") as string, executable))
            {
                key.DeleteValue("TaskbarPin", false);
                return false;
            }
            key.SetValue("TaskbarPin", "\"" + executable + "\"");
            return true;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= Win32.WS_EX_TOOLWINDOW | Win32.WS_EX_NOACTIVATE;
                return cp;
            }
        }

        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(false);
        }

        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x001A || m.Msg == 0x007E) _lastTaskbarPositionProbeAt = DateTime.MinValue;
            if (TaskbarCreatedMessage != 0 && (uint)m.Msg == TaskbarCreatedMessage)
            {
                OnTaskbarCreated();
                return;
            }
            base.WndProc(ref m);
            if (m.Msg == 0x031A || m.Msg == 0x001A || m.Msg == 0x007E) // theme, settings, display
                RefreshAppearance();
        }

        private void OnTaskbarCreated()
        {
            if (_resourcesDisposed || _trayIcon == null) return;
            _trayHwnd = IntPtr.Zero;
            _positionTrayHwnd = IntPtr.Zero;
            _dockedTaskbarBounds = Rectangle.Empty;
            _lastTaskbarPositionProbeAt = DateTime.MinValue;
            _lastAutoHideProbeAt = DateTime.MinValue;
            SyncAutoHide();
            RefreshAppearance();
            UpdateVisualState();
            _trayIcon.Visible = false;
            _trayIcon.Visible = true;
        }

        private int Scale(int value) { return Math.Max(1, (int)Math.Round(value * _uiScale)); }

        private static float ReadScale()
        {
            using (Graphics screen = Graphics.FromHwnd(IntPtr.Zero)) return screen.DpiX / 96f;
        }

        private bool RefreshScale()
        {
            float scale = _readScale();
            bool changed = scale != _uiScale;
            _uiScale = scale;
            using (Font systemMenuFont = SystemFonts.MenuFont)
            {
                if (changed || !systemMenuFont.Equals(_menuFont))
                {
                    Font previous = _menuFont;
                    _menuFont = (Font)systemMenuFont.Clone();
                    _contextMenu.Font = _menuFont;
                    if (previous != null) previous.Dispose();
                }
            }
            if (!changed) return false;
            _contextMenu.ImageScalingSize = new Size(Scale(18), Scale(18));
            _contextMenu.Padding = new Padding(Scale(4), Scale(6), Scale(4), Scale(6));
            foreach (ToolStripItem item in _contextMenu.Items)
                if (item is ToolStripMenuItem) item.Padding = new Padding(Scale(6), Scale(7), Scale(12), Scale(7));
            return true;
        }

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
            bool scaleChanged = RefreshScale();
            UiPalette menu = UiPalette.Read(false);
            UiPalette taskbar = UiPalette.Read(true);
            bool taskbarChanged = !_taskbarPalette.Matches(taskbar);
            if (!_menuPalette.Matches(menu)) ApplyMenuTheme(menu);
            else if (scaleChanged) UpdateMenuImages();
            if (!scaleChanged && !taskbarChanged) return;
            _taskbarPalette = taskbar;
            UpdateTrayIconImage();
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
            if (_isPinnedByApp)
            {
                _waitingForCollapse = false;
                _collapseStage = 0;
                // Pin taskbar: disable auto-hide (ABS_ALWAYSONTOP)
                ApplyTaskbarAutoHide(false);
                UpdateVisualState();
            }
            else
            {
                // Unpin taskbar: enable auto-hide (ABS_AUTOHIDE) and immediately trigger collapse
                UpdateVisualState();
                _waitingForCollapse = true;
                _unpinRequestedAt = DateTime.UtcNow;
                _collapseStage = 0;

                ApplyTaskbarAutoHide(true);
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
            bool attached = fgThread != 0 && fgThread != curThread && Win32.AttachThreadInput(curThread, fgThread, true);
            try
            {
                Win32.keybd_event(0, 0, 0, UIntPtr.Zero);
                Win32.SetForegroundWindow(targetHwnd);
            }
            finally
            {
                if (attached) Win32.AttachThreadInput(curThread, fgThread, false);
            }
        }

        private static bool CoversDisplay(IntPtr window)
        {
            if (window == IntPtr.Zero || window == Win32.GetShellWindow()) return false;
            Win32.RECT bounds;
            if (!Win32.GetWindowRect(window, out bounds)) return false;
            return Rectangle.FromLTRB(bounds.left, bounds.top, bounds.right, bounds.bottom).Contains(Screen.FromHandle(window).Bounds);
        }

        private static void PulseTrayFocus(IntPtr tray, IntPtr target, Action<IntPtr> focus, Func<IntPtr, bool> coversDisplay)
        {
            if (target == IntPtr.Zero || coversDisplay(target)) return;
            focus(tray);
            focus(target);
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
            if (targetFg != IntPtr.Zero && !CoversDisplay(targetFg))
            {
                ForceForegroundWindow(targetFg);
            }
        }

        public static void TriggerNativeTaskbarCollapse(IntPtr preferredFallbackFg, bool includeFocusPulse)
        {
            if (Win32.GetCapture() != IntPtr.Zero) return;
            IntPtr tray = Win32.FindWindow("Shell_TrayWnd", null);
            if (tray == IntPtr.Zero) return;

            TaskbarGeometry geometry = ReadTaskbarGeometry(tray, true);
            if (geometry == null) return;

            Win32.POINT origPt;
            if (!Win32.GetCursorPos(out origPt)) return;

            Point touch, exit;
            if (!geometry.GetCollapsePoints(new Point(origPt.x, origPt.y), out touch, out exit)) return;

            if (!Win32.SetCursorPos(touch.X, touch.Y)) return;
            try
            {
                Win32.mouse_event(Win32.MOUSEEVENTF_MOVE, 0, 1, 0, UIntPtr.Zero);
                Thread.Sleep(20);
                Win32.mouse_event(Win32.MOUSEEVENTF_MOVE, 0, -1, 0, UIntPtr.Zero);
            }
            finally { Win32.SetCursorPos(exit.X, exit.Y); }

            if (includeFocusPulse)
            {
                IntPtr targetFg = Win32.GetForegroundWindow();
                if (targetFg == IntPtr.Zero || targetFg == tray || !Win32.IsWindowVisible(targetFg))
                {
                    targetFg = (preferredFallbackFg != IntPtr.Zero && Win32.IsWindow(preferredFallbackFg) && Win32.IsWindowVisible(preferredFallbackFg) && preferredFallbackFg != tray)
                        ? preferredFallbackFg
                        : Win32.GetShellWindow();
                }
                PulseTrayFocus(tray, targetFg, ForceForegroundWindow, CoversDisplay);
            }
            else
            {
                ReleaseTrayFocusIfNeeded(tray, preferredFallbackFg);
            }
        }

        private void PollTimer_Tick(object sender, EventArgs e)
        {
            try { PollTaskbar(); }
            catch (Exception ex) { CrashLog.Write("PollTimer", ex); }
        }

        private void SyncAutoHide()
        {
            _lastAutoHideProbeAt = DateTime.UtcNow;
            bool? observed = _readAutoHide();
            if (!observed.HasValue || observed.Value == !_isPinnedByApp ||
                (_lastAutoHideProbeAt - _unpinRequestedAt).TotalMilliseconds <= 1000) return;
            _isPinnedByApp = !observed.Value;
            EndCollapse();
        }

        private void PollTaskbar()
        {
            ExpireCollapse();
            IntPtr tray = TrayHwnd();
            if (tray != IntPtr.Zero)
            {
                IntPtr fg = Win32.GetForegroundWindow();
                if (fg != IntPtr.Zero && fg != tray && fg != this.Handle && Win32.IsWindowVisible(fg) && IsForeignAppWindow(fg))
                {
                    _lastNonTrayForeground = fg;
                }
            }

            // SHAppBarMessage(ABM_GETSTATE) is a cross-process round trip to the shell. At 60 Hz
            // that was the biggest single cost of an idle instance, for a value that only changes
            // when the user edits taskbar settings, so it is probed twice per second.
            if ((DateTime.UtcNow - _lastAutoHideProbeAt).TotalMilliseconds >= 500) SyncAutoHide();
            if (_visualPinned != _isPinnedByApp) UpdateVisualState();

            if (!_isPinnedByApp && _waitingForCollapse && tray != IntPtr.Zero)
            {
                TaskbarGeometry geometry = ReadTaskbarGeometry(tray);
                if (geometry != null)
                {
                    double msSinceUnpin = (DateTime.UtcNow - _unpinRequestedAt).TotalMilliseconds;
                    Win32.POINT cursorPt;
                    bool cursorAtEdge = Win32.GetCursorPos(out cursorPt) && geometry.IsCursorAtEdge(new Point(cursorPt.x, cursorPt.y));
                    if (!geometry.IsShown || (msSinceUnpin > 300 && cursorAtEdge))
                    {
                        EndCollapse();
                    }
                    else if (_collapseStage == 0 && msSinceUnpin >= 25)
                    {
                        _collapseStage = 1;
                        TriggerNativeTaskbarCollapse(_lastNonTrayForeground, false);
                    }
                    else if (_collapseStage == 1 && msSinceUnpin >= 260)
                    {
                        _collapseStage = 2;
                        TriggerNativeTaskbarCollapse(_lastNonTrayForeground, true);
                    }
                }
            }

            // Only the collapse animation needs 60 Hz.
            int desiredInterval = _waitingForCollapse ? 16 : 50;
            if (_pollTimer.Interval != desiredInterval)
            {
                _pollTimer.Interval = desiredInterval;
            }
        }

        private static bool IsForeignAppWindow(IntPtr window)
        {
            uint processId;
            Win32.GetWindowThreadProcessId(window, out processId);
            if (processId == 0 || processId == Win32.GetCurrentProcessId()) return false;
            System.Text.StringBuilder name = new System.Text.StringBuilder(256);
            if (Win32.GetClassName(window, name, name.Capacity) == 0) return true;
            string className = name.ToString();
            return className != "Shell_TrayWnd" && className != "Shell_SecondaryTrayWnd" &&
                className != "NotifyIconOverflowWindow" && className != "TopLevelWindowForOverflowXamlIsland";
        }

        private void EndCollapse()
        {
            _waitingForCollapse = false;
            _collapseStage = 0;
        }

        private void ExpireCollapse()
        {
            if (_waitingForCollapse && (DateTime.UtcNow - _unpinRequestedAt).TotalMilliseconds >= 1100)
                EndCollapse();
        }

        private IntPtr TrayHwnd()
        {
            // Cache the handle: FindWindow() on every 16 ms tick is wasted work. IsWindow() keeps
            // this correct across an Explorer restart, and is far cheaper than a by-class search.
            if (_trayHwnd != IntPtr.Zero && Win32.IsWindow(_trayHwnd)) return _trayHwnd;
            _trayHwnd = Win32.FindWindow("Shell_TrayWnd", null);
            return _trayHwnd;
        }

        private static TaskbarGeometry ReadTaskbarGeometry(IntPtr tray, bool refresh = false)
        {
            Win32.RECT bounds;
            if (tray == IntPtr.Zero || !Win32.GetWindowRect(tray, out bounds) || bounds.right <= bounds.left || bounds.bottom <= bounds.top) return null;
            Rectangle animated = Rectangle.FromLTRB(bounds.left, bounds.top, bounds.right, bounds.bottom);
            if (_positionTrayHwnd != tray)
            {
                _positionTrayHwnd = tray;
                _dockedTaskbarBounds = Rectangle.Empty;
                refresh = true;
            }
            if (refresh || (DateTime.UtcNow - _lastTaskbarPositionProbeAt).TotalMilliseconds >= 500)
            {
                Win32.APPBARDATA data = new Win32.APPBARDATA();
                data.cbSize = (uint)Marshal.SizeOf(typeof(Win32.APPBARDATA));
                data.hWnd = tray;
                if (Win32.SHAppBarMessage(Win32.ABM_GETTASKBARPOS, ref data) != IntPtr.Zero && data.rc.right > data.rc.left && data.rc.bottom > data.rc.top)
                    _dockedTaskbarBounds = Rectangle.FromLTRB(data.rc.left, data.rc.top, data.rc.right, data.rc.bottom);
                _lastTaskbarPositionProbeAt = DateTime.UtcNow;
            }
            Screen[] screens = Screen.AllScreens;
            Rectangle[] monitors = new Rectangle[screens.Length];
            for (int screenIndex = 0; screenIndex < screens.Length; screenIndex++) monitors[screenIndex] = screens[screenIndex].Bounds;
            return TaskbarGeometry.Create(animated, _dockedTaskbarBounds.IsEmpty ? animated : _dockedTaskbarBounds, monitors);
        }

        public static bool GetTaskbarAutoHide()
        {
            bool autoHide;
            return TryGetTaskbarAutoHide(out autoHide) && autoHide;
        }

        private static bool? ReadAutoHide()
        {
            bool autoHide;
            return TryGetTaskbarAutoHide(out autoHide) ? (bool?)autoHide : null;
        }

        public static bool TryGetTaskbarAutoHide(out bool autoHide)
        {
            autoHide = false;
            IntPtr tray = Win32.FindWindow("Shell_TrayWnd", null);
            if (tray == IntPtr.Zero || Win32.IsHungAppWindow(tray)) return false;
            Win32.APPBARDATA abd = new Win32.APPBARDATA();
            abd.cbSize = (uint)Marshal.SizeOf(typeof(Win32.APPBARDATA));
            abd.hWnd = tray;
            IntPtr state = Win32.SHAppBarMessage(Win32.ABM_GETSTATE, ref abd);
            if (state == IntPtr.Zero && Win32.SHAppBarMessage(Win32.ABM_GETTASKBARPOS, ref abd) == IntPtr.Zero)
                return false;
            if (!Win32.IsWindow(tray)) return false;
            autoHide = (state.ToInt64() & Win32.ABS_AUTOHIDE) != 0;
            return true;
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
            _lastAutoHideProbeAt = DateTime.UtcNow;
        }

        private void UpdateVisualState()
        {
            bool pinChanged = _visualPinned != _isPinnedByApp;
            bool autoStart = _menuAutoStart.Checked;
            if (!pinChanged && _visualAutoStart == autoStart) return;
            _menuAutoStart.Text = autoStart ? "开机启动（已开启）(&S)" : "开机启动(&S)";
            if (!pinChanged)
            {
                UpdateMenuImages();
                _visualAutoStart = autoStart;
                return;
            }
            _menuStatus.Text = _isPinnedByApp ? "已钉住 · 任务栏保持显示" : "自动隐藏 · 随系统收起";
            _menuTogglePin.Checked = _isPinnedByApp;
            _menuTogglePin.Text = _isPinnedByApp ? "恢复自动隐藏(&P)" : "钉住任务栏(&P)";
            UpdateMenuImages();

            _trayIcon.Text = _isPinnedByApp ? "任务栏钉子：已钉住 (点击释放)" : "任务栏钉子：自动隐藏中 (点击钉住)";
            UpdateTrayIconImage();

            _visualPinned = _isPinnedByApp;
            _visualAutoStart = autoStart;
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
            }
            base.Dispose(disposing);
        }

    }

    sealed class PinApplicationContext : ApplicationContext
    {
        private readonly PinTrayForm _widget;

        public PinApplicationContext()
        {
            _widget = new PinTrayForm();
            _widget.FormClosed += delegate { ExitThread(); };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _widget.Dispose();
            base.Dispose(disposing);
        }
    }

    static class Program
    {
        [STAThread]
        static int Main(string[] args)
        {
            Win32.SetProcessDPIAware();

            // winexe has no console, so an unhandled exception used to make the application vanish
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

            try { return Run(args); }
            catch (Exception ex)
            {
                CrashLog.Write("Startup", ex);
                return 1;
            }
        }

        private static int Run(string[] args)
        {
            int? exitCode = RunCommandLine(args, PinTrayForm.SetTaskbarAutoHide,
                delegate { PinTrayForm.TriggerNativeTaskbarCollapse(Win32.GetShellWindow(), true); });
            if (exitCode.HasValue) return exitCode.Value;

            bool createdNew;
            using (Mutex mutex = new Mutex(true, "Local\\TaskbarPin_SingleInstance_Mutex", out createdNew))
            {
                if (!createdNew) return 0;
                try
                {
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    using (PinApplicationContext context = new PinApplicationContext())
                        Application.Run(context);
                }
                finally { mutex.ReleaseMutex(); }
            }
            return 0;
        }

        private static int? RunCommandLine(string[] args, Action<bool> setAutoHide, Action collapse)
        {
            if (args.Length == 0) return null;
            if (args.Length == 1 && string.Equals(args[0], "--pin", StringComparison.OrdinalIgnoreCase))
            {
                setAutoHide(false);
                return 0;
            }
            if (args.Length == 1 && string.Equals(args[0], "--unpin", StringComparison.OrdinalIgnoreCase))
            {
                setAutoHide(true);
                collapse();
                return 0;
            }
            CrashLog.Write("CommandLine", new ArgumentException("Usage: TaskbarPin.exe [--pin | --unpin]"));
            return 2;
        }
    }

    static class AppStorage
    {
        private static readonly string PortableDirectory = AppDomain.CurrentDomain.BaseDirectory;
        private static readonly Lazy<string> LocalDirectory = new Lazy<string>(delegate
        {
            string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return string.IsNullOrEmpty(local) ? PortableDirectory : Path.Combine(local, "TaskbarPin");
        });
        private static readonly Lazy<string> DirectoryChoice = new Lazy<string>(delegate
        {
            return ResolveDirectory(PortableDirectory, LocalDirectory.Value);
        });

        public static string DirectoryPath { get { return DirectoryChoice.Value; } }

        internal static string ResolveDirectory(string portable, string fallback)
        {
            if (CanWrite(portable)) return portable;
            CanWrite(fallback);
            return fallback;
        }

        private static bool CanWrite(string directory)
        {
            try
            {
                Directory.CreateDirectory(directory);
                string log = Path.Combine(directory, "debug.log");
                if (File.Exists(log) && (File.GetAttributes(log) & FileAttributes.ReadOnly) != 0) return false;
                string probe = Path.Combine(directory, "TaskbarPin-" + Guid.NewGuid().ToString("N") + ".tmp");
                using (FileStream stream = new FileStream(probe, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.DeleteOnClose))
                    stream.WriteByte(0);
                return true;
            }
            catch (IOException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
            catch (System.Security.SecurityException) { return false; }
        }

    }

    /// <summary>
    /// Minimal crash/diagnostic log. The app is a winexe, so anything swallowed by an empty
    /// catch block used to disappear without a trace.
    /// </summary>
    static class CrashLog
    {
        private const int MaxLogBytes = 256 * 1024;
        private const int MaxRecentErrors = 128;
        private static readonly object Gate = new object();
        private static readonly Dictionary<string, long> RecentErrors = new Dictionary<string, long>();
        private static readonly System.Diagnostics.Stopwatch Clock = System.Diagnostics.Stopwatch.StartNew();

        public static void Write(string context, Exception ex)
        {
            try
            {
                string key = context + "\n" + (ex == null ? "" : ex.GetType().FullName + "\n" + ex.Message);
                if (key.Length > 2048) key = key.Substring(0, 2048);
                lock (Gate)
                {
                    long now = Clock.ElapsedMilliseconds;
                    long previous;
                    if (RecentErrors.TryGetValue(key, out previous) && now - previous < 60000) return;
                    if (RecentErrors.Count >= MaxRecentErrors)
                    {
                        foreach (string recent in new List<string>(RecentErrors.Keys))
                            if (now - RecentErrors[recent] >= 60000) RecentErrors.Remove(recent);
                        if (RecentErrors.Count >= MaxRecentErrors) return;
                    }
                    RecentErrors[key] = now;

                    string details = ex == null ? "(no exception object)" : ex.ToString();
                    if (details.Length > 16384) details = details.Substring(0, 16384);
                    string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " [" + context + "] " + details + Environment.NewLine;
                    byte[] content = System.Text.Encoding.UTF8.GetBytes(line);
                    string path = Path.Combine(AppStorage.DirectoryPath, "debug.log");
                    FileInfo existing = new FileInfo(path);
                    if (existing.Exists && existing.Length + content.Length > MaxLogBytes)
                    {
                        string archive = path + ".1";
                        File.Delete(archive);
                        if (existing.Length > MaxLogBytes) File.Delete(path);
                        else File.Move(path, archive);
                    }
                    using (FileStream stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read))
                        stream.Write(content, 0, content.Length);
                }
            }
            catch
            {
                // Logging must never take the app down.
            }
        }
    }
}
