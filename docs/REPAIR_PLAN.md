# TaskbarPin 修正方案：移除可移动大按钮，仅保留托盘小按钮

> 面向执行修复的 agent。本文取代此前以稳健性为主的修复方案；旧方案原文保留在文末“附录”。
> 需求：程序不再需要附着在任务栏上、可自行拖动的大按钮（38×38 的 `Shell_TrayWnd` 子窗口），只保留系统托盘小按钮（`NotifyIcon`）。

## 0. 硬性约束

- 基于当前工作区未提交代码继续修改，保留既有稳健性修复：`CrashLog` 限流轮转、`TryGetTaskbarAutoHide` 三态、`AppStorage` 只读回退、`RefreshScale`、`CoversDisplay` 全屏保护、`RunCommandLine`、托盘图标句柄回收。
- 保持单文件、便携 WinForms、.NET Framework 4.x、**C# 5**（`build.ps1` 使用 `/langversion:5`；禁用 `?.`、`nameof`、字符串插值、`out var`、表达式体成员）；不新增依赖。
- 不改命令行接口 `--pin` / `--unpin` 与单实例互斥锁。
- 不重启用户的 Explorer；不新建分支、不提交，除非用户要求。
- 任何改动自动隐藏状态或光标位置的测试，必须在 `finally` 中还原；不覆盖用户真实的开机启动注册表值。
- 不顺手重构无关代码。

## 1. 目标架构

| 项目 | 现状 | 修正后 |
| --- | --- | --- |
| 大按钮 | `PinWidgetForm` 为分层子窗口，`SetParent` 到 `Shell_TrayWnd`，可拖动、悬停、鼠标穿透 | 删除；窗口永不显示、不挂载 |
| 托盘小按钮 | `NotifyIcon`，左键切换、右键菜单 | 保留，成为唯一界面 |
| 窗口类 | `PinWidgetForm` | 重命名为 `PinTrayForm`，作为不可见消息宿主，仍由 `PinApplicationContext` 管理生命周期 |
| 位置配置 | `config.ini` 的 `OffsetX` | 删除读写；`AppStorage` 仅供 `debug.log` 使用 |
| 收起任务栏 | 点击后触发原生收起 | 保留 `TriggerNativeTaskbarCollapse` 与 `TaskbarGeometry` 的收起相关部分 |

隐藏宿主要求：

- `FormBorderStyle = None`、`ShowInTaskbar = false`；重写 `SetVisibleCore(bool)`，始终调用 `base.SetVisibleCore(false)`；`ShowWithoutActivation` 返回 `true`。
- `CreateParams.ExStyle` 仅保留 `WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE`，移除 `WS_EX_LAYERED`、`WS_EX_TOPMOST`、`WS_EX_TRANSPARENT`。
- 构造函数仍读取 `this.Handle` 创建句柄，以支持 `BeginInvoke` 和接收系统广播；不再 `SetParent`，因此 Explorer 重启不会销毁宿主句柄。
- 通过 `RegisterWindowMessage("TaskbarCreated")` 监听 Explorer 重建托盘：收到后重置 `_trayHwnd`、`_positionTrayHwnd`、`_dockedTaskbarBounds` 与探测时间戳，并调用 `SyncAutoHide()`、`RefreshAppearance()`、`UpdateVisualState()`；同时让 `NotifyIcon` 重新可见（先 `Visible=false` 再 `true`）。

## 2. `src/TaskbarPin.cs` 修改清单

### 2.1 删除

