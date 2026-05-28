using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhigrosArchive.Save;
using PhigrosArchive.Save.Data;

namespace PhigrosShellGUI.ViewModels;

/// <summary>信息概览：合并用户信息 + 游戏进度 + 游戏设置于一页</summary>
public partial class InfoOverviewViewModel : ViewModelBase
{
    /// <summary>请求打开全局 Data 编辑 Overlay（由 MainWindow 处理）</summary>
    public event EventHandler<DataEditStartingEventArgs>? DataEditStarting;

    // ── 原始数据对象引用（确认修改时写回） ──
    private PhigrosUser? _user;
    private PhigrosProgress? _progress;
    private PhigrosSettings? _settings;

    // ════════════════════════════════════════
    //  内联编辑状态
    // ════════════════════════════════════════

    // --- 用户 ---
    [ObservableProperty] private bool _isSelfIntroValid = true;
    [ObservableProperty] private bool _isAvatarValid = true;
    [ObservableProperty] private bool _isBackgroundValid = true;

    // --- 进度 ---
    [ObservableProperty] private bool _isCompletedValid = true;
    [ObservableProperty] private bool _isSongUpdateInfoValid = true;
    [ObservableProperty] private bool _isChallengeModeRankValid = true;
    [ObservableProperty] private bool _isUnlockSpasmodicValid = true;
    [ObservableProperty] private bool _isUnlockIgalltaValid = true;
    [ObservableProperty] private bool _isUnlockRrharilValid = true;
    [ObservableProperty] private bool _isRandomVersionUnlockedValid = true;
    [ObservableProperty] private bool _isChapter8SongUnlockedValid = true;

    // --- 设置 ---
    [ObservableProperty] private bool _isDeviceNameValid = true;
    [ObservableProperty] private bool _isBrightValid = true;
    [ObservableProperty] private bool _isMusicVolumeValid = true;
    [ObservableProperty] private bool _isEffectVolumeValid = true;
    [ObservableProperty] private bool _isHitSoundVolumeValid = true;
    [ObservableProperty] private bool _isSoundOffsetValid = true;
    [ObservableProperty] private bool _isNoteScaleValid = true;

    // ════════════════════════════════════════
    //  数据属性
    // ════════════════════════════════════════

    // ── PhigrosUser ──
    [ObservableProperty] private bool _showPlayerId;
    [ObservableProperty] private string _selfIntro = string.Empty;
    [ObservableProperty] private string _avatar = string.Empty;
    [ObservableProperty] private string _background = string.Empty;
    [ObservableProperty] private int _userOverflow;

    // ── PhigrosProgress ──
    [ObservableProperty] private bool _isFirstRun;
    [ObservableProperty] private bool _legacyChapterFinished;
    [ObservableProperty] private string _completed = string.Empty;
    [ObservableProperty] private int _songUpdateInfo;
    [ObservableProperty] private short _challengeModeRank;
    [ObservableProperty] private int _moneyKB;
    [ObservableProperty] private int _moneyMB;
    [ObservableProperty] private int _moneyGB;
    [ObservableProperty] private int _moneyTB;
    [ObservableProperty] private int _moneyPB;
    [ObservableProperty] private byte _unlockSpasmodic;
    [ObservableProperty] private byte _unlockIgallta;
    [ObservableProperty] private byte _unlockRrharil;
    [ObservableProperty] private byte _randomVersionUnlocked;
    [ObservableProperty] private byte _chapter8SongUnlocked;
    [ObservableProperty] private int _progressOverflow;

    // ── PhigrosSettings ──
    [ObservableProperty] private bool _chordSupport;
    [ObservableProperty] private bool _fcAPIndicator;
    [ObservableProperty] private bool _enableHitSound;
    [ObservableProperty] private bool _lowResolutionMode;
    [ObservableProperty] private string _deviceName = string.Empty;
    [ObservableProperty] private float _bright;
    [ObservableProperty] private float _musicVolume;
    [ObservableProperty] private float _effectVolume;
    [ObservableProperty] private float _hitSoundVolume;
    [ObservableProperty] private float _soundOffset;
    [ObservableProperty] private float _noteScale;
    [ObservableProperty] private int _settingsOverflow;

