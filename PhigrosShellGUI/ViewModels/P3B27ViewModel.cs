using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using PhigrosArchive.Abstractions;
using PhigrosArchive.Save.Data;

namespace PhigrosShellGUI.ViewModels;

/// <summary>P3B27 页面中单条曲目数据</summary>
public partial class P3B27Item : ViewModelBase
{
    [ObservableProperty] private int _rank;
    [ObservableProperty] private string _songName = string.Empty;
    [ObservableProperty] private string _difficultyKey = string.Empty;
    [ObservableProperty] private int _score;
    [ObservableProperty] private string _rankText = string.Empty;
    [ObservableProperty] private float _acc;
    [ObservableProperty] private float? _difficulty;
    [ObservableProperty] private float? _rankingScore;
    [ObservableProperty] private string? _expectedAcc; // B27 提升建议
    [ObservableProperty] private bool _isP3;
    [ObservableProperty] private string _rksDisplay = "?";
}

/// <summary>P3B27 页面 ViewModel</summary>
public partial class P3B27ViewModel : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<P3B27Item> _p3Items = new();
    [ObservableProperty] private ObservableCollection<P3B27Item> _b27Items = new();
    [ObservableProperty] private string _rankingScoreDisplay = "?";
    [ObservableProperty] private bool _hasTsv = true;
    [ObservableProperty] private string _tsvStatus = "?";
    [ObservableProperty] private bool _hasData;

    /// <summary>从 GameRecord 构建 P3B27 数据</summary>
    public P3B27ViewModel(PhigrosRecord? gameRecord, IDifficultyProvider? difficultyProvider)
    {
        HasTsv = difficultyProvider != null;
        TsvStatus = HasTsv ? "已导入定数文件" : "未导入定数文件";

        if (gameRecord == null || gameRecord.Records.Count == 0)
        {
            HasData = false;
            return;
        }

        HasData = true;
        float? rankingScore = gameRecord.RankingScore;
        RankingScoreDisplay = rankingScore?.ToString("F6") ?? "?";

        // 收集所有曲目记录
        var allEntries = gameRecord.Records
            .SelectMany(kvp =>
                kvp.Value.GetDictionary()
                    .Where(songkvp => songkvp.Value != null)
                    .Select(songkvp => new
                    {
                        SongId = kvp.Key,
                        DifficultyKey = songkvp.Key,
                        Record = songkvp.Value!
                    })
            )
            .ToList();

        // 使用 DifficultyProvider 的定数（如果有），否则使用游戏内定数
        var entriesWithDifficulty = allEntries.Select(e =>
        {
            var diff = GetDifficulty(e.SongId, e.DifficultyKey, difficultyProvider)
                       ?? e.Record.Difficulty;
            return new { e.SongId, e.DifficultyKey, e.Record, Difficulty = diff };
        }).ToList();

        // P3: 满分(1000000) + ACC 100 的曲目，按定数降序取前3
        var p3Raw = entriesWithDifficulty
            .Where(e => e.Record.Score == 1000000 && e.Record.Acc == 100)
            .OrderByDescending(e => e.Difficulty ?? 0)
            .Take(3)
            .ToList();

        for (int i = 0; i < p3Raw.Count; i++)
        {
            var e = p3Raw[i];
            P3Items.Add(new P3B27Item
            {
                Rank = i + 1,
                SongName = e.SongId,
                DifficultyKey = e.DifficultyKey,
                Score = e.Record.Score,
                RankText = e.Record.Rank.ToString(),
                Acc = e.Record.Acc,
                Difficulty = e.Difficulty,
                RankingScore = e.Record.RankingScore,
                RksDisplay = e.Record.RankingScore?.ToString("F3") ?? "?",
                IsP3 = true,
            });
        }

        // B27: 按 RankingScore 降序取前27
        float rksForCalc = rankingScore ?? 0.1f;
        double p3Difficulty = p3Raw.Count >= 3 ? p3Raw[2].Difficulty ?? 0 : 0;
        double b27Threshold = 0;

        var b27Raw = entriesWithDifficulty
            .Where(e => e.Record.RankingScore != null)
            .OrderByDescending(e => e.Record.RankingScore)
            .Take(27)
            .ToList();

        if (b27Raw.Count >= 27)
            b27Threshold = b27Raw[26].Record.RankingScore ?? 0;

        for (int i = 0; i < b27Raw.Count; i++)
        {
            var e = b27Raw[i];
            double acc = e.Record.Acc;
            float difficulty = e.Difficulty ?? 0.1f;

            // 计算期望 ACC（与 CLI 版本相同逻辑）
            double? expectedAcc = CalculateRKS(
                acc, difficulty, rksForCalc,
                p3Difficulty, b27Threshold);

            B27Items.Add(new P3B27Item
            {
                Rank = i + 1,
                SongName = e.SongId,
                DifficultyKey = e.DifficultyKey,
                Score = e.Record.Score,
                RankText = e.Record.Rank.ToString(),
                Acc = e.Record.Acc,
                Difficulty = e.Difficulty,
                RankingScore = e.Record.RankingScore,
                RksDisplay = e.Record.RankingScore?.ToString("F3") ?? "?",
                ExpectedAcc = expectedAcc != null ? $"{expectedAcc:F3}%" : null,
                IsP3 = false,
            });
        }
    }

    /// <summary>从 DifficultyProvider 获取定数</summary>
    private static float? GetDifficulty(string songId, string difficultyKey,
        IDifficultyProvider? provider)
    {
        if (provider == null) return null;
        int idx = difficultyKey switch
        {
            "EZ" => 0,
            "HD" => 1,
            "IN" => 2,
            "AT" => 3,
            "Legacy" => 4,
            _ => -1
        };
        return idx >= 0 ? provider.GetDifficulty(songId, idx) : null;
    }

    // ── RKS 计算（移植自 CLI 版本） ──

    private static double CalculateSingleRankingScore(double acc, double difficulty)
    {
        if (acc < 70) return 0;
        return Math.Pow((acc - 55) / 45, 2) * difficulty;
    }

    private static double CalculateNextRKS(double value)
    {
        if (value < 0) throw new ArgumentException("只支持非负数处理", nameof(value));
        double y = value * 200.0;
        double n = Math.Ceiling(y);
        long longN = (long)Math.Round(n, 0);
        if (longN % 2 == 0) longN += 1;
        else if (longN / 200.0 <= value + 1e-12) longN += 2;
        return longN / 200.0;
    }

    private static double InverseAcc(double singleRks, float difficulty)
        => 55.0 + 45.0 * Math.Sqrt(singleRks / difficulty);

    private static double? CalculateRKS(double acc, float difficulty, float rks,
        double p3Difficulty, double b27Rks)
    {
        double nextrks = CalculateNextRKS(rks);
        double rksIncrement = nextrks - rks;
        double fAcc = CalculateSingleRankingScore(acc, difficulty);
        double targetRks = Math.Max(fAcc, b27Rks) + 30 * rksIncrement;
        double x = InverseAcc(targetRks, difficulty);

        if (x > 100)
        {
            double singleIncrement = CalculateSingleRankingScore(x, difficulty)
                                     - CalculateSingleRankingScore(acc, difficulty);
            double phiIncrement = difficulty - p3Difficulty;
            if (phiIncrement <= 0) return null;
            double realRksIncrement = (singleIncrement + phiIncrement) / 30.0;
            return realRksIncrement >= rksIncrement ? 100 : null;
        }
        return x;
    }
}