- 窗口与渲染：`UpdateWidgetPositionAndVisibility`、`TryScreenToParentClient`、两个 `RenderLayeredBitmap`、`DrawPinBadge`、`SetMouseThrough`、`OnHandleCreated` 中的定位调用、`OnHandleDestroyed` 中与渲染相关的重置。
- 鼠标交互：`OnMouseEnter/Leave/Down/Move/Up/CaptureChanged`、`FinishDrag`、`HasStartedDragging`。
- 字段：`_widgetSize`、`_toolTip`、`_isHovered`、`_isDragging`、`_hasMovedDuringDrag`、`_dragIsHorizontal`、`_dragStartCursor`、`_dragStartOffsetX`、`_isMouseThrough`、`_lastRenderedX/Y`、`_renderDirty`、`_renderFailureLogged`、`_parentAttachFailed`、`_offsetFromTrayNotifyLeft`。
- 位置持久化：`LoadConfig`、`SaveConfig`、`_configPath`、`_alternateConfigPath`、`AppStorage.WriteConfig`；`AppStorage.CanWrite` 中对 `config.ini` 只读的检查改为检查 `debug.log`。
- 菜单：`_menuResetPos`（“重置图标位置”）及其添加；`PinArtwork.MenuGlyph` 的 `"reset"` 分支。
- `TaskbarGeometry`：`GetWidgetSize`、`GetWidgetLocation`、`IsDesktopSide`。
- Win32 冗余声明：`GWL_EXSTYLE`、`WS_EX_LAYERED`、`WS_EX_TOPMOST`、`WS_EX_TRANSPARENT`、`WM_MOUSEACTIVATE`、`MA_NOACTIVATE`、`HWND_TOPMOST`、`SWP_*`、`SetParent`、`GetParent`、`GetAncestor`、`GA_PARENT`、`SetWindowPos`、`FindWindowEx`、`ScreenToClient`、`GetWindowLong`、`SetWindowLong`、`UpdateLayeredWindow`、`ULW_ALPHA`、`AC_SRC_*`、`SIZE`、`BLENDFUNCTION`、`GetDC`、`ReleaseDC`、`CreateCompatibleDC`、`DeleteDC`、`SelectObject`、`DeleteObject`。
- 窗口图标：隐藏窗口不需要 `this.Icon`，可移除 `LoadApplicationIcon` 与 `_applicationIcon`（若保留，须同步保留其 Dispose 与测试）。

### 2.2 保留

- `NotifyIcon`：左键切换 + 350 ms 双击去抖、`UpdateTrayIconImage`（按 `Scale(16)` 绘制，`DestroyIcon` 回收旧句柄）、`ReleaseTrayIconHandle`。
- 右键菜单：状态行、钉住/恢复、开机启动、退出并恢复自动隐藏（共 3 个可点击命令，6 项含分隔线）。
- `PinArtwork`、`UiPalette`、菜单渲染器、`RefreshScale` / `RefreshAppearance`（`WM_SETTINGCHANGE` `0x1A`、`WM_DISPLAYCHANGE` `0x7E`、`WM_THEMECHANGED` `0x31A`）。
- `SetPinnedState`、`TogglePinState`、`SyncAutoHide`、`TryGetTaskbarAutoHide`、`SetTaskbarAutoHide`、`TriggerNativeTaskbarCollapse`、`PulseTrayFocus`、`CoversDisplay`、`ForceForegroundWindow`、`ReleaseTrayFocusIfNeeded`。
- `TaskbarGeometry` 的 `MonitorBounds`、`Bounds`、`IsHorizontal`、`Edge`、`IsShown`、`Create`、`IsCursorAtEdge`、`GetCollapsePoints`。
- `Program`、`PinApplicationContext`、`CrashLog`、`AppStorage.ResolveDirectory`。

### 2.3 修改

- `SetPinnedState`：取消钉住后无大按钮可穿透，保留 `_waitingForCollapse` / `_collapseStage` / `_unpinRequestedAt`、绝对超时（≈1.1 s）与 `ExpireCollapse`、`EndCollapse`（去掉 `SetMouseThrough`）。
- `PollTaskbar`：空闲时不再读取任务栏矩形与屏幕列表，仅同步 `ABM_GETSTATE`（500 ms）与 `UpdateVisualState`；仅在 `!_isPinnedByApp && _waitingForCollapse` 时调用 `ReadTaskbarGeometry`，判定收起完成并执行 25 ms / 260 ms 两阶段收起；间隔保持空闲 50 ms、收起中 16 ms。
- `_lastNonTrayForeground` 更新：排除本进程窗口（`GetWindowThreadProcessId` 对比当前进程）以及类名 `Shell_TrayWnd`、`Shell_SecondaryTrayWnd`、`NotifyIconOverflowWindow`、`TopLevelWindowForOverflowXamlIsland`；新增 `GetClassName` P/Invoke。避免点击托盘或打开菜单后把目标前台窗口误记为托盘窗口。
- `WndProc`：移除 `0x001F` 与 `WM_MOUSEACTIVATE`，新增 `TaskbarCreated` 处理。
- `OnMenuExit`：移除 `Hide()`，其余（恢复自动隐藏、触发收起、`Close`、`Application.Exit`）保持。
- `UpdateVisualState`：不再设置 `_toolTip`；托盘提示文本 `_trayIcon.Text` 保留，且遵守 `NotifyIcon.Text` 63 字符上限；不再调用渲染函数。
- `Program.Main`：`NotifyIcon` / 窗体创建失败时记录日志并返回非零退出码，不产生未处理崩溃。

