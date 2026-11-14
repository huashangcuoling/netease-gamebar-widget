# 网易云迷你播放器 · Xbox Game Bar Widget

> A compact 377×65 Xbox Game Bar widget that controls NetEase Cloud Music through Windows SMTC.
>
> 一个 377×65 的紧凑 Game Bar 小组件，用 Windows SMTC 读取并控制网易云音乐。

![preview](docs/preview.png)

> 上图为静态设计示意，不是 Game Bar 实机截图；示例歌曲文本仅用于展示排版。

## 功能

- 377×65 固定尺寸的紧凑卡片，适合固定在屏幕角落。
- 显示专辑封面、歌曲名、歌手。
- 上一首 / 播放暂停 / 下一首。
- 底部细进度条，按播放状态自动推进。
- UWP `AcrylicBrush` 半透明毛玻璃卡片。
- 监听系统 SMTC，不依赖网易云内部接口，也不做进程注入。
- 自动跟随网易云音乐的 SMTC 会话；切歌、暂停后 UI 会同步更新。
- 普通启动时显示设计预览页，方便在 Visual Studio 里调 UI。

## 已验证环境

- Windows 11 26H2（Build 26300.9974）
- Xbox Game Bar 7.326.11042.0
- 网易云音乐 3.1.43.205627（`cloudmusic.exe`）
- Visual Studio Community 2026 + Windows 11 SDK 10.0.26100
- 已完成：解决方案编译、VS Debug 启动、SMTC 歌曲读取、切歌/暂停控制

## 环境要求

### 运行

1. Windows 11。
2. Xbox Game Bar 已安装，`Win+G` 可以打开。
3. 网易云音乐已登录并正在播放，且系统媒体控制里能看到它的 SMTC 会话。

### 开发/编译

1. Visual Studio 2022 或更新版本。
2. **UWP / WinUI 构建工作负载**：
   - 完整 Visual Studio：工作负载里选 **WinUI application development**，右侧“安装详细信息”勾选 **Universal Windows Platform tools** 和 **Windows 11 SDK (10.0.26100.x)**。
   - 只装 Build Tools：工作负载里选 **WinUI application development build tools**，右侧确认 **.NET WinUI app development build tools** 和 **Windows 11 SDK**。
3. 开发阶段建议开启 Windows 开发者模式：`设置 → 系统 → 开发者选项 → 开发人员模式`。

> 从 Visual Studio 2022 17.10 起，原来的顶层“UWP 开发”工作负载被合并/改名，所以找不到“通用 Windows 平台开发”是正常的，找 **WinUI** 即可。

仓库根目录的 `.vsconfig` 已按 Build Tools 的 `Microsoft.VisualStudio.Workload.UniversalBuildTools` 写好，可在 Visual Studio Installer 里导入。

## 安装

### 方式一：Visual Studio 调试安装（推荐）

1. 克隆仓库：

```powershell
git clone https://github.com/huashangcuoling/netease-gamebar-widget.git
cd netease-gamebar-widget
```

2. 用 Visual Studio 打开：

```text
NeteaseGameBarWidget.sln
```

3. 顶部选择 `Debug | x64`，按 **F5**。
4. 第一次运行会自动部署依赖并注册应用；如果弹出开发者模式提示，按提示开启。
5. 按 `Win+G` 打开 Game Bar，点击顶部的小组件菜单，添加 **网易云迷你播放器**。
6. 在小组件标题栏点击图钉，把它固定到屏幕。

> 如果小组件没有出现在菜单里，完全退出 `GameBar.exe`（任务管理器结束进程）再重新打开 Game Bar。

### 方式二：命令行构建 + 开发版注册

```powershell
cd <repo>
.\tools\Build.ps1 -Configuration Debug -Platform x64
.\tools\Install-Dev.ps1
```

