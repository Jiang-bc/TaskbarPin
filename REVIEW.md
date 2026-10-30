# TaskbarPin 审查与修复报告

审查对象：`src/TaskbarPin.cs`（单文件 WinForms，.NET Framework 4.x / C# 5）。
方法：静态通读 → 编译 → 反射测试脚手架（`tests/`）→ 真实进程 + 外部探针实测（位置 / 句柄 / CPU / 拖拽端到端）。

## 复现与验证方法

```bat
:: 编译（必须在 src 目录内，避免 MSYS 路径转换吃参数）
cd src && %WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe -nologo -optimize+ ^
  -target:winexe -out:..\TaskbarPin.exe TaskbarPin.cs

:: 反射回归测试（36 项）
cd tests && csc.exe -nologo -optimize+ -out:Tests.exe -r:System.Windows.Forms.dll -r:System.Drawing.dll Tests.cs
Tests.exe ..\TaskbarPin.exe

:: 真实进程度量
Probe.exe    <pid>        :: 任务栏/widget 矩形、样式、ABM_GETSTATE、父窗口
Stats.exe    <pid> <sec>  :: CPU 时间与 USER/GDI 句柄
DragTest.exe <pid> <dx>   :: 真实鼠标拖拽并发（会移动光标）
```

## 已确认缺陷与修复

| # | 严重度 | 缺陷 | 实测证据 | 修复 |
|---|---|---|---|---|
| 1 | **P0** | 坐标空间混用：widget 是 `Shell_TrayWnd` 的子窗口，`UpdateLayeredWindow` 的 `pptDst` 与 `Form.Location` 都按**父客户区**解释，但代码传的是**屏幕**坐标 → 再叠加一次 `tray.top` | widget 实到 `y=2049`，任务栏只有 `1019–1067` → 钉子整块在屏幕外，默认底部任务栏下**完全不可见**。测试里 `expected 1545 → actual 3073 (= 1545+1528)` | 新增 `ScreenToParentClient()`，位置与重绘统一用父客户区坐标；`_lastRenderedX/Y` 语义随之改为客户区坐标 |
| 2 | **高** | 托盘图标句柄泄漏：`Bitmap.GetHicon()` 每调用一次分配 1 个 USER + 3 个 GDI 句柄，`DestroyIcon` 声明了却从未调用，`Icon.Dispose()` 不销毁原生句柄 | 200 次 `UpdateVisualState()` → `USER 33→233, GDI 53→653`；单测 `UpdateTrayIconImage` 100 次 = `USER +100, GDI +300` → 约 3300 次后触顶 10000 GDI 上限，随后绘制静默失败 | 记录 `_trayIconHandle`，重建时 `DestroyIcon(旧)`；新增 `ReleaseTrayIconHandle()` 并在 `Dispose()` / `OnMenuExit` 调用 |
| 3 | **中** | 每 16 ms 执行一次 `SetParent`：`GetParent()` 对 re-parented 的 popup 返回 **owner(0)** 而非父窗口，判据永远为真 | 探针：`GetParent=0x0`，`GetAncestor(GA_PARENT)=0x100FE=tray`。这是空闲 CPU 的主因 | 判据改为 `GetAncestor(hwnd, GA_PARENT) != tray` |
| 4 | **中** | 空闲 CPU 4.38%：16 ms 定时器每 tick 都 `FindWindow`(两次) + `GetForegroundWindow` + `GetWindowRect` + `FindWindowEx` + 跨进程 `SHAppBarMessage(ABM_GETSTATE)`（62 次/秒） | `438 ms / 10 s = 4.38% of one core` | ① 缓存任务栏 HWND（`IsWindow` 校验，兼容 Explorer 重启）② `ABM_GETSTATE` 500 ms 探测一次并缓存 ③ 空闲 50 ms、折叠/拖拽动画时 16 ms |
| 5 | **中** | hover 重绘二次偏移：无参 `RenderLayeredBitmap()` 用 `Left/Top`（re-parented popup 上仍是屏幕坐标）当客户区坐标 | 悬停一次：`1984,1545 → 1987,3110`（跳下一个任务栏高度） | 无参重载改用上次成功 blit 的客户区坐标，仅在从未渲染时回退 `Left/Top` |
| 6 | **中** | 配置：`Pinned=` 只写不读（死键，误导排障）；按键解析要求行首严格等于 `OffsetX=`，手改 `offsetx = -35` 被静默忽略 | 写盘内容 `OffsetX=-48\r\nPinned=True\r\n`，重启后 pin 状态不来自文件；`offsetx = -35` 实测被忽略（offset 保持 -72） | 只落盘 `OffsetX=`（pin 以 `ABM_GETSTATE` 实时状态为准）；解析改为 `IndexOf('=')` + `Trim()` + 忽略大小写 |
| 7 | **低** | 异常全被 `catch {}` 吞掉，winexe 无控制台也无日志，崩溃/失败无从排查 | 手工制造损坏 config 时无任何痕迹 | 新增 `CrashLog`（`debug.log`）+ `AppDomain.UnhandledException` / `Application.ThreadException` 兜底；`LoadConfig`/`SaveConfig`/自启动写入改为记录异常 |
| 8 | **低** | 托盘图标双击触发两次 `MouseClick` → 连续两次切换 pin（任务栏闪一下又变回去） | 代码路径确认 | 350 ms 去抖 |
| 9 | **低** | `_toolTip` / `_contextMenu` / 定时器 / 托盘图标从未 Dispose；退出时不回收图标句柄 | 代码路径确认 | 重写 `Dispose(bool)` 统一释放 |

