using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhigrosArchive.Save.Data;

namespace PhigrosShellGUI.ViewModels;

/// <summary>歌曲成绩标签页的 ViewModel（含搜索 + 自适应列数 + 虚拟化行）</summary>
public partial class SongRecordsViewModel : ViewModelBase
{
    /// <summary>请求打开歌曲编辑 Overlay</summary>
    public event EventHandler<RecordEditStartingEventArgs>? RecordEditStarting;

    /// <summary>当前每行的卡片数（自适应设置）</summary>
    private int _cardsPerRow = 2;

    private List<SongRecordViewModel> _allRecords = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditRecordCommand))]
    private ObservableCollection<SongRecordRowViewModel> _filteredRows = new();

    /// <summary>
    /// 设置每行卡片数并重新分组。
    /// 由外部（View 的 SizeChanged 处理）调用，实现响应式列数。
    /// </summary>
    public void SetCardsPerRow(int columns)
    {
        if (columns < 1) columns = 1;
        if (columns == _cardsPerRow) return;
        _cardsPerRow = columns;
        RecomputeFilteredRows();
    }

    /// <summary>异步从 GameRecord 创建（在后台线程构建卡片）</summary>
    public static Task<SongRecordsViewModel> FromGameRecordAsync(PhigrosRecord record)
    {
        return Task.Run(() =>
        {
            var vm = new SongRecordsViewModel();
            var cards = new List<SongRecordViewModel>(record.Records.Count);

            foreach (var (songId, diffInfo) in record.Records)
            {
                var card = SongRecordViewModel.FromRecord(songId, diffInfo);
                card.EditCommand = vm.EditRecordCommand;
                cards.Add(card);
            }

            vm._allRecords = cards;
            vm.RecomputeFilteredRows();
            return vm;
        });
    }

    /// <summary>将当前过滤后的卡片列表按 _cardsPerRow 分组为行</summary>
    private void RecomputeFilteredRows()
    {
        var source = string.IsNullOrWhiteSpace(SearchText)
            ? _allRecords
            : _allRecords
                .Where(r => r.SongId.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                .ToList();

        int cols = _cardsPerRow;
        var rows = new List<SongRecordRowViewModel>();

        for (int i = 0; i < source.Count; i += cols)
        {
            var row = new SongRecordRowViewModel { Columns = cols };
            int end = Math.Min(i + cols, source.Count);
            for (int j = i; j < end; j++)
                row.Cards.Add(source[j]);
            rows.Add(row);
        }

        FilteredRows = new ObservableCollection<SongRecordRowViewModel>(rows);
    }

    // ── 防抖搜索 ──
    private CancellationTokenSource? _filterCts;

    partial void OnSearchTextChanged(string value)
    {
        _filterCts?.Cancel();
        _filterCts = new CancellationTokenSource();
        var token = _filterCts.Token;

        Task.Run(async () =>
        {
            try
            {
                await Task.Delay(150, token);

                var source = string.IsNullOrWhiteSpace(value)
                    ? _allRecords
                    : _allRecords
                        .Where(r => r.SongId.Contains(value, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                int cols = _cardsPerRow;
                var rows = new List<SongRecordRowViewModel>();
                for (int i = 0; i < source.Count; i += cols)
                {
                    var row = new SongRecordRowViewModel { Columns = cols };
                    int end = Math.Min(i + cols, source.Count);
                    for (int j = i; j < end; j++)
                        row.Cards.Add(source[j]);
                    rows.Add(row);
                }

                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    if (!token.IsCancellationRequested)
                        FilteredRows = new ObservableCollection<SongRecordRowViewModel>(rows);
                });
            }
            catch (OperationCanceledException) { }
        }, token);
    }

    [RelayCommand(CanExecute = nameof(CanEditRecord))]
    private void EditRecord(SongRecordViewModel? record)
    {
        if (record?.RecordData == null) return;

        var args = new RecordEditStartingEventArgs
        {
            SongId = record.SongId,
            CurrentRecord = record.RecordData,
            OnEditCompleted = result =>
            {
                if (result != null)
                {
                    record.RecordData.EZ = result.EZ;
                    record.RecordData.HD = result.HD;
                    record.RecordData.IN = result.IN;
                    record.RecordData.AT = result.AT;
                    record.RecordData.Legacy = result.Legacy;
                    record.RefreshFromData();
                }
            }
        };
        RecordEditStarting?.Invoke(this, args);
    }

    private bool CanEditRecord(SongRecordViewModel? record)
        => record != null;
}
