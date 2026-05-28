using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;

namespace PhigrosShellGUI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // 设置窗口图标
        try
        {
            using var stream = AssetLoader.Open(new Uri("avares://PhigrosShellGUI/Assets/phishellstudio.ico"));
            Icon = new WindowIcon(stream);
        }
        catch
        {
            // 加载图标失败时静默处理，使用默认图标
        }
    }
}
