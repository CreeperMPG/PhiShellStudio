using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhigrosArchive;
using PhigrosShellGUI.Services;

namespace PhigrosShellGUI.ViewModels;

/// <summary>扫码登录状态的枚举</summary>
public enum QrLoginState
{
    Normal,        // 默认：Token 输入 + QR 按钮
    LoadingQR,     // 正在获取二维码
    ScanQR,        // 二维码已显示，等待扫码
    LoadingProfile,// 扫码成功，正在拉取用户信息
    Error,         // 出错
}

public partial class LoginViewModel : ViewModelBase
{
    /// <summary>登录成功后触发，携带登录后的玩家信息</summary>
    public event Action<PhigrosPlayerInfo>? LoginSucceeded;

    // ── Token 登录相关 ──

    [ObservableProperty]
    private string _token = string.Empty;

    [ObservableProperty]
    private bool _rememberLogin;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string? _statusMessage;

    // ── QR 扫码登录相关 ──

    [ObservableProperty]
    private QrLoginState _qrLoginState = QrLoginState.Normal;

    /// <summary>QR 码图片（绑定到 Image.Source）</summary>
    [ObservableProperty]
    private Bitmap? _qrCodeBitmap;

    /// <summary>QR 码 URL（可点开）</summary>
    [ObservableProperty]
    private string? _qrCodeUrl;

    /// <summary>二维码过期倒计时（秒）</summary>
    [ObservableProperty]
    private int _qrExpiresIn;

    /// <summary>当前扫码状态文字</summary>
    [ObservableProperty]
    private string? _qrStatusText;

    /// <summary>登录后的玩家信息（记住登录用）</summary>
    public PhigrosPlayerInfo? LastPlayerInfo { get; private set; }

    private CancellationTokenSource? _qrCts;
    private string? _deviceCode;
    private int _pollInterval;

    public bool IsQrActive => QrLoginState is QrLoginState.LoadingQR or QrLoginState.ScanQR or QrLoginState.LoadingProfile;

    partial void OnQrLoginStateChanged(QrLoginState value)
    {
        OnPropertyChanged(nameof(IsQrActive));
    }

    // ── Token 登录 ──

    [RelayCommand]
    private async Task LoginWithTokenAsync()
    {
        if (string.IsNullOrWhiteSpace(Token))
        {
            ErrorMessage = "请输入 SessionToken";
            return;
        }

        IsLoading = true;
        ErrorMessage = null;
        StatusMessage = "正在登录...";

        try
        {
            var playerInfo = await PhigrosPlayerInfo.FetchAsync(Token.Trim());

            if (playerInfo == null)
            {
                ErrorMessage = "登录失败：无法获取玩家信息";
                return;
            }

            LastPlayerInfo = playerInfo;
            StatusMessage = $"登录成功！欢迎 {playerInfo.Nickname}";
            LoginSucceeded?.Invoke(playerInfo);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"登录失败：{ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ── QR 扫码登录 ──

    [RelayCommand]
    private async Task LoginWithQRAsync()
    {
        _qrCts?.Cancel();
        _qrCts = new CancellationTokenSource();
        var ct = _qrCts.Token;

        QrLoginState = QrLoginState.LoadingQR;
        ErrorMessage = null;
        StatusMessage = null;
        QrStatusText = "正在准备二维码...";

        try
        {
            // 1. 获取二维码
            var qrResponse = await Taptap.GetLoginQRCode(china: true);
            if (qrResponse == null)
            {
                QrLoginState = QrLoginState.Error;
                ErrorMessage = "获取二维码失败";
                return;
            }

            _deviceCode = qrResponse.Value.device_code;
            _pollInterval = qrResponse.Value.interval;
            QrExpiresIn = qrResponse.Value.expires_in;
            QrCodeUrl = qrResponse.Value.qrcode_url;

            // 2. 生成 QR 码图片
            QrCodeBitmap = QrCodeHelper.GenerateQrBitmap(qrResponse.Value.qrcode_url, 256);

            // 3. 切换到扫码等待状态
            QrLoginState = QrLoginState.ScanQR;
            QrStatusText = "请用 TapTap 客户端扫描二维码";

            // 4. 轮询扫码结果
            await PollQrLoginAsync(ct);
        }
        catch (OperationCanceledException)
        {
            // 用户取消
            ResetToNormal();
        }
        catch (Exception ex)
        {
            QrLoginState = QrLoginState.Error;
            ErrorMessage = $"扫码登录失败：{ex.Message}";
        }
    }

    /// <summary>轮询扫码结果直到成功或超时</summary>
    private async Task PollQrLoginAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && QrExpiresIn > 0)
        {
            await Task.Delay(_pollInterval * 1000, ct);

            var pollResult = await Taptap.PollQRCode(_deviceCode!, china: true);

            var status = pollResult.Key;
            var qrResult = pollResult.Value;

            switch (status)
            {
                case QRCodeStatus.AuthorizationPending:
                    // 未扫码，继续等待
                    QrExpiresIn -= _pollInterval;
                    QrStatusText = $"请用 TapTap 客户端扫描二维码（{QrExpiresIn}秒后失效）";
                    continue;

                case QRCodeStatus.AuthorizationWaiting:
                    QrStatusText = "已扫码，请在手机上确认授权...";
                    continue;

                case QRCodeStatus.Success when qrResult != null:
                    // 扫码成功！
                    await OnQrLoginSuccessAsync(qrResult.Value, ct);
                    return;

                case QRCodeStatus.InvalidGrantCode:
                    QrLoginState = QrLoginState.Error;
                    ErrorMessage = "二维码已失效，请重新获取";
                    return;

                default:
                    QrLoginState = QrLoginState.Error;
                    ErrorMessage = "扫码登录失败";
                    return;
            }
        }

        // 超时
        if (QrExpiresIn <= 0)
        {
            QrLoginState = QrLoginState.Error;
            ErrorMessage = "二维码已过期，请重新获取";
        }
    }