## 3. 测试与工具修改

- `tests/TestAccess.cs`：新增 `internal const string FormTypeName = "TaskbarPin.PinTrayForm";`，所有套件改用该常量；保留 `PollTimer`、`TrayIcon`。
- `tests/Tests.cs`：删除 `TestConfig`、`TestPositioning`、`TestOffsetPipeline` 及 `TestLeakAttribution` 中 `RenderLayeredBitmap` 项；`TestDraw` 改为反射 `TaskbarPin.PinArtwork.DrawBadge`，在 16 px 与 32 px 验证钉住/未钉住像素差异与悬停差异；`TestStateMachine` 去掉 `config.ini` 断言；保留 `TestEnvironment`、`TestHandleLeak`、`TestAutoHideRoundTrip`、`TestSingleInstanceMutex`。
- `tests/UiTests.cs`：`TestMenu` 改为 3 个命令均带图标（`actions == 3 && illustrated == 3`）、`menu.Items.Count == 6`、不存在“重置图标位置”；保留嵌入图标、主题对比度与句柄生命周期检查。
- `tests/RobustnessTests.cs`：删除 `TestInteraction`、`TestRenderingFailure` 及 `TestGeometry` 中的 `GetWidgetLocation` 断言；保留四边收起几何、`IsShown`、多显示器归属；重写 `TestLifecycle`：宿主调用 `Show()` 后仍不可见且不是 `Shell_TrayWnd` 的子窗口；投递 `TaskbarCreated` 后状态刷新无异常；`OnMenuExit` / `Close()` 正常结束消息循环；左键去抖仍生效；前台为本进程窗口时 `_lastNonTrayForeground` 不被覆盖。
- `tests/RepairTests.cs`：删除 `TestConfigMigration`、`HasStartedDragging` 断言、`TestStartupIcon`（若图标逻辑一并移除）；`TestStorage` 改为验证目录不可写或 `debug.log` 只读时回退，且 `CrashLog.Write` 落盘；其余 `TestLogLimit`、`TestPollingFailure`、`TestUnknownAutoHide`、`TestAutoStartPath`、`TestCollapseSafety`、`TestCommandLine` 保持全绿。
- `tests/Render.cs`：改用新类型名；移除重置项后 `menu.Items[3]` 为“开机启动”，确认高亮截图仍有效。
- 删除仅服务大按钮的 `tests/DragTest.cs`；`tests/Probe.cs` 精简为只检查 `Shell_TrayWnd` 与 `ABM_GETSTATE`（并确认进程在任务栏下没有任何子窗口）；`tests/Stats.cs`、`tests/GdiTest.cs` 保留。
- `tools/IconAssetBuilder.cs` 与 `build.ps1` 主流程无需改动。

## 4. 文档修改

- `README.md`：删除大按钮、拖动、`config.ini` / `OffsetX`、菜单“重置位置”与快捷键 `R` 的描述；改写功能与“实现要点”（隐藏宿主、`TaskbarCreated`、仅托盘小按钮）；补充 Win11 提示：新图标可能位于 `^` 溢出区，需手动拖到常驻区；`debug.log` 目录策略保留。
- 重新运行 `tests/Render.cs` 更新 `docs/images/menu-light.png`、`docs/images/menu-dark.png`。
- `REVIEW.md`：保留既有审查记录，在末尾追加“大按钮已移除”的说明，并更新“已知限制”（不再涉及子窗口、DPI 对大按钮的影响）。

