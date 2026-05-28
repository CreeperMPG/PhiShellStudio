using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentIcons.Common;
using PhigrosArchive;
using PhigrosArchive.Abstractions;
using PhigrosArchive.Save;
using PhigrosArchive.Save.Data;
using PhigrosShellGUI.Services;

namespace PhigrosShellGUI.ViewModels;

/// <summary>存档详情页的标签页项</summary>
public partial class DetailTabItem : ViewModelBase
{
    [ObservableProperty]
    private string _header = string.Empty;

    [ObservableProperty]
    private Icon _icon = Icon.InfoSparkle;

    [ObservableProperty]
    private ViewModelBase _content = null!;
}

/// <summary>存档详情页 ViewModel（带选项卡）</summary>
public partial class SlotDetailViewModel : ViewModelBase
{
    /// <summary>返回按钮被点击时触发</summary>
    public event Action? GoBackRequested;

    /// <summary>内部 ViewModel 请求全局 Data 编辑 Overlay</summary>
    public event EventHandler<DataEditStartingEventArgs>? DataEditStarting;

    /// <summary>内部 ViewModel 请求全局歌曲成绩编辑 Overlay</summary>
    public event EventHandler<RecordEditStartingEventArgs>? SongRecordEditStarting;

    /// <summary>请求最新的 IDifficultyProvider（在 Settings 中导入/更改 TSV 后使用）</summary>
    public event Func<IDifficultyProvider?>? RefreshRequested;

    // ── 私有字段 ──
    private PhigrosPlayerInfo? _currentPlayerInfo;
    private SaveFileInfo? _saveFileInfo;
    private SaveFile? _saveFile;
    private IDifficultyProvider? _difficultyProvider;
    private int _slotIndex;

    // ── 可绑定属性 ──

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private ObservableCollection<DetailTabItem> _tabs = new();

    // 存档检查
    [ObservableProperty] private int _issueCount;
    [ObservableProperty] private string _issueSummary = "检查存档";
    [ObservableProperty] private bool _isCheckingIssues;
    [ObservableProperty] private bool _isIssueOverlayVisible;
    [ObservableProperty] private ObservableCollection<SaveDataIssue> _issues = new();

    // 导出
    [ObservableProperty] private string _exportStatus = "导出";
    [ObservableProperty] private bool _isExporting;

    // 上传
    [ObservableProperty] private bool _isUploading;
    [ObservableProperty] private string _uploadStatus = "上传";

    // 刷新
    [ObservableProperty] private bool _isRefreshing;

    // 错误 Overlay
    [ObservableProperty] private bool _isErrorOverlayVisible;
    [ObservableProperty] private string _errorTitle = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;

    // 确认 Overlay
    [ObservableProperty] private bool _isConfirmVisible;
    [ObservableProperty] private string _confirmTitle = string.Empty;
    [ObservableProperty] private string _confirmMessage = string.Empty;

    // 登录感知
    [ObservableProperty] private bool _isUploadEnabled = true;

    /// <summary>是否可以点击上传按钮（组合了忙碌状态和登录/云存档状态）</summary>
    public bool CanUploadNow => !IsUploading && IsUploadEnabled;

    partial void OnIsUploadingChanged(bool value) => OnPropertyChanged(nameof(CanUploadNow));
    partial void OnIsUploadEnabledChanged(bool value) => OnPropertyChanged(nameof(CanUploadNow));

    private bool _disposed;

    // ── 初始化 ──

    /// <summary>初始化详情页</summary>
    public void Initialize(SaveFile saveFile, int slotIndex,
        IDifficultyProvider? difficultyProvider = null,
        PhigrosPlayerInfo? playerInfo = null,
        SaveFileInfo? saveFileInfo = null)
    {
        Title = $"Slot #{slotIndex} - 存档详情";
        _saveFile = saveFile;
        _currentPlayerInfo = playerInfo;
        _saveFileInfo = saveFileInfo;
        _difficultyProvider = difficultyProvider;
        _slotIndex = slotIndex;

        // 本地存档无法上传（无 CloudInfo），云存档需登录才能上传
        UpdateUploadEnable();
        LoginStateProvider.LoginStateChanged += OnLoginStateChanged;
        _disposed = false;

        RebuildTabs();
    }

    private void UpdateUploadEnable()
    {
        // 上传条件：已登录 +（有 CloudInfo 则云存档 / 没有 CloudInfo 则覆盖上传到 Slot #0）
        IsUploadEnabled = LoginStateProvider.IsLoggedIn;
    }

