using Avalonia.Controls;
using PhigrosShellGUI.ViewModels;

namespace PhigrosShellGUI.Views;

public partial class SongRecordsView : UserControl
{
    /// <summary>卡片最小宽度（含间距）</summary>
    private const double MinCardWidth = 360;

    public SongRecordsView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(System.EventArgs e)
    {
        base.OnDataContextChanged(e);
        RecalcColumns();
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        RecalcColumns();
    }

    private void RecalcColumns()
    {
        if (DataContext is not SongRecordsViewModel vm)
            return;

        double width = Bounds.Width;
        if (width <= 0) return;

        // 每一行有左右 padding 16px, 行内卡片之间有 gap
        int columns = (int)(width / MinCardWidth);
        if (columns < 1) columns = 1;

        vm.SetCardsPerRow(columns);
    }
}