`Build.ps1` 会先找装有 UWP 工作负载的 Visual Studio；`Install-Dev.ps1` 会注册 `bin\x64\Debug\AppX\AppxManifest.xml`。

> `Install-Dev.ps1` 依赖 Visual Studio 部署生成的 `AppX` 目录。如果命令行构建后没有这个目录，请先在 Visual Studio 里按一次 F5，或使用下面的自签名 MSIX。

### 方式三：自签名 MSIX

适合不想开 Visual Studio、或需要在另一台机器安装的情况。

```powershell
# 只生成一次证书；会装进 CurrentUser\TrustedPeople
.\tools\New-DevCert.ps1

# 用同一张证书打包
.\tools\Pack.ps1 -Configuration Release -Platform x64
```

然后安装 `AppPackages` 下生成的 `.msix`。

如果提示发布者不受信任，把 `build\NeteaseGameBarWidget.cer` 安装到 `LocalMachine\TrustedPeople`（需要管理员权限）。

> 证书 Subject 必须和 `Package.appxmanifest` 的 `Publisher` 完全一致，本项目固定为 `CN=NeteaseGameBarWidget`。

> 如果 `New-SelfSignedCertificate` 在 PowerShell 7 里不可用，请用 Windows PowerShell 5.1 运行该脚本：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tools\New-DevCert.ps1
```

### 方式四：GitHub Releases 安装包

如果仓库的 Releases 页面提供了 `NeteaseGameBarWidget_*.msix` 和证书：

1. 把 `.cer` 证书安装到“本地计算机 → 受信任人”（需要管理员）。
2. 双击 `.msix` 安装。
3. 按 `Win+G` 添加小组件。

> 自签名证书只适合个人/小范围分发；公开分发建议使用受信任的代码签名证书。

### 卸载

```powershell
Get-AppxPackage -Name NeteaseGameBarWidget | Remove-AppxPackage
```

或运行：

```powershell
.\tools\Uninstall.ps1
```

## 使用

1. 先启动网易云音乐并播放歌曲。
2. 按 `Win+G` 打开 Game Bar。
3. 在小组件菜单中添加“网易云迷你播放器”。
4. 点击图钉固定。
5. 卡片上的按钮会直接控制网易云的 SMTC 会话。

## 工作原理

### SMTC

核心代码在 [Services/SmtcService.cs](Services/SmtcService.cs)。

```csharp
_manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
```

通过 `SourceAppUserModelId` 中含 `cloudmusic` 的会话来识别网易云：

```csharp
source.IndexOf("cloudmusic", StringComparison.OrdinalIgnoreCase) >= 0
```

监听：

- `CurrentSessionChanged`
- `SessionsChanged`
- `MediaPropertiesChanged`
- `PlaybackInfoChanged`
- `TimelinePropertiesChanged`

控制：

- `TryTogglePlayPauseAsync()`
- `TrySkipPreviousAsync()`
- `TrySkipNextAsync()`

封面来自 `MediaProperties.Thumbnail`。

### 受限能力

`Package.appxmanifest` 声明了：

```xml
<uap7:Capability Name="globalMediaControl" />
```

这是读取其他应用 SMTC 会话所需的受限能力。**自签名旁加载在部分 Windows 版本上可能部署成功但运行时拒绝访问**。如果遇到“无法读取网易云 / SMTC 初始化失败”，优先尝试：

1. 用 Visual Studio F5 部署；
2. 使用受信任签名的 MSIX；
3. 仍有问题则考虑增加 full-trust 辅助进程读取 SMTC，再通过 AppService/命名管道传给 widget。

### 毛玻璃

卡片使用 UWP `AcrylicBrush`：

```xml
<AcrylicBrush BackgroundSource="HostBackdrop"
              TintColor="#141414"
              TintOpacity="0.52"
              FallbackColor="#F0141414" />
