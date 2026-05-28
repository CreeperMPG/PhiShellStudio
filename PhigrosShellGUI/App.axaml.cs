using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using PhigrosShellGUI.ViewModels;
using PhigrosShellGUI.Views;

namespace PhigrosShellGUI;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
            MainWindowHelper.Instance = mainWindow;
            desktop.MainWindow = mainWindow;
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            var mainVm = new MainWindowViewModel();
            var appView = new AppView
            {
                DataContext = mainVm,
            };
            MainWindowHelper.MainViewInstance = appView;
            singleView.MainView = appView;
        }

        base.OnFrameworkInitializationCompleted();
    }
}