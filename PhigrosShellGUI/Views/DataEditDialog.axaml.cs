using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using PhigrosArchive.Save.Data;
using PhigrosShellGUI.ViewModels;

namespace PhigrosShellGUI.Views;

public partial class DataEditDialog : Window
{
    public DataEditDialog()
    {
        InitializeComponent();

        // 确认
        ConfirmButton.Click += OnConfirmClick;

        // 取消
        CancelButton.Click += OnCancelClick;

        // 每个分量输入框 LostFocus 时触发同步（包含进位）
        PbBox.LostFocus += (_, _) => SyncFromPerUnit();
        TbBox.LostFocus += (_, _) => SyncFromPerUnit();
        GbBox.LostFocus += (_, _) => SyncFromPerUnit();
        MbBox.LostFocus += (_, _) => SyncFromPerUnit();
        KbBox.LostFocus += (_, _) => SyncFromPerUnit();
    }

    /// <summary>显示对话框，返回编辑后的 PhiMoney，取消返回 null</summary>
    public static async Task<PhiMoney?> ShowAsync(Window owner, int kb, int mb, int gb, int tb, int pb)
    {
        var dialog = new DataEditDialog
        {
            DataContext = new DataEditViewModel(kb, mb, gb, tb, pb)
        };

        await dialog.ShowDialog(owner);
        return dialog.DataContext is DataEditViewModel vm ? vm.Result : null;
    }

    private void SyncFromPerUnit()
    {
        if (DataContext is DataEditViewModel vm)
        {
            // 重新设置各分量值，触发 ViewModel 的同步逻辑和自动进位
            vm.ValuePB = PbBox.Text ?? "0";
            vm.ValueTB = TbBox.Text ?? "0";
            vm.ValueGB = GbBox.Text ?? "0";
            vm.ValueMB = MbBox.Text ?? "0";
            vm.ValueKB = KbBox.Text ?? "0";
        }
    }

    private void OnConfirmClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is DataEditViewModel vm)
        {
            // 确认前确保同步
            SyncFromPerUnit();

            if (vm.TryConfirm())
            {
                Close();
            }
        }
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is DataEditViewModel vm)
        {
            vm.CancelEdit();
        }
        Close();
    }
}