## 5. 执行顺序

1. **测试先行**：按第 3 节改契约（`TestAccess` → 各套件 → `Render` → 删除 `DragTest`、精简 `Probe`），确认旧二进制上相关新断言失败。
2. **重构源码**：先重命名与隐藏宿主，再删大按钮/配置/冗余 Win32，最后修改 `PollTaskbar`、前台过滤与 `TaskbarCreated`；逐步编译。
3. **验证**：`powershell -NoProfile -ExecutionPolicy Bypass -File build.ps1 -Test`，四个套件全部通过；运行 `Render` 检查两张菜单图；手工检查 `--pin`、`--unpin`、`--bogus`（退出码 2）、单击托盘切换、双击不闪、菜单开机启动与退出恢复自动隐藏；确认任务栏下无本进程子窗口；测试后自动隐藏与光标已还原。
4. **文档**：更新 `README.md`、`REVIEW.md` 与截图，检查 `git status`、`git diff` 无临时文件与残留进程。

## 6. 完成定义

- 运行时不存在任何位于任务栏上的程序窗口；仅有托盘图标。
- 点击托盘小按钮、右键菜单、`--pin` / `--unpin`、退出恢复自动隐藏行为与此前一致。
- 空闲 CPU 不高于 REVIEW.md 记录的 0.57 % 单核；句柄稳态 delta = 0。
- 全量测试通过；`config.ini` 不再生成；旧版遗留的 `config.ini` 被忽略且不报错。
- 最终说明列出改动文件及未覆盖场景（Win10、多显示器、竖直/顶部任务栏、Win11 溢出区真机）。

---

# 附录：历史修复方案（已被本文取代，仅供追溯）

# TaskbarPin 批判性审查与修复指导

> 面向后续执行修复的 agent。审查基于 `src/TaskbarPin.cs`（约 1617 行）当前工作区状态（含未提交的 robustness 修复）、`REVIEW.md`、`docs/superpowers/specs|plans/2026-11-17-robustness*.md`、`tests/`。
> 本轮仅做静态代码审查，**未运行程序或测试**。每条结论标注置信度：**[确认]**＝代码路径直接可见；**[待验证]**＝推断，修复前须先写复现。

## 0. 硬性约束（所有修复必须遵守）

- 保持单文件、可移植 WinForms、.NET Framework 4.x、**C# 5**（无 `?.`、`nameof`、字符串插值、`out var`）；不新增依赖；不改命令行接口 `--pin` / `--unpin`。
- 不重启用户的 Explorer；不动用户正在运行的安装实例；不创建分支/提交，除非用户要求。
- 任何改动 auto-hide 或光标位置的测试，必须在 `finally` 中还原。
- 先写失败测试（复现），再修；保留既有 36 项反射回归与 `tests/RobustnessTests.cs` 全绿。
- 不顺手重构无关代码；一个问题一个小改动。

## 1. 总体评价

上一轮修复（句柄泄漏、`GetAncestor` 判据、坐标系、ApplicationContext 独立生命周期、渲染事务化）方向正确。剩余风险集中在：**① 轮询回调无异常防护；② 状态源判断“失败即当作已钉住”；③ 文件/注册表等环境假设（可写目录、exe 路径）；④ DPI/显示变化后缓存的缩放值过期；⑤ 抢焦点/移动光标的折叠手法的副作用；⑥ 测试不在构建流程里且依赖私有成员名。**

## 2. 问题清单（按优先级）

### P1 — 应优先修复

