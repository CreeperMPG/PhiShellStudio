using Avalonia.Controls;
using Avalonia.Input;
using PhigrosShellGUI.ViewModels;

namespace PhigrosShellGUI.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    private void OnSlotPointerPressed(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Border border && border.Tag is SaveSlotBrief slot)
        {
            if (DataContext is MainViewModel vm)
                vm.SelectSlotCommand.Execute(slot);
        }
    }
}
