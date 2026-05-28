using Android.App;
using Android.Content.PM;
using Avalonia;
using Avalonia.Android;

namespace PhigrosShellGUI.Android;

[Activity(
    Label = "PhiShell Studio",
    Theme = "@style/MyTheme.NoActionBar",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation
        | ConfigChanges.ScreenSize
        | ConfigChanges.UiMode
        | ConfigChanges.ScreenLayout
        | ConfigChanges.SmallestScreenSize
        | ConfigChanges.Density)]
public class MainActivity : AvaloniaMainActivity
{
}
