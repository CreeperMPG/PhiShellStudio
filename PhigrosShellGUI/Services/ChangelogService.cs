using System;
using System.IO;
using System.Reflection;

namespace PhigrosShellGUI.Services;

/// <summary>读取嵌入的更新日志文件</summary>
public static class ChangelogService
{
    private static string? _cached;

    /// <summary>获取更新日志文本（缓存）</summary>
    public static string GetChangelog()
    {
        if (_cached != null) return _cached;

        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var name = assembly.GetName().Name;
            var resourceName = $"{name}.CHANGELOG.txt";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null) return "（未找到更新日志文件）";

            using var reader = new StreamReader(stream);
            _cached = reader.ReadToEnd();
            return _cached;
        }
        catch (Exception ex)
        {
            return $"（读取更新日志失败: {ex.Message}）";
        }
    }
}
