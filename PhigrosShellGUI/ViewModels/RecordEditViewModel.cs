using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhigrosArchive.Save.Data;

namespace PhigrosShellGUI.ViewModels;

/// <summary>编辑对话框中的单难度字段</summary>
public partial class DifficultyFieldViewModel : ViewModelBase
{
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private bool _exists;
    [ObservableProperty] private string _score = "0";
    [ObservableProperty] private string _acc = "0";
    [ObservableProperty] private bool _fc;
    [ObservableProperty] private string _difficultyConstant = "-";

    /// <summary>父 ViewModel 设置的移除回调</summary>
    public Action<DifficultyFieldViewModel>? RemoveRequested { get; set; }

    [RelayCommand]
    private void RemoveSelf()
    {
        RemoveRequested?.Invoke(this);
    }
}

/// <summary>歌曲成绩编辑 Overlay 的 ViewModel</summary>
public partial class RecordEditViewModel : ViewModelBase
{
    [ObservableProperty] private string _songId = string.Empty;

    public string Title => $"{SongId} 成绩编辑";

    partial void OnSongIdChanged(string value) => OnPropertyChanged(nameof(Title));

    [ObservableProperty] private ObservableCollection<DifficultyFieldViewModel> _items = new();
    [ObservableProperty] private string _errorMessage = string.Empty;

    [ObservableProperty] private string? _selectedDifficultyToAdd;
    public ObservableCollection<string> AvailableDifficulties { get; } = new();

    public bool HasAvailableDifficulties => AvailableDifficulties.Count > 0;
    public bool CanAddDifficulty => SelectedDifficultyToAdd != null;

    partial void OnSelectedDifficultyToAddChanged(string? value)
    {
        OnPropertyChanged(nameof(CanAddDifficulty));
    }

    /// <summary>编辑完成事件（参数：是否确认）</summary>
    public event EventHandler<bool>? EditCompleted;

    /// <summary>确认后返回的结果，null 表示取消</summary>
    public PhiDifficultyInfo<PhiLevelRecord?>? Result { get; private set; }

    public static RecordEditViewModel FromRecord(string songId, PhiDifficultyInfo<PhiLevelRecord?> record)
    {
        var vm = new RecordEditViewModel { SongId = songId };

        vm.AddField("EZ", record.EZ);
        vm.AddField("HD", record.HD);
        vm.AddField("IN", record.IN);
        vm.AddField("AT", record.AT);
        vm.AddField("Legacy", record.Legacy);

        vm.RefreshAvailableDifficulties();
        return vm;
    }

    private void AddField(string name, PhiLevelRecord? level)
    {
        var field = new DifficultyFieldViewModel { Name = name };
        field.RemoveRequested = RemoveDifficulty;

        if (level != null)
        {
            field.Exists = true;
            field.Score = level.Score.ToString();
            field.Acc = level.Acc.ToString("F2");
            field.Fc = level.Fc;
            field.DifficultyConstant = level.Difficulty?.ToString("F1") ?? "-";
        }

        Items.Add(field);
    }

    /// <summary>刷新可添加的难度列表</summary>
    public void RefreshAvailableDifficulties()
    {
        AvailableDifficulties.Clear();
        var all = new[] { "EZ", "HD", "IN", "AT", "Legacy" };
        foreach (var name in all)
        {
            var field = Items.FirstOrDefault(f => f.Name == name);
            if (field == null || !field.Exists)
                AvailableDifficulties.Add(name);
        }

        if (SelectedDifficultyToAdd != null && !AvailableDifficulties.Contains(SelectedDifficultyToAdd))
            SelectedDifficultyToAdd = null;

        OnPropertyChanged(nameof(HasAvailableDifficulties));
    }

    /// <summary>移除一个难度（设为不显示）</summary>
    public void RemoveDifficulty(DifficultyFieldViewModel field)
    {
        field.Exists = false;
        field.Score = "0";
        field.Acc = "0";
        field.Fc = false;
        RefreshAvailableDifficulties();
    }

    [RelayCommand]
    private void AddDifficulty()
    {
        if (string.IsNullOrEmpty(SelectedDifficultyToAdd)) return;

        var field = Items.FirstOrDefault(f => f.Name == SelectedDifficultyToAdd);
        if (field != null)
        {
            field.Exists = true;
            field.Score = "0";
            field.Acc = "0";
            field.Fc = false;
            SelectedDifficultyToAdd = null;
            RefreshAvailableDifficulties();
        }
    }

    [RelayCommand]
    private void Confirm()
    {
        var records = new PhiLevelRecord?[5];
        bool allValid = true;
        var errors = new System.Text.StringBuilder();

        for (int i = 0; i < Math.Min(Items.Count, 5); i++)
        {
            var field = Items[i];
            if (!field.Exists) continue;

            if (!int.TryParse(field.Score, out int score) || score < 0 || score > 1000000)
            {
                errors.AppendLine($"{field.Name}: 分数需为 0-1000000 的整数");
                allValid = false;
                continue;
            }

            if (!float.TryParse(field.Acc, out float acc) || acc < 0 || acc > 100)
            {
                errors.AppendLine($"{field.Name}: ACC 需为 0-100 的数字");
                allValid = false;
                continue;
            }

            records[i] = new PhiLevelRecord(score, acc, field.Fc);
        }

        if (!allValid)
        {
            ErrorMessage = errors.ToString();
            return;
        }

        ErrorMessage = string.Empty;
        Result = new PhiDifficultyInfo<PhiLevelRecord?>(records[0], records[1], records[2], records[3], records[4]);
        EditCompleted?.Invoke(this, true);
    }

    [RelayCommand]
    private void Cancel()
    {
        Result = null;
        EditCompleted?.Invoke(this, false);
    }
}
