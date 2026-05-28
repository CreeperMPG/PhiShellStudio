using Avalonia;
using System;

namespace PhigrosShellGUI.Desktop;

sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
        => AppBuilderHelper.BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
}
