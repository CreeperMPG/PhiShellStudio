using System;
using System.IO;
using System.Text.Json;
using PhigrosShellGUI.Models;

namespace PhigrosShellGUI.Services;

/// <summary>
/// 应用设置持久化服务。
/// 存储位置（跨平台）：
///   Windows → %LOCALAPPDATA%\PhigrosShellGUI\settings.json
///   Linux   → ~/.local/share/PhigrosShellGUI/settings.json
///   macOS   → ~/Library/Application Support/PhigrosShellGUI/settings.json
/// </summary>
public sealed class SettingsService
{
    private readonly string _filePath;

    public SettingsService()
    {
        var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appDir = Path.Combine(baseDir, "PhigrosShellGUI");
        Directory.CreateDirectory(appDir);
        _filePath = Path.Combine(appDir, "settings.json");
    }

    /// <summary>加载设置，文件不存在时返回默认值</summary>
    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_filePath))
                return new AppSettings();

            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    /// <summary>保存设置到磁盘</summary>
    public void Save(AppSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_filePath, json);
        }
        catch (Exception ex)
        {
            // 写文件失败不崩溃，仅日志
            System.Diagnostics.Debug.WriteLine($"[SettingsService] Save failed: {ex.Message}");
        }
    }
}
