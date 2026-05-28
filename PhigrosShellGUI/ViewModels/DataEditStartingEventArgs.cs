using System;
using PhigrosArchive.Save.Data;

namespace PhigrosShellGUI.ViewModels;

/// <summary>
/// InfoOverviewViewModel 发起 Data 编辑时的事件参数。
/// 携带当前值和一个回调，编辑完成后通过回调传回结果。
/// </summary>
public class DataEditStartingEventArgs : EventArgs
{
    public int KB { get; init; }
    public int MB { get; init; }
    public int GB { get; init; }
    public int TB { get; init; }
    public int PB { get; init; }

    /// <summary>编辑完成回调，PhiMoney? 为 null 表示取消</summary>
    public required Action<PhiMoney?> OnEditCompleted { get; init; }
}