| # | 问题 | 位置 | 置信度 |
|---|---|---|---|
| P1-1 | **`PollTimer_Tick` 无 try/catch**。16–50 ms 定时器内任何异常都会走 `Application.ThreadException`（仅写日志、继续运行），下一 tick 再次抛出 → `debug.log` 以每秒数十行速度膨胀；`CrashLog` 无大小上限、无节流。 | `PollTimer_Tick`(~L1084)、`CrashLog`(~L1590) | 确认 |
| P1-2 | **`GetTaskbarAutoHide()` 失败即“已钉住”**：`FindWindow` 返回 0 或 `SHAppBarMessage` 失败时返回值为 0 → 结果 `false` → 程序认为任务栏非自动隐藏 → `_isPinnedByApp = true`（构造函数、`ContextMenu.Opening`、轮询同步三处）。Explorer 重启/卡顿期间会把 UI 状态误翻转并触发 `UpdateVisualState`。 | `GetTaskbarAutoHide`(~L1300)、构造函数 L675、L650、轮询 L1099–1118 | 确认（行为后果待验证） |
| P1-3 | **配置/日志目录假设可写**：`config.ini`、`debug.log` 都写在 `AppDomain.BaseDirectory`。放进 `Program Files` 或只读位置时：位置无法保存（仅记日志，而日志同样写失败 → 完全静默），用户感知为“拖动后重启复位”。 | 构造函数 L608、`SaveConfig`、`CrashLog.LogPath` | 确认 |
| P1-4 | **`SetPinnedState` 每次切换都 `SaveConfig()`**，但被保存的只有 `OffsetX`（未变化）。无意义写盘，且写盘失败会在每次点击时记日志；与 `SaveConfig` 注释“只保存偏移”自相矛盾。写入还是非原子的 `File.WriteAllText`，断电/崩溃可能留下空文件。 | `SetPinnedState` L973、`SaveConfig` L713 | 确认 |

### P2 — 重要

| # | 问题 | 位置 | 置信度 |
|---|---|---|---|
| P2-1 | **`_uiScale` 只在构造时读取一次**。显示器 DPI/缩放变更（WM_DISPLAYCHANGE `0x7E`、WM_SETTINGCHANGE `0x1A`）后 `RefreshAppearance` 只重读调色板，菜单字体/图标尺寸、托盘图标 `Scale(16)`、`_widgetSize = 38`（是否随 DPI 缩放需核对）都保持旧值。 | L615、`RefreshAppearance` L923 | 确认缓存；视觉后果待验证 |
| P2-2 | **开机自启只比较“键是否存在”**，不校验指向的路径。exe 被移动/重命名后，菜单仍显示“已开启”，实际登录时启动失败；再点击会“关闭”而非修复。 | `IsAutoStartEnabled` L725、`OnMenuAutoStart` L737 | 确认 |
| P2-3 | **折叠手法的副作用面大**：`TriggerNativeTaskbarCollapse` 在 UI 线程 `Thread.Sleep(20)`、`SetCursorPos` 搬光标、`mouse_event` 注入相对移动（受指针加速影响）、`ForceForegroundWindow` 用 `AttachThreadInput + keybd_event(0…)` 抢前台，并先后把焦点给 tray 再还给目标。全屏游戏/视频/远程桌面/UAC 提示下可能打断用户或失败。`--unpin` CLI 同样走此路径。 | L1046–1082、L1007–1028 | 确认；影响面待验证 |
| P2-4 | **`_waitingForCollapse` 的退出条件需核对**：轮询里只在 `msSinceUnpin > 1000` 且系统状态与预期不符时清除。若任务栏因鼠标停留而不收起，应确认存在超时兜底，否则 `SetMouseThrough(true)` 与 16 ms 高频轮询可能长期保持。 | L987–1003、L1106–1125（折叠阶段处理段 L1126–1150 需通读） | 待验证 |
| P2-5 | **拖动阈值写死 5 px（未缩放）**，高 DPI 下过于敏感，轻触易被判为拖动而不是点击。应使用 `SystemInformation.DragSize`。 | `OnMouseMove` L876 | 确认 |
| P2-6 | **构造函数中未保护的初始化**：`Icon.ExtractAssociatedIcon(...)`、`new NotifyIcon()`、`Registry` 访问任一抛异常都会让进程启动即崩溃（此时 `ThreadException` 尚无消息循环，用户看到的是“双击没反应”）。 | L656–661 | 确认 |

### P3 — 次要 / 维护性

