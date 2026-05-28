using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PhigrosShellGUI.ViewModels;

/// <summary>歌曲卡片行（每行水平排列 N 张卡片，供虚拟化 ListBox 使用）</summary>
public partial class SongRecordRowViewModel : ViewModelBase
{
    /// <summary>此行中的卡片</summary>
    [ObservableProperty]
    private ObservableCollection<SongRecordViewModel> _cards = new();

    /// <summary>此行 UniformGrid 的列数（由父级 ViewModel 设置）</summary>
    [ObservableProperty]
    private int _columns = 3;
}
