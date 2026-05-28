using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhigrosArchive;
using PhigrosArchive.Abstractions;
using PhigrosArchive.Save;
using PhigrosArchive.Save.Data;
using PhigrosShellGUI.Services;
using PhigrosShellGUI.Views;

namespace PhigrosShellGUI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly SettingsService _settingsService = new();

    // 当前 DifficultyProvider（在应用启动或设置保存后重建）
    private IDifficultyProvider? _currentDifficultyProvider;

    // 导航状态
    private MainViewModel? _mainVm;
    private HomeViewModel? _homeVm;

    [ObservableProperty]
    private ViewModelBase _currentView = null!;

    // ════════════════════════════════════════
    //  登录 Overlay
    // ════════════════════════════════════════

    /// <summary>是否显示登录 Overlay</summary>
    [ObservableProperty] private bool _isLoginOverlayVisible;

    /// <summary>登录覆盖层的 ViewModel</summary>
    [ObservableProperty] private LoginViewModel? _loginVM;

    /// <summary>登录按钮文本</summary>
    [ObservableProperty] private string _loginButtonText = "登录";

    /// <summary>记住的 SessionToken（登录成功后保存）</summary>
    private string? _savedSessionToken;

    /// <summary>上次登录的账号名称（用于显示可点击的快速登录提示）</summary>
    [ObservableProperty] private string? _lastLoginHint;

    /// <summary>是否有保存的登录信息（控制快速登录提示的可见性）</summary>
    public bool HasLastLoginHint => !string.IsNullOrEmpty(LastLoginHint);

    partial void OnLastLoginHintChanged(string? value) => OnPropertyChanged(nameof(HasLastLoginHint));

    /// <summary>登录成功后是否需要重新触发云存档打开</summary>
    private bool _pendingCloudOpen;

    /// <summary>打开登录 Overlay（从全局顶栏或 NeedLogin 事件）</summary>
    [RelayCommand]
    private void OpenLoginOverlay()
    {
        if (LoginStateProvider.IsLoggedIn) return; // 已登录，不做任何事

        var vm = new LoginViewModel();
        vm.LoginSucceeded += OnLoginFromOverlay;
        LoginVM = vm;
        IsLoginOverlayVisible = true;
    }

    /// <summary>使用保存的 token 自动登录</summary>
    [RelayCommand]
    private async Task AutoLoginWithSavedTokenAsync()
    {
        if (string.IsNullOrEmpty(_savedSessionToken)) return;

        try
        {
            var playerInfo = await PhigrosPlayerInfo.FetchAsync(_savedSessionToken);
            if (playerInfo != null)
            {
                LoginStateProvider.Login(playerInfo);
                LoginButtonText = playerInfo.Nickname;
                IsLoginOverlayVisible = false;
                LoginVM = null;
            }
        }
        catch { }
    }

    [RelayCommand]
    private void CloseLoginOverlay()
    {
        IsLoginOverlayVisible = false;
        LoginVM = null;
        _pendingCloudOpen = false;
    }

    /// <summary>登录 Overlay 中的登录成功</summary>
    private void OnLoginFromOverlay(PhigrosPlayerInfo info)
    {
        LoginStateProvider.Login(info);
        LoginButtonText = info.Nickname;

        // 如果 login VM 有勾选记住，保存 token
        if (LoginVM?.RememberLogin == true && !string.IsNullOrWhiteSpace(LoginVM?.Token))
        {
            var settings = _settingsService.Load();
            settings.SavedSessionToken = LoginVM.Token;
            settings.LastLoginNickname = info.Nickname;
            _settingsService.Save(settings);
            _savedSessionToken = LoginVM.Token;
        }

        IsLoginOverlayVisible = false;
        LoginVM = null;

        // 如果是从云存档按钮触发的登录，自动重试
        if (_pendingCloudOpen)
        {
            _pendingCloudOpen = false;
            _homeVm?.OpenFromCloudCommand.ExecuteAsync(null);
        }
    }

    // ════════════════════════════════════════
    //  全局 Data 编辑 Overlay
    // ════════════════════════════════════════

    [ObservableProperty] private bool _isDataEditOverlayVisible;
    [ObservableProperty] private DataEditViewModel? _dataEditVM;

    [RelayCommand]
    private void CloseDataEditOverlay()
    {
        IsDataEditOverlayVisible = false;
        DataEditVM = null;
    }

    // ════════════════════════════════════════
    //  全局歌曲成绩编辑 Overlay
    // ════════════════════════════════════════

    [ObservableProperty] private bool _isRecordEditOverlayVisible;
    [ObservableProperty] private RecordEditViewModel? _recordEditVM;

    [RelayCommand]
    private void CloseRecordEditOverlay()
    {
        IsRecordEditOverlayVisible = false;
        RecordEditVM = null;
    }

    // ════════════════════════════════════════
    //  关于 Overlay
    // ════════════════════════════════════════

    [ObservableProperty] private bool _isAboutOverlayVisible;
    public string ChangelogText { get; } = ChangelogService.GetChangelog();
    public string AppVersion { get; } = "V1.0.0";

    [RelayCommand]
    private void OpenAbout() => IsAboutOverlayVisible = true;

    [RelayCommand]
    private void CloseAbout() => IsAboutOverlayVisible = false;

    // ════════════════════════════════════════
    //  设置 Overlay
    // ════════════════════════════════════════

    [ObservableProperty] private bool _isSettingsOverlayVisible;
    [ObservableProperty] private SettingsViewModel? _settingsVM;

    [RelayCommand]
    private void OpenSettings()
    {
        var vm = new SettingsViewModel(_settingsService);
        vm.CloseRequested += OnSettingsClosed;
        SettingsVM = vm;
        IsSettingsOverlayVisible = true;
    }

    private void OnSettingsClosed(bool saved)
    {
        IsSettingsOverlayVisible = false;
        SettingsVM = null;

        if (saved)
        {
            ApplyThemeFromSettings();
            RefreshDifficultyProvider();
        }
    }

    // ════════════════════════════════════════
    //  构造 + 初始化
    // ════════════════════════════════════════

    public MainWindowViewModel()
    {
        ApplyThemeFromSettings();
        RefreshDifficultyProvider();

        // 加载保存的登录信息
        var settings = _settingsService.Load();
        _savedSessionToken = settings.SavedSessionToken;
        if (!string.IsNullOrEmpty(settings.LastLoginNickname))
            LastLoginHint = $"登录上次账号：{settings.LastLoginNickname}";

        // 启动时显示首页
        ShowHome();
    }

    /// <summary>从持久化设置中读取并应用主题</summary>
    private void ApplyThemeFromSettings()
    {
        var s = _settingsService.Load();
        var app = Application.Current;
        if (app != null)
        {
            app.RequestedThemeVariant = s.Theme switch
            {
                "Light" => ThemeVariant.Light,
                "Dark" => ThemeVariant.Dark,
                _ => ThemeVariant.Default
            };
        }
    }

    /// <summary>从固定路径读取 difficulty.tsv，重建 DifficultyProvider</summary>
    private IDifficultyProvider? RefreshDifficultyProvider()
    {
        var tsvPath = TsvStorageService.GetTsvFilePath();
        if (TsvStorageService.TsvFileExists())
            _currentDifficultyProvider = new DifficultyProviderFromTsv(tsvPath);
        else
            _currentDifficultyProvider = null;
        return _currentDifficultyProvider;
    }

    // ════════════════════════════════════════
    //  导航
    // ════════════════════════════════════════

    /// <summary>显示首页</summary>
    private void ShowHome()
    {
        _mainVm = null; // 回到首页时释放 slot 列表
        _homeVm = new HomeViewModel();
        _homeVm.SaveFileLoaded += OnSaveFileLoaded;
        _homeVm.CloudSlotSelected += OnCloudSlotSelected;
        _homeVm.NeedLogin += OnNeedLoginForCloud;
        CurrentView = _homeVm;
    }

    /// <summary>从文件打开 或 单存档自动进入 → 直接进详情页</summary>
    private async void OnSaveFileLoaded(SaveFile saveFile, SaveFileInfo? saveFileInfo)
    {
        if (saveFileInfo == null)
        {
            // 本地文件
            CurrentView = CreateSlotDetailVm(saveFile, 0, null, null);
            return;
        }

        // 云存档（自动进入）：用最新 provider 重新解析，修复首次打开误判 TSV
        saveFile = new SaveFile(saveFile.PackToZip(), _currentDifficultyProvider);
        int slotIndex = await GetSlotIndexAsync(saveFileInfo);
        CurrentView = CreateSlotDetailVm(saveFile, slotIndex, saveFileInfo,
            LoginStateProvider.CurrentPlayerInfo);
    }

    /// <summary>选择 slot → 下载存档 → 切换到详情页</summary>
    private async void OnSlotSelected(SaveFileInfo saveInfo)
    {
        try
        {
            CurrentView = new PlaceholderViewModel("正在下载存档...");
            var saveFile = await saveInfo.FetchSaveAsync(_currentDifficultyProvider);
            int slotIndex = await GetSlotIndexAsync(saveInfo);
            var detailVm = CreateSlotDetailVm(saveFile, slotIndex, saveInfo, LoginStateProvider.CurrentPlayerInfo);
            CurrentView = detailVm;
        }
        catch (Exception)
        {
            await ShowCloudSlotListAsync();
        }
    }

    /// <summary>创建详情页 ViewModel 并挂载事件</summary>
    private SlotDetailViewModel CreateSlotDetailVm(SaveFile saveFile, int slotIndex,
        SaveFileInfo? saveFileInfo, PhigrosPlayerInfo? playerInfo)
    {
        var detailVm = new SlotDetailViewModel();
        detailVm.GoBackRequested += OnDetailGoBack;
        detailVm.DataEditStarting += OnDataEditStarting;
        detailVm.SongRecordEditStarting += OnSongRecordEditStarting;
        detailVm.RefreshRequested += () => RefreshDifficultyProvider();
        detailVm.Initialize(saveFile, slotIndex, _currentDifficultyProvider, playerInfo, saveFileInfo);
        return detailVm;
    }

    /// <summary>从详情页返回</summary>
    private void OnDetailGoBack()
    {
        if (_mainVm != null)
            CurrentView = _mainVm; // 从云存档过来的 → 回 slot 列表
        else
            ShowHome(); // 从本地文件过来的 → 回首页
    }

    /// <summary>处理内部 ViewModel 发起的 Data 编辑请求</summary>
    private void OnDataEditStarting(object? sender, DataEditStartingEventArgs e)
    {
        var vm = new DataEditViewModel(e.KB, e.MB, e.GB, e.TB, e.PB);
        vm.EditCompleted += (_, confirmed) =>
        {
            e.OnEditCompleted(confirmed ? vm.Result : null);
            IsDataEditOverlayVisible = false;
            DataEditVM = null;
        };
        DataEditVM = vm;
        IsDataEditOverlayVisible = true;
    }

    /// <summary>处理内部 ViewModel 发起的歌曲成绩编辑请求</summary>
    private void OnSongRecordEditStarting(object? sender, RecordEditStartingEventArgs e)
    {
        var vm = RecordEditViewModel.FromRecord(e.SongId, e.CurrentRecord);
        vm.EditCompleted += (_, confirmed) =>
        {
            e.OnEditCompleted(confirmed ? vm.Result : null);
            IsRecordEditOverlayVisible = false;
            RecordEditVM = null;
        };
        RecordEditVM = vm;
        IsRecordEditOverlayVisible = true;
    }

    // ── 云存档流程 ──

    /// <summary>从云存档打开（slot 列表）</summary>
    private async void OnCloudSlotSelected(SaveFileInfo[] saveInfos)
    {
        _mainVm = new MainViewModel();
        _mainVm.SlotSelected += OnSlotSelected;
        if (LoginStateProvider.CurrentPlayerInfo != null)
            await _mainVm.InitializeAsync(LoginStateProvider.CurrentPlayerInfo);
        CurrentView = _mainVm;
    }

    /// <summary>未登录时点击云存档 → 弹登录 Overlay，登录后自动重试</summary>
    private void OnNeedLoginForCloud()
    {
        _pendingCloudOpen = true;
        OpenLoginOverlay();
    }

    /// <summary>显示云存档列表（刷新/错误重试时用）</summary>
    private async Task ShowCloudSlotListAsync()
    {
        if (LoginStateProvider.CurrentPlayerInfo != null)
        {
            _mainVm = new MainViewModel();
            _mainVm.SlotSelected += OnSlotSelected;
            await _mainVm.InitializeAsync(LoginStateProvider.CurrentPlayerInfo);
            CurrentView = _mainVm;
        }
        else
        {
            ShowHome();
        }
    }

    /// <summary>获取某个 SaveFileInfo 对应的 slot 索引</summary>
    private async Task<int> GetSlotIndexAsync(SaveFileInfo target)
    {
        var playerInfo = LoginStateProvider.CurrentPlayerInfo;
        if (playerInfo == null) return 0;
        try
        {
            var allInfos = await playerInfo.FetchSaveInfoAsync();
            for (int i = 0; i < allInfos.Length; i++)
            {
                if (allInfos[i].CloudInfo?.FileUrl == target.CloudInfo?.FileUrl)
                    return i;
            }
        }
        catch { }
        return 0;
    }
}
