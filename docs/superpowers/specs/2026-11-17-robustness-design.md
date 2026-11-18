# TaskbarPin robustness fixes

The user authorized fixing the five findings from the code review. Keep the portable, single-source WinForms application, .NET Framework 4.x, C# 5, and the existing command-line interface. Do not add dependencies or alter the user's running installation.

## Approach

Use an ApplicationContext independent of the widget's native window. Keep the managed form, timer, tray icon and state alive when its Explorer parent disappears, then recreate and reparent its handle when the taskbar returns. Closing the widget intentionally must still exit and release resources.

Restrict mouse transparency to an active collapse attempt. Restore interaction when that attempt completes. End dragging on capture loss or cancellation, without interpreting cancellation as a click.

Extract taskbar geometry into a small internal helper in the existing source. Derive the monitor from the taskbar, determine its edge, clamp placement to the actual taskbar bounds, and use the long axis for dragging. Preserve OffsetX as the stored offset along that axis, including vertical taskbars. Base collapse touch/exit coordinates and visibility on the same geometry, including negative monitor origins.

Treat native rendering as a transaction: commit coordinates only after UpdateLayeredWindow succeeds; retain a retry flag after failure and reset it when the handle changes. Log the native error once per failure episode. Validate parenting before interpreting positions in parent coordinates.

## Alternatives considered

- A detached topmost widget avoids parent destruction but breaks the existing fullscreen and animation behavior.
- A supervisor that recreates the entire form adds resource churn and complicates state transfer. Retaining the form under an independent context is smaller and preserves the existing tray icon.
- Per-monitor widgets are outside this repair. Follow Shell_TrayWnd on its monitor; do not add secondary taskbar widgets or promise per-monitor DPI awareness.

## Verification

Add regression tests for an independently hosted parent disappearing, visible unpinned interaction, interrupted dragging, four taskbar edges, negative display origins, overflow-sized offsets, small taskbars, and a failed native blit followed by recovery. Existing reflection and UI suites must pass. Any test that changes auto-hide or cursor position restores it in finally. Do not restart the user's Explorer.