| # | 问题 | 置信度 |
|---|---|---|
| P3-1 | `UpdateVisualState` 每次都重建托盘图标与菜单图像；`WM_SETTINGCHANGE` 极其频繁（任何系统设置变更），`RefreshAppearance` 同样重建全部。应在调色板/缩放/状态未变化时跳过。 | 确认 |
| P3-2 | CLI 只识别精确小写 `--pin` / `--unpin`；其它参数（`-pin`、`/pin`、`--help`、拼写错误）会**静默启动完整常驻实例**。应对未知参数给出提示或退出。 | 确认 |
| P3-3 | 第二个实例静默退出，无任何反馈；用户误以为程序没启动。可选：`SetForegroundWindow` 不适用，可考虑短暂托盘气泡或日志。低优先级。 | 确认 |
| P3-4 | 仅处理主任务栏 `Shell_TrayWnd`；`Shell_SecondaryTrayWnd`（多显示器）未覆盖。已在 spec 明确列为范围外，**不要在本轮修复**，但 README 应明确写出。 | 确认 |
| P3-5 | 未声明 DPI manifest，靠 `SetProcessDPIAware()`（system-aware）。多显示器混合 DPI 下会被位图缩放。spec 已声明不承诺 PerMonitor。 | 确认 |
| P3-6 | `tests/` 依赖反射私有成员名（`_isMouseThrough`、`PollTimer_Tick`、`_widget` 等），重命名即静默失败；且未接入 `build.ps1`。 | 确认 |
| P3-7 | Win10 / 竖直任务栏 / 顶部任务栏 / 多显示器仅有纯几何单测，缺乏真机验证（`REVIEW.md` 已自述）。 | 确认 |

## 3. 修复规划（按依赖排序，每步独立可验证）

### 步骤 1 — 轮询与日志防护（P1-1）

- 在 `PollTimer_Tick` 外包 `try { … } catch (Exception ex) { CrashLog.WriteOnce("PollTimer", ex); }`。
- `CrashLog` 增加：① 相同 `context + ex.GetType() + ex.Message` 在 N 秒（建议 60 s）内只记一次；② 文件超过 256 KB 时截断/滚动为 `debug.log.1`。保持 C# 5 与“日志失败绝不抛出”。
- **验收**：测试中令 tick 内部必然抛异常（注入失败），运行 5 s，`debug.log` 行数 ≤ 2；进程仍存活，定时器仍在运行。

### 步骤 2 — 自动隐藏状态“未知”三态（P1-2）

- 新增 `static bool TryGetTaskbarAutoHide(out bool autoHide)`：`tray == 0` 或 `SHAppBarMessage` 返回值为 0 时返回 `false`（未知）。保留旧 `GetTaskbarAutoHide()` 供反射测试，内部委托新方法。
- 构造函数、`Opening`、轮询：未知时**保持原状态**，不翻转 `_isPinnedByApp`；构造函数未知时默认按“自动隐藏=false→已钉住”之前先重试一次。
- **验收**：注入“探测失败”时 `_isPinnedByApp` 不变；探测恢复后正常同步。需在测试中通过缓存字段/可替换探测委托实现，**不得**真的杀 Explorer。

### 步骤 3 — 配置与日志位置、原子写入、去除多余写盘（P1-3、P1-4）

