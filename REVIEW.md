# TaskbarPin 审查与修复报告

审查对象：`src/TaskbarPin.cs`（单文件 WinForms，.NET Framework 4.x / C# 5）。
方法：静态通读 → 编译 → 反射测试脚手架（`tests/`）→ 真实进程 + 外部探针实测（位置 / 句柄 / CPU / 拖拽端到端）。

## 修复规划执行记录

本轮按 `docs/REPAIR_PLAN.md` 加固轮询、状态探测、持久化和启动路径，并将回归接入 `build.ps1 -Test`。

- 轮询异常保持消息循环运行。同类日志按 60 秒节流，当前文件和新生成的归档各以 256 KiB 为上限。
- `ABM_GETSTATE` 返回零也可能表示有效的非自动隐藏状态。零值通过任务栏位置探测确认，探测未知时保留上次状态。启动连续两次探测失败时先呈现未钉住，后续以恢复的系统状态同步。
- 配置与日志共用便携目录和用户目录回退策略。配置同目录替换，钉住状态变更不写盘。自启动项按当前 EXE 路径验证，启动异常有非零退出码和日志。
- 系统设置消息触发布局和外观复查，重复设置消息复用图标。拖动阈值跟随 Windows 设置。收起超时在任务栏几何读取之前清理，覆盖 Explorer 暂时不可用的场景。保留原生触碰序列的 20 ms 停留，当前线程持有鼠标捕获时跳过序列。覆盖所在显示器的前台窗口跳过焦点脉冲。
- Windows 11 Build 26300 上的脚本回归共 131 项，逻辑 37 项、UI 19 项、健壮性 45 项、本轮修复 30 项。覆盖真实 `--PIN` 进程、注入式 `--UnPiN`、临时注册表键、日志故障和配置替换失败。100 次重复设置消息的 USER 和 GDI 句柄增量均为 0。
- 独立进程空闲采样 60 秒，CPU 占单核 0.21%，USER 句柄 27、GDI 句柄 34，采样前后相同。真实拖动 -100 px 后落盘 `OffsetX=-148`。模拟托盘双击回调仅切换一次，菜单退出恢复自动隐藏并释放窗体。测试后还原自动隐藏和光标位置。

## 2026-11-17 健壮性修复

本轮修复独立于下方历史审查记录，保持单文件、C# 5 和 .NET Framework 4.x 构建方式。

- **窗口生命周期**：新增独立 `PinApplicationContext`；任务栏父窗口销毁时保留托盘、定时器和托管窗体，重建原生句柄时重置坐标缓存、穿透和拖拽状态。使用独立隐藏辅助进程销毁父窗口，验证消息循环继续运行、轮询重新挂载、主动关闭才退出。未重启真实 Explorer。
- **鼠标交互**：穿透只在主动收起期间开启，收起完成或超时后恢复；拖拽在丢失捕获、取消操作和左键释放时结束，取消不触发点击切换。延迟收起回调会检查当前状态，避免重新钉住后执行过期操作。
- **布局通用性**：统一任务栏几何，按所在显示器、任务栏边缘和长轴计算位置、拖动、可见性与鼠标离开方向；支持负坐标、极端偏移和较窄的任务栏，保留 `OffsetX` 配置键。仍只跟随 `Shell_TrayWnd`。
- **绘制失败**：验证父窗口挂载和坐标转换，只在原生绘制成功后提交缓存；失败保留 dirty 状态供后续重试，每次持续失败只记录一次错误。
- **回归**：新增 `tests/RobustnessTests.cs`；现有逻辑测试增加 DPI 声明和鼠标位置恢复，避免实测结束后改变光标位置。
- **独立复核**：修复复核发现的两个边界问题：使用 `ABM_GETTASKBARPOS` 报告的停靠矩形识别所属显示器，避免隐藏窗口跨入邻屏后误判；创建原生句柄时应用待处理的穿透样式，设置穿透时同时校验实际样式。新增相邻屏幕和重建样式回归。

侧边、顶部、负坐标和极端尺寸以确定性的几何用例验证。不同 Windows 10/11 版本、混合 DPI 和真实 Explorer 重启仍需额外实机验证；不把数学回归视为全部系统兼容性验证。

本轮完整构建及验证：`build.ps1` 成功；逻辑回归 **36 passed**，UI 回归 **19 passed**，健壮性回归 **45 passed**，共 **100 项断言通过**。UI 生命周期检查为 `USER +0 / GDI +0`；`git diff --check` 通过。真实程序启动、重复实例退出和主动关闭的冒烟检查均通过。

## 复现与验证方法

```bat
:: 编译（必须在 src 目录内，避免 MSYS 路径转换吃参数）
cd src && %WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe -nologo -optimize+ ^
  -target:winexe -out:..\TaskbarPin.exe TaskbarPin.cs

:: 反射回归测试（36 项）
cd tests && csc.exe -nologo -optimize+ -out:Tests.exe -r:System.Windows.Forms.dll -r:System.Drawing.dll Tests.cs TestAccess.cs
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

- 历史实测使用 Windows 11 24H2 / 2560×1600 @150%；Windows 10、多显示器和顶部或侧边任务栏仍需真机验证。
- 程序为 system-DPI-aware，测试夹具中部分进程未声明 DPI 感知，坐标按 1.5 倍换算对比。
- `build.ps1 -Test` 使用交互式桌面的真实任务栏。Windows 10、混合 DPI、多显示器和真实顶部或侧边任务栏的交互仍需对应设备验证。

## 托盘界面调整

大按钮、拖动与位置配置已移除；程序由隐藏窗体接收消息，系统托盘图标是唯一界面。Explorer 重建托盘时重新同步状态并显示图标。上文关于大按钮的记录保留为历史审查，不再描述当前实现。Windows 11 托盘溢出区仍需真机验证。