    public static InfoOverviewViewModel FromSaveFile(SaveFile saveFile)
    {
        var vm = new InfoOverviewViewModel
        {
            _user = saveFile.User,
            _progress = saveFile.GameProgress,
            _settings = saveFile.Settings,
        };

        if (saveFile.User != null)
        {
            vm.ShowPlayerId = saveFile.User.ShowPlayerId;
            vm.SelfIntro = saveFile.User.SelfIntro;
            vm.Avatar = saveFile.User.Avatar;
            vm.Background = saveFile.User.Background;
            vm.UserOverflow = saveFile.User.OverflowData.Length;
        }

        if (saveFile.GameProgress != null)
        {
            var p = saveFile.GameProgress;
            vm.IsFirstRun = p.IsFirstRun;
            vm.LegacyChapterFinished = p.LegacyChapterFinished;
            vm.Completed = p.Completed;
            vm.SongUpdateInfo = p.SongUpdateInfo;
            vm.ChallengeModeRank = p.ChallengeModeRank;
            vm.MoneyKB = p.Money.KB;
            vm.MoneyMB = p.Money.MB;
            vm.MoneyGB = p.Money.GB;
            vm.MoneyTB = p.Money.TB;
            vm.MoneyPB = p.Money.PB;
            vm.UnlockSpasmodic = p.UnlockFlagOfSpasmodic;
            vm.UnlockIgallta = p.UnlockFlagOfIgallta;
            vm.UnlockRrharil = p.UnlockFlagOfRrharil;
            vm.RandomVersionUnlocked = p.RandomVersionUnlocked;
            vm.Chapter8SongUnlocked = p.Chapter8SongUnlocked;
            vm.ProgressOverflow = p.OverflowData.Length;
        }

        if (saveFile.Settings != null)
        {
            var s = saveFile.Settings;
            vm.ChordSupport = s.ChordSupport;
            vm.FcAPIndicator = s.FcAPIndicator;
            vm.EnableHitSound = s.EnableHitSound;
            vm.LowResolutionMode = s.LowResolutionMode;
            vm.DeviceName = s.DeviceName;
            vm.Bright = s.Bright;
            vm.MusicVolume = s.MusicVolume;
            vm.EffectVolume = s.EffectVolume;
            vm.HitSoundVolume = s.HitSoundVolume;
            vm.SoundOffset = s.SoundOffset;
            vm.NoteScale = s.NoteScale;
            vm.SettingsOverflow = s.OverflowData.Length;
        }

        return vm;
    }

    // ════════════════════════════════════════
    //  Data 编辑（触发全局 Overlay）
    // ════════════════════════════════════════

    [RelayCommand]
    private void EditData()
    {
        var args = new DataEditStartingEventArgs
        {
            KB = MoneyKB,
            MB = MoneyMB,
            GB = MoneyGB,
            TB = MoneyTB,
            PB = MoneyPB,
            OnEditCompleted = result =>
            {
                if (result != null)
                {
                    MoneyKB = result.KB;
                    MoneyMB = result.MB;
                    MoneyGB = result.GB;
                    MoneyTB = result.TB;
                    MoneyPB = result.PB;

                    if (_progress != null)
                    {
                        _progress.Money.KB = result.KB;
                        _progress.Money.MB = result.MB;
                        _progress.Money.GB = result.GB;
                        _progress.Money.TB = result.TB;
                        _progress.Money.PB = result.PB;
                    }
                }
            }
        };
        DataEditStarting?.Invoke(this, args);
    }

    // ════════════════════════════════════════
    //  确认命令
    // ════════════════════════════════════════

    // ── 字符串字段 ──

    [RelayCommand]
    private void ConfirmSelfIntro(string? value)
        => ConfirmString(value, 500, v => { SelfIntro = v; if (_user != null) _user.SelfIntro = v; }, v => IsSelfIntroValid = v);

    [RelayCommand]
    private void ConfirmAvatar(string? value)
        => ConfirmString(value, 100, v => { Avatar = v; if (_user != null) _user.Avatar = v; }, v => IsAvatarValid = v);

    [RelayCommand]
    private void ConfirmBackground(string? value)
        => ConfirmString(value, 100, v => { Background = v; if (_user != null) _user.Background = v; }, v => IsBackgroundValid = v);

    [RelayCommand]
    private void ConfirmCompleted(string? value)
        => ConfirmString(value, 100, v => { Completed = v; if (_progress != null) _progress.Completed = v; }, v => IsCompletedValid = v);

    [RelayCommand]
    private void ConfirmDeviceName(string? value)
        => ConfirmString(value, 100, v => { DeviceName = v; if (_settings != null) _settings.DeviceName = v; }, v => IsDeviceNameValid = v);

    // ── 整数 int ──

    [RelayCommand]
    private void ConfirmSongUpdateInfo(string? value)
        => ConfirmInt(value, 0, int.MaxValue, v => { SongUpdateInfo = v; if (_progress != null) _progress.SongUpdateInfo = v; }, v => IsSongUpdateInfoValid = v);

