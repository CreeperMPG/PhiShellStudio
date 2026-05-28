using System;
using PhigrosArchive;

namespace PhigrosShellGUI.Services;

/// <summary>
/// 全局登录状态管理器。
/// SlotDetailView 等页面通过 <see cref="LoginStateChanged"/> 事件感知登录状态变化。
/// </summary>
public static class LoginStateProvider
{
    /// <summary>当前登录的玩家信息，null 表示未登录</summary>
    public static PhigrosPlayerInfo? CurrentPlayerInfo { get; private set; }

    /// <summary>登录/登出状态发生变化时触发</summary>
    public static event Action? LoginStateChanged;

    /// <summary>是否已登录</summary>
    public static bool IsLoggedIn => CurrentPlayerInfo != null;

    /// <summary>登录</summary>
    public static void Login(PhigrosPlayerInfo info)
    {
        CurrentPlayerInfo = info;
        LoginStateChanged?.Invoke();
    }

    /// <summary>登出</summary>
    public static void Logout()
    {
        CurrentPlayerInfo = null;
        LoginStateChanged?.Invoke();
    }
}
