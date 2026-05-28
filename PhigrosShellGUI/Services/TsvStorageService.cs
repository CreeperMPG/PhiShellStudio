using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;

namespace PhigrosShellGUI.Services;

/// <summary>
/// 管理 TSV 定数文件的存储路径和导入。
/// 文件固定存放在 AppData/PhigrosShellGUI/difficulty.tsv。
/// </summary>
public static class TsvStorageService
{
    private static string GetDirectory()
    {
        var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(baseDir, "PhigrosShellGUI");
    }

    /// <summary>TSV 文件的目标路径（不保证文件存在）</summary>
    public static string GetTsvFilePath()
    {
        return Path.Combine(GetDirectory(), "difficulty.tsv");
    }

    /// <summary>TSV 文件是否存在</summary>
    public static bool TsvFileExists()
    {
        return File.Exists(GetTsvFilePath());
    }

    /// <summary>将 StorageFile 中的内容复制到目标 TSV 路径</summary>
    public static async Task<string?> ImportFromStorageFileAsync(IStorageFile sourceFile)
    {
        var destPath = GetTsvFilePath();
        var dir = Path.GetDirectoryName(destPath);
        if (dir != null) Directory.CreateDirectory(dir);

        try
        {
            await using var srcStream = await sourceFile.OpenReadAsync();
            await using var destStream = File.Create(destPath);
            await srcStream.CopyToAsync(destStream);
            return destPath;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TsvStorageService] Import failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>删除 TSV 文件</summary>
    public static void DeleteTsvFile()
    {
        var path = GetTsvFilePath();
        if (File.Exists(path))
            File.Delete(path);
    }
}