```

Game Bar 前台打开时，宿主可能使用 fallback 颜色；固定小组件并关闭 Game Bar 后，透明效果可能不同。实际效果以机器和 Game Bar 版本为准。

## 项目结构

```text
NeteaseGameBarWidget/
├─ App.xaml / App.xaml.cs           # Game Bar 激活、377×65 窗口尺寸
├─ WidgetPage.xaml / .cs            # Game Bar 宿主页
├─ WidgetView.xaml / .cs            # 卡片 UI、封面、按钮、进度
├─ MainPage.xaml / .cs              # 普通启动时的设计预览
├─ Services/SmtcService.cs          # SMTC 会话发现、监听、控制
├─ Models/SmtcSnapshot.cs           # UI 状态模型
├─ Package.appxmanifest             # Game Bar 扩展、globalMediaControl
├─ Assets/                          # MSIX 图标
├─ GameBar/WidgetIcon.png           # Game Bar 小组件图标
├─ docs/preview.png                 # 设计预览图
├─ tools/                           # 构建、安装、签名、资源生成脚本
└─ NeteaseGameBarWidget.sln
```

## 常见问题

### 小组件菜单里没有“网易云迷你播放器”

- 确认 Visual Studio 已成功 F5 部署。
- 完全退出 `GameBar.exe` 后重新打开。
- 确认 Game Bar 版本支持 `microsoft.gameBarUIExtension`；本项目在 7.326 上验证过。

### Game Bar 显示“此小组件有问题”

- 多数是旧进程/旧连接残留：结束 `GameBar.exe` 和 `GameBarFTServer.exe`，重新打开。
- 确认没有手动杀掉正在运行的 `NeteaseGameBarWidget.exe` 后再点旧卡片。

### 显示“无法读取网易云”

- 确认网易云正在播放，系统音量控制中心能看到歌曲。
- 确认清单包含 `globalMediaControl`。
- 尝试 Visual Studio F5 部署或受信任签名，排除旁加载限制。

### VS 提示 .NET Native 已弃用

这是 VS 2026 的信息栏提示，不是构建错误。当前 Game Bar SDK 仍是 UWP XAML + WinRT 组件，项目暂时保留 UWP/.NET Native 以保持 Game Bar 激活链路。迁移到 .NET 10 + Native AOT 属于单独的项目模型迁移。

### 找不到 UWP 工作负载

在 Visual Studio Installer 里找 **WinUI application development**（完整 VS）或 **WinUI application development build tools**（Build Tools），不要再找旧名称“UWP 开发”。

## 已知限制

- Game Bar 会给小组件加自己的标题栏和边距，65px 是内容区高度。
- `HostBackdrop` 在 Game Bar 宿主里的透明表现取决于版本和固定状态。
- `globalMediaControl` 的旁加载策略可能需要受信任签名。
- 只匹配 AUMID 中含 `cloudmusic` 的会话；网易云改进程名时需要调整 `SmtcService.IsNetease`。
- 目前没有公开的商店签名安装包，仓库以源码 + 开发版安装为主。

## 参考

- [Xbox Game Bar SDK 入门](https://learn.microsoft.com/en-us/xbox/game-bar/quickstart/introduction)
- [官方示例](https://github.com/microsoft/XboxGameBarSamples)
- [Microsoft.Gaming.XboxGameBar NuGet](https://www.nuget.org/packages/Microsoft.Gaming.XboxGameBar)
- [Compact mode 尺寸](https://learn.microsoft.com/en-us/xbox/game-bar/guide/resizing-widgets-compact-mode)
- [SMTC API](https://learn.microsoft.com/en-us/uwp/api/windows.media.control.globalsystemmediatransportcontrolssessionmanager)

## 说明

本项目是个人开发的小工具，与网易云音乐、Microsoft、Xbox 没有隶属关系。网易云音乐和 Xbox Game Bar 的商标归各自所有者所有。

## Changelog

见 [CHANGELOG.md](CHANGELOG.md)。

## License

[MIT](LICENSE)