    [RelayCommand]
    private void ConfirmChallengeModeRank(string? value)
        => ConfirmShort(value, 0, short.MaxValue, v => { ChallengeModeRank = v; if (_progress != null) _progress.ChallengeModeRank = v; }, v => IsChallengeModeRankValid = v);

    // ── 字节 byte ──

    [RelayCommand]
    private void ConfirmUnlockSpasmodic(string? value)
        => ConfirmByte(value, v => { UnlockSpasmodic = v; if (_progress != null) _progress.UnlockFlagOfSpasmodic = v; }, v => IsUnlockSpasmodicValid = v);

    [RelayCommand]
    private void ConfirmUnlockIgallta(string? value)
        => ConfirmByte(value, v => { UnlockIgallta = v; if (_progress != null) _progress.UnlockFlagOfIgallta = v; }, v => IsUnlockIgalltaValid = v);

    [RelayCommand]
    private void ConfirmUnlockRrharil(string? value)
        => ConfirmByte(value, v => { UnlockRrharil = v; if (_progress != null) _progress.UnlockFlagOfRrharil = v; }, v => IsUnlockRrharilValid = v);

    [RelayCommand]
    private void ConfirmRandomVersionUnlocked(string? value)
        => ConfirmByte(value, v => { RandomVersionUnlocked = v; if (_progress != null) _progress.RandomVersionUnlocked = v; }, v => IsRandomVersionUnlockedValid = v);

    [RelayCommand]
    private void ConfirmChapter8SongUnlocked(string? value)
        => ConfirmByte(value, v => { Chapter8SongUnlocked = v; if (_progress != null) _progress.Chapter8SongUnlocked = v; }, v => IsChapter8SongUnlockedValid = v);

    // ── 浮点数 float ──

    [RelayCommand]
    private void ConfirmBright(string? value)
        => ConfirmFloat(value, 0, 1, v => { Bright = v; if (_settings != null) _settings.Bright = v; }, v => IsBrightValid = v);

    [RelayCommand]
    private void ConfirmMusicVolume(string? value)
        => ConfirmFloat(value, 0, 1, v => { MusicVolume = v; if (_settings != null) _settings.MusicVolume = v; }, v => IsMusicVolumeValid = v);

    [RelayCommand]
    private void ConfirmEffectVolume(string? value)
        => ConfirmFloat(value, 0, 1, v => { EffectVolume = v; if (_settings != null) _settings.EffectVolume = v; }, v => IsEffectVolumeValid = v);

    [RelayCommand]
    private void ConfirmHitSoundVolume(string? value)
        => ConfirmFloat(value, 0, 1, v => { HitSoundVolume = v; if (_settings != null) _settings.HitSoundVolume = v; }, v => IsHitSoundVolumeValid = v);

    [RelayCommand]
    private void ConfirmSoundOffset(string? value)
        => ConfirmFloat(value, -400, 600, v => { SoundOffset = v; if (_settings != null) _settings.SoundOffset = v; }, v => IsSoundOffsetValid = v);

    [RelayCommand]
    private void ConfirmNoteScale(string? value)
        => ConfirmFloat(value, 0, 1, v => { NoteScale = v; if (_settings != null) _settings.NoteScale = v; }, v => IsNoteScaleValid = v);

    // ════════════════════════════════════════
    //  验证辅助方法
    // ════════════════════════════════════════

    private static void ConfirmString(string? input, int maxLen,
        Action<string> onSuccess, Action<bool> setValid)
    {
        var val = input ?? string.Empty;
        if (val.Length > maxLen)
        {
            setValid(false);
            return;
        }
        setValid(true);
        onSuccess(val);
    }

    private static void ConfirmInt(string? input, int min, int max,
        Action<int> onSuccess, Action<bool> setValid)
    {
        if (!int.TryParse(input, out var val) || val < min || val > max)
        {
            setValid(false);
            return;
        }
        setValid(true);
        onSuccess(val);
    }

    private static void ConfirmShort(string? input, short min, short max,
        Action<short> onSuccess, Action<bool> setValid)
    {
        if (!short.TryParse(input, out var val) || val < min || val > max)
        {
            setValid(false);
            return;
        }
        setValid(true);
        onSuccess(val);
    }

    private static void ConfirmByte(string? input,
        Action<byte> onSuccess, Action<bool> setValid)
    {
        if (!byte.TryParse(input, out var val))
        {
            setValid(false);
            return;
        }
        setValid(true);
        onSuccess(val);
    }

    private static void ConfirmFloat(string? input, float min, float max,
        Action<float> onSuccess, Action<bool> setValid)
    {
        if (!float.TryParse(input, out var val) || val < min || val > max)
        {
            setValid(false);
            return;
        }
        setValid(true);
        onSuccess(val);
    }
}