### 判定为「非缺陷」而保留的行为

- `TriggerNativeTaskbarCollapse` 会移动真实光标并 `Thread.Sleep(20)`：这是模拟系统“点一下任务栏空白处”以收起任务栏的唯一可靠手段；光标 y 已有还原逻辑（光标本就在任务栏内时留在上方 15 px 是刻意设计），未改动。
- `--pin` / `--unpin` 走独立进程、不经互斥体：命令行工具本就是“对已运行实例的状态操作”，程序退出后仍生效，符合预期。

## 修复后实测

| 指标 | 修复前 | 修复后 |
|---|---|---|
| widget 位置（虚拟坐标，任务栏 1019–1067） | `1355,2049`（屏外） | `1323,1030` / `1216,1030`，`inside taskbar? YES` |
| 空闲 CPU（60 s 采样） | 4.38 % 单核 | **0.57 % 单核**（10 s 采样 0.63 %） |
| 句柄（稳态） | 每次状态刷新 +4（1 USER + 3 GDI） | **0 增长**（200 次重建 delta=0；60 s 静置恒为 USER 27 / GDI 34） |
| 反射回归 | — | **36 passed, 0 failed** |
| 拖拽端到端 | — | 拖动 -160 px → widget 移动 -160 px，`config.ini` 落盘 `OffsetX=-208` |
| 命令行 | — | `--unpin` → `ABM_GETSTATE 0x1 (AUTOHIDE=True)`、widget 隐藏；`--pin` → 任务栏恢复、widget 可见且不再 click-through |
| 单实例 | 语义正确 | 第二次启动 139 ms 内退出，不产生第二个 widget |

## 已知限制

- 仅在 Windows 11 24H2 / 2560×1600 @150% 上实测；Windows 10 与多显示器、任务栏置于顶部/侧边的场景未跑通回归（子窗口方案在 Win10 上 `SetParent` 到 `Shell_TrayWnd` 的行为差异较大，建议实测）。
- 程序为 system-DPI-aware，测试夹具中部分进程未声明 DPI 感知，坐标按 1.5 倍换算对比。
- `tests/` 为本次新增的审查脚手架，未纳入 `.gitignore`，也未提交，可视需要保留或删除。
