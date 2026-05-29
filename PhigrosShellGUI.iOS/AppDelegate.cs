using Foundation;
using UIKit;
using Avalonia;
using Avalonia.iOS;

namespace PhigrosShellGUI.iOS;

/// <summary>
/// iOS 应用委托，由 Avalonia.iOS 驱动。
/// </summary>
[Register("AppDelegate")]
public class AppDelegate : AvaloniaAppDelegate<App>
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder);
    }
}
