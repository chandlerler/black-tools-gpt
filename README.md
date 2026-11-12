# 幕黑 · Windows 屏幕休息工具

驻留右下角系统托盘的息屏工具。按快捷键或右键托盘图标，一束白光射入三棱镜、分散成彩虹，再用三秒渐暗到纯黑；直接输入约定字符即可恢复，并可设置多久后进入 Windows 睡眠。

## 下载与使用

从 [GitHub Releases](https://github.com/chandlerler/black-tools-gpt/releases/latest) 下载。普通 Intel / AMD Windows 10 / 11 电脑使用 `MuHei-win-x64.zip`；ARM Windows 使用 `MuHei-win-arm64.zip`。

1. 解压并双击 `BlackScreen.exe`。无需安装 .NET，无需管理员权限。启动后默认驻留托盘，不弹出设置窗口。
2. 在右下角托盘中找到三棱镜图标。右键菜单提供「立即息屏」「设置」「退出幕黑」，双击图标打开设置。
3. 默认全局快捷键为 **Ctrl + Alt + B**，即使正在使用其他程序也可触发息屏。
4. 在设置中点击快捷键输入框，按下想用的组合键并保存。支持 Ctrl 或 Alt（至少一个），可加 Shift，配合英文字母、数字或 F1–F24。快捷键被占用时会提示，原快捷键保持可用。
5. 黑屏期间直接连续输入 **zcz**（或自定义字符），不必回车，即可恢复。恢复后继续驻留托盘。

Windows 可能把新图标放进右下角 `^` 折叠菜单。可将图标拖到可见区域，或在系统任务栏设置中开启显示；应用不能替用户强制固定系统托盘图标。关闭设置窗口只会隐藏它，彻底退出请使用托盘菜单。软件不会自动设置开机启动。

## 解锁、动画与睡眠

- 解锁字符支持 1–32 位英文字母或数字，不区分大小写，默认 `zcz`。按英文字母键识别，不依赖中文输入法。错误输入不会解锁，输错后重新连续输入完整字符即可。按住一个键不算重复字符，非字母数字键会中断输入序列。
- 所有显示器同步播放三秒动画：白光射入、彩虹展开、整体渐暗；三秒后保持纯黑并停止动画绘制。图标和动画采用附件三棱镜分光构图的程序重绘版本，不是原图的逐像素复制。
- 动画期间也可输入解锁字符退出。重复触发息屏不会叠加窗口或重新计时。
- 睡眠时间默认为 30 分钟，可设置 0–1440 分钟。0 表示不自动睡眠。**三秒动画结束后开始睡眠计时**，错误输入不会重置计时。
- 程序在等待期间阻止系统因空闲提前关闭屏幕或睡眠；合盖、电源按钮和系统策略仍可能影响电源状态。
- 到时只申请一次睡眠；唤醒后仍需输入解锁字符，不会循环触发睡眠。Windows 拒绝睡眠时会恢复设置窗口并提示。
- 忘记字符可按 **Ctrl + Alt + Shift + F12** 紧急解除黑屏。该组合不能设为息屏快捷键。也可通过 Windows 的 Ctrl + Alt + Delete 打开任务管理器结束程序。

本工具用于视觉遮屏，不是安全锁屏。纯黑覆盖不会真正关闭背光；系统安全桌面及系统级提示可以覆盖普通应用窗口。

## 设置与升级

设置位于 `%LOCALAPPDATA%\MuHei\settings.json`。v1.0 的解锁字符和睡眠时间会保留，并自动采用默认息屏快捷键。使用托盘及全局快捷键时采用已保存的设置，设置窗口内未保存的编辑不影响它们。

升级前先通过旧版托盘菜单退出；v1.0 用户请关闭旧版设置窗口。然后解压新版到新目录启动。不要同时运行两版。

双击 `立即黑屏.cmd` 或运行 `BlackScreen.exe --black` 可启动后直接播放息屏动画。程序不记录或上传键盘输入；只有黑屏期间监听输入，解除后立即移除监听，平时仅向 Windows 注册全局快捷键。解锁字符是本地普通设置，不应使用真实账户密码。

## 开发与打包

使用 .NET 10 SDK，无第三方运行时包依赖。

```powershell
# 跨平台核心测试
dotnet run --project tests/BlackScreen.Core.Tests -c Release

# Windows 集成测试（图形渲染、快捷键注册、窗口生命周期）
dotnet run --project tests/BlackScreen.Windows.Tests -c Release

# Windows 上运行应用（启动后查看系统托盘）
dotnet run --project src/BlackScreen

# 生成独立 EXE 和 ZIP
./scripts/publish.ps1
./scripts/publish.ps1 -Runtime win-arm64
```

产物在 `artifacts/`。GitHub Actions 在 Windows 上运行测试和构建。以 `v` 开头的版本标签会在构建成功后发布 GitHub Release。Windows 集成测试生成的动画关键帧保存在 Actions 的 `Windows-animation-frames` 附件中。单文件版首次启动由 .NET 自动解压必要运行库至用户临时目录。

Linux 可执行核心测试和 Windows 交叉编译，不能原生运行 WinForms：

```sh
dotnet publish src/BlackScreen/BlackScreen.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -p:DebugSymbols=false -o artifacts/win-x64
```

## Windows 实机验收

自动测试不能替代真实设备的输入和电源验收：

- 启动后仅托盘可见，右键可息屏、设置、退出；设置窗口关闭再打开正常。
- 在其他应用中使用默认和自定义快捷键，确认只触发一次；测试占用冲突、保存失败时旧快捷键保持有效。
- 用快捷键启用后松开 Ctrl / Alt，解锁后其他程序没有残留按键状态。
- 多屏、不同 DPI、竖屏和显示器热插拔时均正确覆盖；动画结束后没有残留发光像素。
- 动画期间、纯黑期间均可解锁；错误字符、鼠标点击不解锁；紧急组合键可恢复。
- 设置 1 分钟，从动画结束计时，确认真实睡眠与唤醒；设置 0 时不自动睡眠。解除后恢复系统原有空闲电源行为。
- 检查 Windows 资源管理器、设置窗口和托盘均使用三棱镜图标，退出后托盘与快捷键被清理。
