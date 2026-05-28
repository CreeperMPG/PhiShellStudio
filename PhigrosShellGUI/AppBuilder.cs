using Avalonia;
using System;

namespace PhigrosShellGUI;

/// <summary>
/// Avalonia 应用构建器。由各平台启动项目调用。
/// </summary>
public static class AppBuilderHelper
{
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
