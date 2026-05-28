using System;
using PhigrosArchive.Save.Data;

namespace PhigrosShellGUI.ViewModels;

/// <summary>
/// SongRecordsViewModel 发起歌曲成绩编辑时的事件参数。
/// 携带当前难度数据和编辑完成回调。
/// </summary>
public class RecordEditStartingEventArgs : EventArgs
{
    public required string SongId { get; init; }
    public required PhiDifficultyInfo<PhiLevelRecord?> CurrentRecord { get; init; }
    public required Action<PhiDifficultyInfo<PhiLevelRecord?>?> OnEditCompleted { get; init; }
}
