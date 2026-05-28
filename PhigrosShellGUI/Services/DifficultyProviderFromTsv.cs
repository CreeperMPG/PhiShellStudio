using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using PhigrosArchive.Abstractions;
using PhiDifficultyProvider = PhigrosArchive.Save.Data.PhiDifficultyInfo<float?>;
using PhiLevelRecord = PhigrosArchive.Save.Data.PhiLevelRecord;

namespace PhigrosShellGUI.Services;

/// <summary>
/// 从 Difficulty.TSV 文件中读取定数并实现 IDifficultyProvider。
/// TSV 格式：每行 Tab 分隔，第一列是歌曲 ID，后续列依次为 EZ/HD/IN/AT/Legacy 的定数。
/// </summary>
public sealed class DifficultyProviderFromTsv : IDifficultyProvider
{
    private readonly Dictionary<string, PhiDifficultyProvider> _difficulties = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>是否已成功从文件加载</summary>
    public bool IsLoaded { get; private set; }

    /// <summary>文件路径（为空或不存在时 IsLoaded = false）</summary>
    public string? FilePath { get; }

    public DifficultyProviderFromTsv(string? filePath)
    {
        FilePath = filePath;
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            return;

        try
        {
            var lines = File.ReadAllLines(filePath);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var parts = line.Split('\t').Select(v => v.Trim()).ToArray();
                if (parts.Length < 2) continue;

                // 歌曲 ID → 例如 "songName" → "songName.0"
                var songId = parts[0] + ".0";

                var diffs = parts.Skip(1)
                    .Select(s => float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var f) ? f : (float?)null)
                    .ToArray();

                var entry = new PhiDifficultyProvider(
                    diffs.ElementAtOrDefault(0) ?? null,
                    diffs.ElementAtOrDefault(1) ?? null,
                    diffs.ElementAtOrDefault(2) ?? null,
                    diffs.ElementAtOrDefault(3) ?? null,
                    diffs.ElementAtOrDefault(4) ?? null
                );

                _difficulties[songId] = entry;
            }

            IsLoaded = _difficulties.Count > 0;
        }
        catch
        {
            IsLoaded = false;
        }
    }

    /// <summary>获取指定歌曲在指定难度索引下的定数</summary>
    public float? GetDifficulty(string songId, int difficultyIndex)
    {
        if (!IsLoaded || !_difficulties.TryGetValue(songId, out var entry))
            return null;

        return entry.GetByIndex(difficultyIndex);
    }

    /// <summary>已加载的歌曲数量</summary>
    public int LoadedSongCount => _difficulties.Count;
}