    private void OnLoginStateChanged()
    {
        UpdateUploadEnable();
    }

    /// <summary>释放时取消 LoginState 订阅</summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            LoginStateProvider.LoginStateChanged -= OnLoginStateChanged;
            _disposed = true;
        }
    }

    /// <summary>重建所有标签页（初始化时和刷新时共用）</summary>
    private void RebuildTabs()
    {
        Tabs.Clear();

        if (_saveFile == null) return;

        // 信息概览
        var overviewVm = InfoOverviewViewModel.FromSaveFile(_saveFile);
        overviewVm.DataEditStarting += (s, e) => DataEditStarting?.Invoke(s, e);
        Tabs.Add(new DetailTabItem
        {
            Header = "信息概览",
            Icon = Icon.InfoSparkle,
            Content = overviewVm
        });

        // P3B27
        if (_saveFile.GameRecord != null)
        {
            var p3b27Vm = new P3B27ViewModel(_saveFile.GameRecord, _difficultyProvider);
            Tabs.Add(new DetailTabItem
            {
                Header = "P3B27",
                Icon = Icon.DataBarVertical,
                Content = p3b27Vm
            });

            // 异步加载时自动检查存档
            _ = CheckIssuesAsync();
        }

        // 歌曲成绩（异步加载）
        if (_saveFile.GameRecord != null)
        {
            var placeholder = new PlaceholderViewModel("正在加载歌曲数据...");
            Tabs.Add(new DetailTabItem
            {
                Header = "歌曲成绩",
                Icon = Icon.Notebook,
                Content = placeholder
            });

            var recordsTabIndex = Tabs.Count - 1;
            _ = LoadSongRecordsAsync(_saveFile.GameRecord, recordsTabIndex);
        }
    }

    // ── 刷新 ──

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (_saveFile == null) return;

        IsRefreshing = true;

        try
        {
            // 从 MainWindowViewModel 获取最新的 difficulty provider（例如 TSV 导入后）
            if (RefreshRequested != null)
                _difficultyProvider = RefreshRequested();

            await Task.Run(() =>
            {
                // 导出当前存档到内存 → 用最新 provider 重建
                var zipBytes = _saveFile.PackToZip();
                _saveFile = new SaveFile(zipBytes, _difficultyProvider);
            });

            // 重建所有标签页（新 SaveFile + 新 provider）
            RebuildTabs();
        }
        catch (Exception ex)
        {
            ShowError("刷新失败", ex.Message);
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    // ── 存档检查 ──

    private async Task CheckIssuesAsync()
    {
        IsCheckingIssues = true;
        IssueSummary = "检查中...";

        try
        {
            var issues = await Task.Run(() =>
            {
                var summary = _saveFileInfo?.Summary;
                return _saveFile?.CheckSaveData(summary) ?? new();
            });

            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                Issues = new ObservableCollection<SaveDataIssue>(issues);
                IssueCount = issues.Count;
                IssueSummary = IssueCount > 0
                    ? $"⚠️ {IssueCount} 个问题"
                    : "✅ 无异常";
            });
        }
        catch (Exception ex)
        {
            IssueSummary = $"检查失败: {ex.Message}";
        }
        finally
        {
            IsCheckingIssues = false;
        }
    }

    [RelayCommand]
    private void OpenIssues()
    {
        IsIssueOverlayVisible = true;
    }

    [RelayCommand]
    private void CloseIssues()
    {
        IsIssueOverlayVisible = false;
    }

    // ── 导出 ──

    [RelayCommand]
    private async Task ExportSaveAsync()
    {
        if (_saveFile == null) return;

        IsExporting = true;
        ExportStatus = "导出中...";

        try
        {
            var topLevel = MainWindowHelper.Instance != null
                ? TopLevel.GetTopLevel(MainWindowHelper.Instance)
                : TopLevel.GetTopLevel(MainWindowHelper.MainViewInstance);
            if (topLevel == null) return;

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "导出存档",
                DefaultExtension = ".save",
                SuggestedFileName = $"{_saveFileInfo?.CloudInfo?.FileObjectID ?? "save"}.save",
                FileTypeChoices =
                [
                    new FilePickerFileType("Phigros 存档") { Patterns = ["*.save", "*.zip"] },
                ],
            });

            if (file == null)
            {
                ExportStatus = "导出";
                return;
            }

            var zipData = await Task.Run(() => _saveFile.PackToZip());
            await using var stream = await file.OpenWriteAsync();
            await stream.WriteAsync(zipData);

            ExportStatus = "✅ 导出成功";
        }
        catch (Exception ex)
        {
            ExportStatus = "❌ 导出失败";
            ShowError("导出失败", ex.Message);
        }
        finally
        {
            IsExporting = false;
        }
    }

    // ── 确认 Overlay ──

    [RelayCommand]
    private void ShowUploadConfirm()
    {
        if (_saveFile == null) return;
        if (!LoginStateProvider.IsLoggedIn)
        {
            ShowError("无法上传", "请先在右上角登录后再上传。");
            return;
        }

        ConfirmTitle = "确认上传";
        ConfirmMessage = _saveFileInfo?.CloudInfo != null
            ? "将把当前存档上传到 Phigros 云端，确认继续？"
            : "当前为本地文件，将上传并覆盖云端 Slot #0，确认继续？";
        IsConfirmVisible = true;
    }

    [RelayCommand]
    private async Task ConfirmActionAsync()
    {
        IsConfirmVisible = false;
        await UploadCoreAsync();
    }

    [RelayCommand]
    private void CancelConfirm()
    {
        IsConfirmVisible = false;
    }

    // ── 上传核心 ──

    private async Task UploadCoreAsync()
    {
        if (_saveFile == null) return;

        var playerInfo = LoginStateProvider.CurrentPlayerInfo;
        if (playerInfo == null)
        {
            ShowError("上传失败", "缺少登录信息，请重新登录后再试。");
            return;
        }

        IsUploading = true;
        UploadStatus = "上传中...";

        try
        {
            // 如果 _saveFileInfo 为 null（本地文件），获取云端 Slot #0 的 SaveFileInfo
            SaveFileInfo? targetInfo = _saveFileInfo;
            if (targetInfo == null)
            {
                var allInfos = await playerInfo.FetchSaveInfoAsync();
                targetInfo = allInfos.Length > 0 ? allInfos[0] : null;
            }

            // 同步摘要（确保服务器上的摘要数据和实际存档一致）
            if (targetInfo != null)
            {
                targetInfo.SyncSaveToSummary(_saveFile);
                System.Diagnostics.Debug.WriteLine("[Upload] Summary synced");
            }

            var zipData = await Task.Run(() => _saveFile.PackToZip());
            System.Diagnostics.Debug.WriteLine($"[Upload] Packed {zipData.Length} bytes");

            var newCloudInfo = await playerInfo.UploadSaveAsync(zipData, targetInfo, null, false);
            System.Diagnostics.Debug.WriteLine($"[Upload] Success, new FileObjectID: {newCloudInfo.FileObjectID}");

            if (_saveFileInfo != null)
                _saveFileInfo.CloudInfo = newCloudInfo;

            UploadStatus = "✅ 上传成功";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Upload] Failed: {ex}");
            UploadStatus = "❌ 上传失败";
            ShowError("上传失败", ex.Message);
        }
        finally
        {
            IsUploading = false;
        }
    }

    // ── 错误 Overlay ──

    private void ShowError(string title, string message)
    {
        ErrorTitle = title;
        ErrorMessage = message;
        IsErrorOverlayVisible = true;
    }

    [RelayCommand]
    private void CloseError()
    {
        IsErrorOverlayVisible = false;
    }

    // ── 返回 ──

    [RelayCommand]
    private void GoBack()
    {
        GoBackRequested?.Invoke();
    }

    // ── 异步加载歌曲成绩 ──

    private async Task LoadSongRecordsAsync(PhigrosRecord record, int tabIndex)
    {
        try
        {
            var recordsVm = await SongRecordsViewModel.FromGameRecordAsync(record);
            recordsVm.RecordEditStarting += (s, e) => SongRecordEditStarting?.Invoke(s, e);

            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (Tabs.Count > tabIndex)
                    Tabs[tabIndex].Content = recordsVm;
            });
        }
        catch (Exception ex)
        {
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (Tabs.Count > tabIndex)
                    Tabs[tabIndex].Content = new PlaceholderViewModel($"加载失败: {ex.Message}");
            });
        }
    }
}

/// <summary>占位 ViewModel（开发中提示）</summary>
public partial class PlaceholderViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _message;

    public PlaceholderViewModel(string message)
    {
        Message = message;
    }
}
