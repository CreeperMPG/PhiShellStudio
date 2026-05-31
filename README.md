# PhiShell Studio

基于 **Avalonia UI** 的 Phigros 存档管理图形界面工具，支持 **桌面端** 和 **移动端**。

## 平台支持

| 平台 | 目标框架 | 架构 | 项目 |
|------|---------|------|------|
| 💻 Windows | `net10.0` | `win-x64` | `PhigrosShellGUI.Desktop` |
| 🐧 Linux | `net10.0` | `linux-x64` | `PhigrosShellGUI.Desktop` |
| 📱 Android | `net10.0-android` | `arm64-v8a` / `armeabi-v7a` / `x86_64` | `PhigrosShellGUI.Android` |
| 🍎 iOS | `net10.0-ios` | `ios-arm64` | `PhigrosShellGUI.iOS` |

## 构建

### 桌面端

```bash
# Windows
dotnet publish PhigrosShellGUI.Desktop -f net10.0 -c Release -r win-x64 --self-contained

# Linux
dotnet publish PhigrosShellGUI.Desktop -f net10.0 -c Release -r linux-x64 --self-contained
```

### Android

```bash
# ARM64 (CoreCLR)
dotnet publish PhigrosShellGUI.Android -f net10.0-android -c Release -r android-arm64 --self-contained

# armeabi-v7a (Mono 运行时)
dotnet publish PhigrosShellGUI.Android -f net10.0-android -c Release -r android-arm --self-contained -p:UseMonoRuntime=true

# x86_64 (CoreCLR)
dotnet publish PhigrosShellGUI.Android -f net10.0-android -c Release -r android-x64 --self-contained
```

> 也提供了 `build-android.bat` 一键构建全部架构。

### iOS

```bash
dotnet publish PhigrosShellGUI.iOS -f net10.0-ios -c Release -r ios-arm64 --self-contained -p:ArchiveOnBuild=true -p:BuildIpa=true
```

> iOS 需要 macOS + Xcode 环境，GitHub Actions workflow 已配置好。

## 应用配置

- **设置文件：** `%LOCALAPPDATA%/PhigrosShellGUI/settings.json`
- **定数文件：** `{AppData}/PhigrosShellGUI/difficulty.tsv`
- 可通过 `IDifficultyProvider` 自定义定数数据源

## NuGet 依赖

- `Avalonia` v12.0.3 + Desktop / Android / iOS
- `Avalonia.Themes.Fluent` + `Avalonia.Fonts.Inter`
- `CommunityToolkit.Mvvm` — MVVM 框架
- `FluentIcons.Avalonia` — 图标库
- `ZXing.Net` — 二维码生成

## 相关仓库

- [PhigrosArchive](https://github.com/CreeperMPG/PhigrosArchive) — 基础库依赖