    /// <summary>扫码成功后获取用户信息</summary>
    private async Task OnQrLoginSuccessAsync(QRCodeResult qrResult, CancellationToken ct)
    {
        QrLoginState = QrLoginState.LoadingProfile;
        QrStatusText = "扫码成功，正在获取用户信息...";

        // 4. 获取用户 profile
        var userProfile = Taptap.FetchUserProfile(qrResult);
        if (userProfile == null)
        {
            QrLoginState = QrLoginState.Error;
            ErrorMessage = "获取用户信息失败";
            return;
        }

        QrStatusText = "正在登录...";

        // 5. 用 TapTap 身份绑定 LeanCloud 用户
        var phiInfo = await Taptap.GetPhiPlayerInfoByTaptap(qrResult, userProfile.Value);
        var sessionToken = phiInfo.RootElement.GetProperty("sessionToken").GetString();

        if (string.IsNullOrEmpty(sessionToken))
        {
            QrLoginState = QrLoginState.Error;
            ErrorMessage = "获取 SessionToken 失败";
            return;
        }

        // 6. 构建玩家信息
        var playerInfo = PhigrosPlayerInfo.FromJson(phiInfo.RootElement, sessionToken);

        // 7. 拉取存档列表
        try
        {
            await playerInfo.FetchSaveInfoAsync();
        }
        catch
        {
            // 存档信息非必需，静默处理
        }

        LastPlayerInfo = playerInfo;
        QrStatusText = $"登录成功！欢迎 {playerInfo.Nickname}";
        QrCodeBitmap = null;
        QrCodeUrl = null;

        // 通知上层
        LoginSucceeded?.Invoke(playerInfo);
    }

    /// <summary>取消 QR 登录</summary>
    [RelayCommand]
    private void CancelQrLogin()
    {
        _qrCts?.Cancel();
        ResetToNormal();
    }

    private void ResetToNormal()
    {
        QrLoginState = QrLoginState.Normal;
        QrCodeBitmap?.Dispose();
        QrCodeBitmap = null;
        QrCodeUrl = null;
        QrStatusText = null;
        _deviceCode = null;
    }

    // ── 加载状态 ──

    [ObservableProperty]
    private bool _isLoading;

    /// <summary>Token 登录按钮点击</summary>
    [RelayCommand]
    private void SwitchToTokenLogin()
    {
        _qrCts?.Cancel();
        ResetToNormal();
        ErrorMessage = null;
        StatusMessage = null;
    }
}
