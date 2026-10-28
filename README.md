# 幕黑 · Windows 屏幕休息工具

一键让所有显示器显示纯黑，直接输入约定字符恢复画面，并在设定时间后进入 Windows 睡眠。

## 使用

支持 Windows 10 / 11。普通 Intel / AMD 电脑使用 `MuHei-win-x64.zip`；ARM Windows 使用 `win-arm64` 版本。

1. 解压压缩包，双击 `BlackScreen.exe`。程序自带运行库，无需安装 .NET，无需管理员权限。
2. 设置解锁字符（默认 `zcz`）和睡眠时间（默认 30 分钟），点击「立即黑屏」。设置会自动保存。
3. 在黑屏时连续输入 `zcz`（或你设置的字符），不必按回车，即可解除黑屏。鼠标点击和错误字符不会解锁；输错后重新连续输入完整字符即可。
4. 下次双击 `立即黑屏.cmd` 可直接使用已保存的设置进入黑屏。也可以用 `BlackScreen.exe --black` 创建快捷方式。

解锁字符支持 1–32 位英文字母和数字，不区分大小写，按英文字母键识别，不依赖中文输入法。数字键盘也可使用。按住一个键不会算作重复输入，重复字符需要重复按键。非字母数字键会中断当前输入序列。

睡眠时间可设为 0–1440 分钟，0 表示保持黑屏且不自动睡眠。计时从本次启用黑屏开始，错误输入不会重置计时。程序在等待期间阻止系统因空闲提前关闭屏幕或睡眠，系统管理策略、合盖和电源按钮仍可能影响电源状态。到时只申请一次睡眠；唤醒后仍需输入解锁字符，不会循环触发睡眠。Windows 不支持或拒绝睡眠时会恢复设置窗口并提示。

忘记字符时可按 **Ctrl + Alt + Shift + F12** 紧急退出黑屏；也可通过 Windows 的 Ctrl + Alt + Delete 打开任务管理器结束程序。该工具用于视觉遮屏，不是安全锁屏。纯黑覆盖不会真正关闭显示器背光，LCD 屏幕可能仍有微光。系统安全桌面、系统级提示可以覆盖普通应用窗口。

设置保存在 `%LOCALAPPDATA%\MuHei\settings.json`。程序不记录或上传键盘输入；仅在黑屏期间监听键盘，解除后立即移除监听。解锁字符是本地普通设置，不应使用真实账户密码。

## 开发与打包

使用 .NET 10 SDK。仓库由 WinForms 桌面项目和跨平台核心逻辑项目组成，无第三方包依赖。

```powershell
# 核心逻辑测试（Windows / Linux / macOS）
dotnet run --project tests/BlackScreen.Core.Tests -c Release

# Windows 上运行界面
dotnet run --project src/BlackScreen

# 生成独立运行的 EXE 和 ZIP
./scripts/publish.ps1
# ARM 版本
./scripts/publish.ps1 -Runtime win-arm64
```

产物位于 `artifacts/`。GitHub Actions 会为 x64 和 ARM64 构建可下载的压缩包。单文件发行版首次启动会由 .NET 解压必要的原生运行库到用户临时目录。

Linux 也可交叉构建（不能在 Linux 上运行 Windows 界面）：

```sh
dotnet publish src/BlackScreen/BlackScreen.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -p:DebugSymbols=false -o artifacts/win-x64
```

## Windows 验收

核心自动测试覆盖字符匹配、错误输入、重叠前缀、设置持久化和睡眠截止时间。真实显示器覆盖、键盘钩子和睡眠必须在 Windows 设备验收：

- 默认设置启用，所有屏幕纯黑、无边框、无任务栏和鼠标；点击鼠标、输入 `abc` 不恢复，输入 `zcz` 恢复。
- 改为 `ab12`，重启程序后设置仍在；旧字符无效，新字符及其大写版本可以解锁。
- 连续输入 `zzcz` 能解锁默认字符；反复按键与长按行为符合说明；中文输入法下仍能识别字母键。
- 设置 1 分钟，错误输入不延长等待，确认电脑睡眠。唤醒后可解锁，且不会再次自动睡眠。
- 设置 0，等待超过系统原有空闲睡眠时间，确认没有自动睡眠；解除后原有电源行为恢复。
- 多屏、不同 DPI、插拔显示器、远程桌面和睡眠唤醒后，检查覆盖范围。
- 紧急快捷键能恢复设置窗口；退出后其他程序键盘输入正常；重复启动只保留一个实例。

当前云环境仅能执行核心测试及交叉编译；不把这些结果视为 Windows 硬件验收通过。
