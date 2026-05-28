using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhigrosShellGUI.Models;
using PhigrosShellGUI.Services;

namespace PhigrosShellGUI.ViewModels;

/// <summary>设置对话框 ViewModel</summary>
public partial class SettingsViewModel : ViewModelBase
{
    private readonly SettingsService _settingsService;

    /// <summary>关闭请求事件（true=确定保存，false=取消）</summary>
    public event Action<bool>? CloseRequested;

    // ════════════ 可绑定属性 ════════════

    /// <summary>所选主题 (Default / Light / Dark)</summary>
    [ObservableProperty]
    private string _selectedTheme = "Default";

    /// <summary>TSV 导入状态文字</summary>
    [ObservableProperty]
    private string _difficultyTsvStatus = "尚未导入 TSV 文件";

    /// <summary>是否已导入 TSV 文件（控制清除按钮可见性）</summary>
    [ObservableProperty]
    private bool _hasDifficultyTsv;

    // ════════════ 主题选项（静态） ════════════

    public static string[] ThemeOptions { get; } = ["Default", "Light", "Dark"];
    public static string[] ThemeLabels { get; } = ["跟随系统", "浅色", "深色"];

    public string SelectedThemeLabel
    {
        get => GetLabelForTheme(SelectedTheme);
        set
        {
            var idx = Array.IndexOf(ThemeLabels, value);
            if (idx >= 0)
                SelectedTheme = ThemeOptions[idx];
        }
    }

    // ════════════ 构造 ════════════

    public SettingsViewModel(SettingsService settingsService)
    {
        _settingsService = settingsService;
        var s = _settingsService.Load();
        _selectedTheme = s.Theme;

        RefreshDifficultyTsvStatus();
    }

    // ════════════ 命令 ════════════

    [RelayCommand]
    private void Confirm()
    {
        var s = new AppSettings
        {
            Theme = SelectedTheme,
        };
        _settingsService.Save(s);
        CloseRequested?.Invoke(true);
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseRequested?.Invoke(false);
    }

    /// <summary>导入 Difficulty.TSV 文件</summary>
    [RelayCommand]
    private async Task ImportDifficultyTsvAsync()
    {
        var topLevel = MainWindowHelper.Instance != null
            ? TopLevel.GetTopLevel(MainWindowHelper.Instance)
            : TopLevel.GetTopLevel(MainWindowHelper.MainViewInstance);
        if (topLevel == null) return;

        var options = new FilePickerOpenOptions
        {
            Title = "选择 Difficulty.TSV 文件",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("TSV 文件") { Patterns = ["*.tsv", "*.txt"] },
                new FilePickerFileType("所有文件") { Patterns = ["*"] },
            ],
        };

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(options);
        var file = files?.FirstOrDefault();
        if (file == null) return;

        // 复制到用户数据目录
        var result = await TsvStorageService.ImportFromStorageFileAsync(file);
        if (result == null)
        {
            DifficultyTsvStatus = "文件导入失败，请重试";
            HasDifficultyTsv = false;
            return;
        }

        RefreshDifficultyTsvStatus();
    }

    /// <summary>清除已导入的 TSV 文件</summary>
    [RelayCommand]
    private void ClearDifficultyTsv()
    {
        TsvStorageService.DeleteTsvFile();
        RefreshDifficultyTsvStatus();
    }

    // ════════════ 辅助 ════════════

    private void RefreshDifficultyTsvStatus()
    {
        var path = TsvStorageService.GetTsvFilePath();

        if (!TsvStorageService.TsvFileExists())
        {
            DifficultyTsvStatus = "尚未导入 TSV 文件";
            HasDifficultyTsv = false;
            return;
        }

        try
        {
            var provider = new DifficultyProviderFromTsv(path);
            if (provider.IsLoaded)
            {
                DifficultyTsvStatus = $"已导入，加载了 {provider.LoadedSongCount} 首歌曲定数";
                HasDifficultyTsv = true;
            }
            else
            {
                DifficultyTsvStatus = "文件已导入，但格式无效或为空";
                HasDifficultyTsv = true;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Settings] TSV parse failed: {ex.Message}");
            DifficultyTsvStatus = "文件已导入，但解析失败（文件可能已损坏）";
            HasDifficultyTsv = true;
        }
    }

    private static string GetLabelForTheme(string theme) => theme switch
    {
        "Light" => "浅色",
        "Dark" => "深色",
        _ => "跟随系统"
    };
}

/// <summary>
/// 辅助类，供 ViewModel 获取当前活动的顶层 Visual（Window 桌面 / View Android）。
/// 在 App.axaml.cs 中初始化。
/// </summary>
public static class MainWindowHelper
{
    public static Window? Instance { get; set; }

    /// <summary>Android 平台的主视图（UserControl），用于 TopLevel.GetTopLevel</summary>
    public static UserControl? MainViewInstance { get; set; }
}
