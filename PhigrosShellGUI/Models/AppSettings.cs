using System.Text.Json.Serialization;

namespace PhigrosShellGUI.Models;

/// <summary>应用设置数据模型（JSON 反序列化用）</summary>
public sealed class AppSettings
{
    /// <summary>主题变体：Light / Dark / Default(System)</summary>
    public string Theme { get; set; } = "Default";

    /// <summary>语言代码（预留，将来支持多语言）</summary>
    public string Language { get; set; } = "zh-CN";

    /// <summary>云存档只有一个 slot 时自动直接进入（默认开启）</summary>
    public bool AutoEnterSingleSlot { get; set; } = true;

    /// <summary>记住的 SessionToken</summary>
    public string? SavedSessionToken { get; set; }

    /// <summary>上次登录的用户名</summary>
    public string? LastLoginNickname { get; set; }
}
