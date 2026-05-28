using System.Windows.Input;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using PhigrosArchive.Save.Data;
using System;

namespace PhigrosShellGUI.ViewModels;

/// <summary>单首歌曲的单难度显示数据</summary>
public partial class DifficultyDisplayItem : ViewModelBase
{
    [ObservableProperty] private string _difficultyName = string.Empty;
    [ObservableProperty] private bool _exists;
    [ObservableProperty] private string _score = "-";
    [ObservableProperty] private string _acc = "-";
    [ObservableProperty] private string _rank = "-";
    [ObservableProperty] private bool _fc;
    [ObservableProperty] private string _difficultyConstant = "-";
}

/// <summary>歌曲成绩卡片的 ViewModel</summary>
public partial class SongRecordViewModel : ViewModelBase
{
    [ObservableProperty] private string _songId = string.Empty;
    [ObservableProperty] private ObservableCollection<DifficultyDisplayItem> _difficulties = new();

    /// <summary>编辑命令（由父级 SongRecordsViewModel 注入）</summary>
    public ICommand? EditCommand { get; set; }

    /// <summary>此歌曲原始数据引用（编辑确认后直接修改该对象）</summary>
    internal PhiDifficultyInfo<PhiLevelRecord?>? RecordData { get; set; }

    internal static SongRecordViewModel FromRecord(string songId, PhiDifficultyInfo<PhiLevelRecord?> diffInfo)
    {
        var vm = new SongRecordViewModel
        {
            SongId = songId,
            RecordData = diffInfo,
        };

        vm.AddDifficulty("EZ", diffInfo.EZ, 0);
        vm.AddDifficulty("HD", diffInfo.HD, 1);
        vm.AddDifficulty("IN", diffInfo.IN, 2);
        if (diffInfo.AT != null) vm.AddDifficulty("AT", diffInfo.AT, 3);
        if (diffInfo.Legacy != null) vm.AddDifficulty("Legacy", diffInfo.Legacy, 4);

        return vm;
    }

    internal void AddDifficulty(string name, PhiLevelRecord? level, int _)
    {
        var item = new DifficultyDisplayItem { DifficultyName = name };

        if (level != null)
        {
            item.Exists = true;
            item.Score = level.Score.ToString();
            item.Acc = level.Acc.ToString("F2") + "%";
            item.Rank = level.Rank.ToString();
            item.Fc = level.Fc;
            item.DifficultyConstant = level.Difficulty?.ToString("F1") ?? "-";
        }

        Difficulties.Add(item);
    }

    /// <summary>编辑确认后刷新显示数据（从 RecordData 重新读取）</summary>
    internal void RefreshFromData()
    {
        if (RecordData == null) return;

        var levels = new[] { RecordData.EZ, RecordData.HD, RecordData.IN, RecordData.AT, RecordData.Legacy };
        for (int i = 0; i < Math.Min(levels.Length, Difficulties.Count); i++)
        {
            var display = Difficulties[i];
            var level = levels[i];
            if (level != null)
            {
                display.Exists = true;
                display.Score = level.Score.ToString();
                display.Acc = level.Acc.ToString("F2") + "%";
                display.Rank = level.Rank.ToString();
                display.Fc = level.Fc;
                display.DifficultyConstant = level.Difficulty?.ToString("F1") ?? "-";
            }
            else
            {
                display.Exists = false;
                display.Score = "-";
                display.Acc = "-";
                display.Rank = "-";
                display.Fc = false;
                display.DifficultyConstant = "-";
            }
        }
    }
}
