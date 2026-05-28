using System;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhigrosArchive.Save.Data;

namespace PhigrosShellGUI.ViewModels;

/// <summary>
/// Data 编辑对话框的 ViewModel。
/// 支持两种输入方式（分量 / 总 KB），双向同步，超 1024 自动进位。
/// </summary>
public partial class DataEditViewModel : ViewModelBase
{
    /// <summary>确认后返回的结果，null 表示取消</summary>
    public PhiMoney? Result { get; private set; }

    /// <summary>编辑完成事件，参数为是否确认</summary>
    public event EventHandler<bool>? EditCompleted;

    // ── 分量输入（使用长命名避免与系统类型冲突）──
    [ObservableProperty] private string _valueKB = "0";
    [ObservableProperty] private string _valueMB = "0";
    [ObservableProperty] private string _valueGB = "0";
    [ObservableProperty] private string _valueTB = "0";
    [ObservableProperty] private string _valuePB = "0";

    // ── 总 KB 输入 ──
    [ObservableProperty] private string _totalKB = "0";

    // ── 验证状态 ──
    [ObservableProperty] private bool _isPerUnitValid = true;
    [ObservableProperty] private bool _isTotalKBValid = true;
    [ObservableProperty] private string _errorMessage = string.Empty;

    /// <summary>防递归同步锁</summary>
    private bool _suppressSync;

    public DataEditViewModel(int kb, int mb, int gb, int tb, int pb)
    {
        long total = kb + mb * 1024L + gb * 1024L * 1024
                   + tb * 1024L * 1024 * 1024
                   + pb * 1024L * 1024 * 1024 * 1024;

        var money = new PhiMoney(total);
        ValueKB = money.KB.ToString();
        ValueMB = money.MB.ToString();
        ValueGB = money.GB.ToString();
        ValueTB = money.TB.ToString();
        ValuePB = money.PB.ToString();
        TotalKB = money.TotalKB.ToString();

        IsPerUnitValid = true;
        IsTotalKBValid = true;

        // 监听属性变化以实现双向同步
        PropertyChanged += OnPropertyChangedHandler;
    }

    private void OnPropertyChangedHandler(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ValueKB):
            case nameof(ValueMB):
            case nameof(ValueGB):
            case nameof(ValueTB):
            case nameof(ValuePB):
                SyncFromPerUnit();
                break;

            case nameof(TotalKB):
                SyncFromTotal();
                break;
        }
    }

    // ════════════════════════════════════════
    //  同步逻辑（用户输入时自动触发）
    // ════════════════════════════════════════

    /// <summary>从分量同步到总 KB，并自动进位</summary>
    private void SyncFromPerUnit()
    {
        if (_suppressSync) return;

        if (!TryParseUnits(out long total, out string? err))
        {
            IsPerUnitValid = false;
            ErrorMessage = err!;
            return;
        }

        IsPerUnitValid = true;
        ErrorMessage = string.Empty;

        _suppressSync = true;
        var money = new PhiMoney(total);
        ValueKB = money.KB.ToString();
        ValueMB = money.MB.ToString();
        ValueGB = money.GB.ToString();
        ValueTB = money.TB.ToString();
        ValuePB = money.PB.ToString();
        TotalKB = total.ToString();
        _suppressSync = false;
    }

    /// <summary>从总 KB 同步到分量</summary>
    private void SyncFromTotal()
    {
        if (_suppressSync) return;

        if (!long.TryParse(TotalKB, out var total) || total < 0)
        {
            IsTotalKBValid = false;
            ErrorMessage = "请输入非负整数";
            return;
        }

        IsTotalKBValid = true;
        ErrorMessage = string.Empty;

        _suppressSync = true;
        var money = new PhiMoney(total);
        ValueKB = money.KB.ToString();
        ValueMB = money.MB.ToString();
        ValueGB = money.GB.ToString();
        ValueTB = money.TB.ToString();
        ValuePB = money.PB.ToString();
        _suppressSync = false;
    }

    // ════════════════════════════════════════
    //  确认 / 取消
    // ════════════════════════════════════════

    [RelayCommand]
    private void Confirm()
    {
        if (TryConfirm()) EditCompleted?.Invoke(this, true);
    }

    [RelayCommand]
    private void Cancel()
    {
        CancelEdit();
        EditCompleted?.Invoke(this, false);
    }

    /// <summary>
    /// 调用此方法以确认编辑。
    /// 返回 true 表示输入有效且已设置 Result。
    /// </summary>
    public bool TryConfirm()
    {
        if (TryParseUnits(out long total, out _) || long.TryParse(TotalKB, out total))
        {
            if (total < 0)
            {
                ErrorMessage = "数值不能为负";
                return false;
            }
            Result = new PhiMoney(total);
            return true;
        }

        ErrorMessage = "请输入有效的整数";
        return false;
    }

    /// <summary>取消：Result 置空</summary>
    public void CancelEdit()
    {
        Result = null;
    }

    // ════════════════════════════════════════
    //  辅助
    // ════════════════════════════════════════

    private bool TryParseUnits(out long total, out string? error)
    {
        total = 0;
        error = null;

        if (!TryParseInt(ValueKB, "KB", out int kb, out error)) return false;
        if (!TryParseInt(ValueMB, "MB", out int mb, out error)) return false;
        if (!TryParseInt(ValueGB, "GB", out int gb, out error)) return false;
        if (!TryParseInt(ValueTB, "TB", out int tb, out error)) return false;
        if (!TryParseInt(ValuePB, "PB", out int pb, out error)) return false;

        total = kb
              + mb * 1024L
              + gb * 1024L * 1024
              + tb * 1024L * 1024 * 1024
              + pb * 1024L * 1024 * 1024 * 1024;

        return true;
    }

    private static bool TryParseInt(string text, string name, out int value, out string? error)
    {
        value = 0;
        error = null;

        if (string.IsNullOrWhiteSpace(text))
        {
            value = 0;
            return true;
        }

        if (!int.TryParse(text, out value) || value < 0)
        {
            error = $"{name} 请输入非负整数";
            return false;
        }

        return true;
    }
}