- `SetPinnedState` 删除 `SaveConfig()` 调用；仅拖动结束保存。
- `SaveConfig`：写 `config.ini.tmp` 再 `File.Replace`/`File.Move` 覆盖（.NET 4 可用）。
- 目录策略：优先 `BaseDirectory`（保持便携）；探测可写（尝试创建临时文件）；不可写时回退到 `%LOCALAPPDATA%\TaskbarPin\`。`LoadConfig` 按同一顺序查找。`CrashLog` 复用同一解析结果。
- **验收**：把 exe 放到只读目录运行，拖动后重启位置保留，日志可见；便携目录行为与现状一致；掉电模拟（写入中抛异常）不留半截文件。

### 步骤 4 — DPI/显示变化重算（P2-1、P2-5、P3-1）

- 抽出 `RefreshScale()`：重读 `_uiScale`，仅当值变化时重建 `_contextMenu.ImageScalingSize`/`Padding`、菜单图像、托盘图标并重新渲染。在 `WM_DISPLAYCHANGE` / `WM_SETTINGCHANGE` 中调用。核对 `_widgetSize` 是否随缩放。
- `RefreshAppearance`/`UpdateVisualState` 对“无变化”短路（缓存上次的 palette 与 pinned 状态）。
- 拖动阈值改用 `SystemInformation.DragSize`。
- **验收**：测试里模拟 `_uiScale` 变化后 `Scale(16)` 与菜单尺寸更新；重复触发 WM_SETTINGCHANGE 不增加 GDI/USER 句柄（沿用 REVIEW.md 的句柄采样方法，delta = 0）。

### 步骤 5 — 自启动路径校验（P2-2）

- `IsAutoStartEnabled` 比较注册表值与当前 `Application.ExecutablePath`（忽略大小写、去引号）；不一致视为“未启用”，再点击时写入当前路径（修复）。
- **验收**：写入指向旧路径的值 → 菜单显示未开启 → 点击后值变为当前路径。测试用后必须还原/删除 `TaskbarPin` Run 值（注意不要覆盖用户真实已有的值，先备份再还原）。

### 步骤 6 — 折叠流程审计与加固（P2-3、P2-4）

- 先通读 L1126–1150 的折叠阶段并确认 `_waitingForCollapse` 有**绝对超时**（建议 ≤ 3 s）清除 + `SetMouseThrough(false)`；没有则补上并加测试。
- 将 `Thread.Sleep(20)` 改为在 UI 线程外无需阻塞的做法，或至少保证不在持有鼠标捕获时调用；若暂不改，必须在文档注明理由（REVIEW.md 已将“搬光标”判为设计行为，勿删除该机制）。
- 抢前台：在目标窗口是全屏/独占窗口（`GetForegroundWindow` 窗口矩形覆盖整个显示器）时跳过焦点脉冲。
- **验收**：折叠超时测试；全屏前台时 `includeFocusPulse` 路径不调用 `ForceForegroundWindow`（用可注入探针断言）。

### 步骤 7 — 启动健壮性与 CLI（P2-6、P3-2）

- 构造函数中图标提取失败回退 `SystemIcons.Application`；`NotifyIcon` 创建失败记录日志并以非零退出而不是未处理崩溃。
- 未知参数：写日志并直接退出（不启动实例）；`--pin/--unpin` 大小写不敏感（已有）；新增 `--help` 可选。
- **验收**：`TaskbarPin.exe --bogus` 不留进程；`--pin`/`--unpin` 行为不变（沿用既有端到端检查）。

### 步骤 8 — 测试与文档（P3-6、P3-7）

- 将 `tests/` 纳入 `build.ps1 -Test`（或 `tools/` 下脚本），并把测试私有成员名集中到一个访问器，减少重命名脆弱性。
- `README.md` 写明：仅主任务栏、系统 DPI 感知、Win11 24H2 验证范围、配置目录回退策略。
- 更新 `REVIEW.md` 的“已知限制”，不要删除既有审查记录。

## 4. 建议执行顺序与分工

1 → 2 → 3 可并行不同 agent？**否**：都改 `TaskbarPin.cs` 的同一类，建议串行或严格按方法划分、合并时跑全量测试。推荐顺序：步骤 1 → 3 → 2 → 4 → 5 → 7 → 6 → 8（风险最高的折叠改动放在基础防护完成之后）。

## 5. 完成定义

- 每个步骤附带先红后绿的测试；全量反射回归 + `RobustnessTests` 全绿。
- 空闲 CPU 不高于 REVIEW.md 记录的 0.57 % 单核；句柄稳态 delta = 0。
- 手工检查：钉住/取消钉住、拖动、双击托盘、退出恢复自动隐藏、`--pin/--unpin` 均正常，测试结束后用户的 auto-hide 与光标位置已还原。
- 最终交付说明列出：改动文件、未覆盖场景（多显示器、Win10、竖直/顶部任务栏真机）。

