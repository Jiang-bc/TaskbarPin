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

    public class PinWidgetForm : Form
    {
        private readonly System.Windows.Forms.Timer _pollTimer;
        private readonly NotifyIcon _trayIcon;
        private readonly ContextMenuStrip _contextMenu;
        private readonly ToolStripMenuItem _menuTogglePin;
        private readonly ToolStripMenuItem _menuAutoStart;
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

            _contextMenu = new ContextMenuStrip();
            _menuTogglePin = new ToolStripMenuItem("钉住任务栏 (禁止自动隐藏)", null, OnMenuTogglePin);
            var menuResetPos = new ToolStripMenuItem("重置小图标位置", null, (s, e) =>
            {
                _offsetFromTrayNotifyLeft = -48;
                SaveConfig();
                UpdateWidgetPositionAndVisibility(true);
            });
            _menuAutoStart = new ToolStripMenuItem("开机自动启动", null, OnMenuAutoStart)
            {
                Checked = IsAutoStartEnabled()
            };
            var menuExit = new ToolStripMenuItem("退出 (恢复任务栏自动隐藏)", null, OnMenuExit);

            _contextMenu.Items.Add(_menuTogglePin);
            _contextMenu.Items.Add(menuResetPos);
            _contextMenu.Items.Add(_menuAutoStart);
            _contextMenu.Items.Add(new ToolStripSeparator());
            _contextMenu.Items.Add(menuExit);

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
            _trayIcon.Dispose();
            ReleaseTrayIconHandle();
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
                ? "任务栏已钉住（不会自动隐藏）\n左键点击：取消钉住，恢复自动隐藏\n按住左键拖动：调整水平位置"
                : "任务栏未钉住（自动隐藏已开启）\n左键点击：钉住任务栏" ;

            if (_toolTip != null)
            {
                _toolTip.SetToolTip(this, tip);
            }

            _menuTogglePin.Checked = _isPinnedByApp;
            _menuTogglePin.Text = _isPinnedByApp ? "取消钉住（恢复任务栏自动隐藏）" : "钉住任务栏（禁止自动隐藏）";

            _trayIcon.Text = _isPinnedByApp ? "任务栏钉子：已钉住 (点击释放)" : "任务栏钉子：自动隐藏中 (点击钉住)";
            UpdateTrayIconImage();

            if (IsHandleCreated)
            {
                RenderLayeredBitmap();
            }
        }

        private void UpdateTrayIconImage()
        {
            using (Bitmap bmp = new Bitmap(32, 32, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    DrawPinBadge(g, 32, _isPinnedByApp, false);
                }

                // Icon.FromHandle() wraps an HICON without taking ownership, so disposing the Icon
                // does not free the native handle: that used to leak 1 USER + 3 GDI handles per
                // call (~3300 status refreshes would exhaust the 10000 GDI handle limit). Keep the
                // HICON and DestroyIcon() the one we replace.
                Icon previousIcon = _trayIcon.Icon;
                IntPtr previousHandle = _trayIconHandle;

                IntPtr hIcon = bmp.GetHicon();
                _trayIcon.Icon = Icon.FromHandle(hIcon);
                _trayIconHandle = hIcon;

                if (previousIcon != null)
                {
                    previousIcon.Dispose();
                }
                if (previousHandle != IntPtr.Zero)
                {
                    Win32.DestroyIcon(previousHandle);
                }
            }
        }

        private void ReleaseTrayIconHandle()
        {
            IntPtr handle = _trayIconHandle;
            _trayIconHandle = IntPtr.Zero;
            if (handle != IntPtr.Zero)
            {
                Win32.DestroyIcon(handle);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_pollTimer != null)
                {
                    _pollTimer.Stop();
                    _pollTimer.Dispose();
                }
                if (_trayIcon != null)
                {
                    _trayIcon.Visible = false;
                    _trayIcon.Dispose();
                }
                if (_contextMenu != null) _contextMenu.Dispose();
                if (_toolTip != null) _toolTip.Dispose();
                ReleaseTrayIconHandle();
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
                    DrawPinBadge(g, _widgetSize, _isPinnedByApp, _isHovered);
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

        private static void DrawPinBadge(Graphics g, int size, bool isPinned, bool isHovered)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.Clear(Color.Transparent);

            float pad = 2.5f;
            RectangleF badgeRect = new RectangleF(pad, pad, size - pad * 2, size - pad * 2);

            using (GraphicsPath badgePath = CreateRoundedRect(badgeRect, 10f))
            {
                if (isPinned)
                {
                    Color c1 = isHovered ? Color.FromArgb(245, 59, 130, 246) : Color.FromArgb(235, 37, 99, 235);
                    Color c2 = isHovered ? Color.FromArgb(245, 29, 78, 216) : Color.FromArgb(235, 30, 64, 175);
                    using (LinearGradientBrush bg = new LinearGradientBrush(badgeRect, c1, c2, 45f))
                    {
                        g.FillPath(bg, badgePath);
                    }
                    using (Pen borderPen = new Pen(Color.FromArgb(200, 147, 197, 253), 1.3f))
                    {
                        g.DrawPath(borderPen, badgePath);
                    }
                }
                else
                {
                    Color c1 = isHovered ? Color.FromArgb(230, 255, 255, 255) : Color.FromArgb(190, 248, 250, 252);
                    Color c2 = isHovered ? Color.FromArgb(230, 226, 232, 240) : Color.FromArgb(190, 203, 213, 225);
                    using (LinearGradientBrush bg = new LinearGradientBrush(badgeRect, c1, c2, 45f))
                    {
                        g.FillPath(bg, badgePath);
                    }
                    using (Pen borderPen = new Pen(Color.FromArgb(160, 100, 116, 139), 1.2f))
                    {
                        g.DrawPath(borderPen, badgePath);
                    }
                }
            }

            GraphicsState state = g.Save();
            g.TranslateTransform(size / 2f, size / 2f);
            if (!isPinned)
            {
                g.RotateTransform(38f);
            }
            float s = size / 38f;
            g.ScaleTransform(s, s);

            Color pinMain = isPinned ? Color.White : Color.FromArgb(230, 51, 65, 85);
            Color pinShade = isPinned ? Color.FromArgb(220, 219, 234, 254) : Color.FromArgb(230, 100, 116, 139);

            using (SolidBrush bMain = new SolidBrush(pinMain))
            using (SolidBrush bShade = new SolidBrush(pinShade))
            using (Pen needlePen = new Pen(pinMain, 2.2f))
            {
                needlePen.StartCap = LineCap.Round;
                needlePen.EndCap = LineCap.Triangle;

                using (GraphicsPath headPath = CreateRoundedRect(new RectangleF(-6.5f, -11f, 13f, 3.6f), 1.8f))
                {
                    g.FillPath(bMain, headPath);
                }

                PointF[] bodyPts = new PointF[]
                {
                    new PointF(-4.2f, -7.4f),
                    new PointF(4.2f, -7.4f),
                    new PointF(5.5f, -0.5f),
                    new PointF(-5.5f, -0.5f)
                };
                g.FillPolygon(bShade, bodyPts);

                using (GraphicsPath flangePath = CreateRoundedRect(new RectangleF(-8.0f, -1.2f, 16.0f, 3.4f), 1.7f))
                {
                    g.FillPath(bMain, flangePath);
                }

                g.DrawLine(needlePen, 0f, 2.2f, 0f, 11.2f);
            }

            g.Restore(state);

            if (isPinned)
            {
                using (SolidBrush dotBrush = new SolidBrush(Color.FromArgb(255, 74, 222, 128)))
                using (Pen dotRing = new Pen(Color.FromArgb(220, 20, 83, 45), 1f))
                {
                    g.FillEllipse(dotBrush, size - 10.5f, size - 10.5f, 6.5f, 6.5f);
                    g.DrawEllipse(dotRing, size - 10.5f, size - 10.5f, 6.5f, 6.5f);
                }
            }
        }

        private static GraphicsPath CreateRoundedRect(RectangleF bounds, float radius)
        {
            float d = radius * 2f;
            GraphicsPath path = new GraphicsPath();
            if (radius <= 0f)
            {
                path.AddRectangle(bounds);
                return path;
            }
            path.AddArc(bounds.Left, bounds.Top, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Top, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
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
