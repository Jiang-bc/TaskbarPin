# TaskbarPin 任务栏钉子

一个 Windows 任务栏小工具：在任务栏“自动隐藏”模式下，用一枚 PinWin 风格的小钉子图标把任务栏钉住；再点一下钉子即可恢复自动隐藏。就这一个效果，无依赖、单文件、便携。

## 功能

- 点击钉子：在“钉住任务栏（禁止自动隐藏）”与“恢复自动隐藏”之间切换
- 钉子与任务栏同层级：钉子作为 `Shell_TrayWnd` 的子窗口，全屏应用遮住任务栏时钉子一起被遮住
- 取消钉住时钉子随任务栏一起滑落，不提前消失
- 按住左键拖动可调整水平位置（位置存入 `config.ini`）
- 右键菜单：钉住/取消、重置位置、开机自启、退出（退出时恢复自动隐藏）
- 系统托盘也有一个小图标，左键同样可切换

## 运行环境

- Windows 10 / 11（分层子窗口需 Windows 8+）
- .NET Framework 4.x（系统自带，无需安装）

## 编译

无需任何第三方依赖，直接用系统自带的 csc：

```bat
%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /optimize+ /target:winexe /out:TaskbarPin.exe src\TaskbarPin.cs
```

## 使用

直接运行 `TaskbarPin.exe`。命令行也支持：

- `TaskbarPin.exe --pin`：钉住任务栏
- `TaskbarPin.exe --unpin`：恢复自动隐藏并立即收起

## 实现要点

- `SHAppBarMessage(ABM_SETSTATE)` 切换 `ABS_AUTOHIDE` / `ABS_ALWAYSONTOP`
- 钉子为 `WS_EX_LAYERED` 窗口，用 `UpdateLayeredWindow` 绘制与移动（屏幕坐标）
- `SetParent` 挂入任务栏，继承其 Z 序与遮盖关系
- 16ms 轮询任务栏矩形，钉子位置跟随任务栏动画
- 取消钉住时通过“触碰-移开”鼠标序列与焦点脉冲触发资源管理器原生收起动画
