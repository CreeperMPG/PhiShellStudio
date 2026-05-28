using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using System;

namespace PhigrosShellGUI.Controls;

/// <summary>
/// 等宽换行面板 —— 所有卡片在同一行等宽，放不下自动换行。
/// </summary>
public class UniformWrapPanel : Panel
{
    /// <summary>
    /// 卡片最小宽度，用来计算每行能放几列。
    /// </summary>
    public static readonly StyledProperty<double> MinItemWidthProperty =
        AvaloniaProperty.Register<UniformWrapPanel, double>(nameof(MinItemWidth), 300.0);

    public double MinItemWidth
    {
        get => GetValue(MinItemWidthProperty);
        set => SetValue(MinItemWidthProperty, value);
    }

    /// <summary>
    /// 卡片之间的水平间距。
    /// </summary>
    public static readonly StyledProperty<double> HorizontalSpacingProperty =
        AvaloniaProperty.Register<UniformWrapPanel, double>(nameof(HorizontalSpacing), 8.0);

    public double HorizontalSpacing
    {
        get => GetValue(HorizontalSpacingProperty);
        set => SetValue(HorizontalSpacingProperty, value);
    }

    /// <summary>
    /// 最大列数（0 表示不限制）。
    /// </summary>
    public static readonly StyledProperty<int> MaxColumnsProperty =
        AvaloniaProperty.Register<UniformWrapPanel, int>(nameof(MaxColumns), 0);

    public int MaxColumns
    {
        get => GetValue(MaxColumnsProperty);
        set => SetValue(MaxColumnsProperty, value);
    }

    /// <summary>
    /// 卡片之间的垂直间距。
    /// </summary>
    public static readonly StyledProperty<double> VerticalSpacingProperty =
        AvaloniaProperty.Register<UniformWrapPanel, double>(nameof(VerticalSpacing), 8.0);

    public double VerticalSpacing
    {
        get => GetValue(VerticalSpacingProperty);
        set => SetValue(VerticalSpacingProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Children.Count == 0)
            return new Size(0, 0);

        double availableWidth = availableSize.Width;

        // 宽度无限时（极少情况），回退到基础布局
        if (double.IsInfinity(availableWidth))
        {
            foreach (var child in Children)
                child.Measure(availableSize);
            return base.MeasureOverride(availableSize);
        }

        int itemsPerRow = CalculateItemsPerRow(availableWidth);
        double itemWidth = CalculateItemWidth(availableWidth, itemsPerRow);

        double totalHeight = 0;
        double rowMaxHeight = 0;
        int countInRow = 0;

        for (int i = 0; i < Children.Count; i++)
        {
            var child = Children[i];
            child.Measure(new Size(itemWidth, availableSize.Height));
            rowMaxHeight = Math.Max(rowMaxHeight, child.DesiredSize.Height);
            countInRow++;

            if (countInRow == itemsPerRow || i == Children.Count - 1)
            {
                totalHeight += rowMaxHeight;
                if (i < Children.Count - 1)
                    totalHeight += VerticalSpacing;
                rowMaxHeight = 0;
                countInRow = 0;
            }
        }

        return new Size(availableWidth, totalHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Children.Count == 0)
            return finalSize;

        double availableWidth = finalSize.Width;

        if (double.IsInfinity(availableWidth))
        {
            foreach (var child in Children)
                child.Arrange(new Rect(child.DesiredSize));
            return finalSize;
        }

        int itemsPerRow = CalculateItemsPerRow(availableWidth);
        double itemWidth = CalculateItemWidth(availableWidth, itemsPerRow);

        double x = 0;
        double y = 0;
        double rowMaxHeight = 0;
        int countInRow = 0;

        for (int i = 0; i < Children.Count; i++)
        {
            var child = Children[i];
            rowMaxHeight = Math.Max(rowMaxHeight, child.DesiredSize.Height);

            child.Arrange(new Rect(x, y, itemWidth, child.DesiredSize.Height));

            x += itemWidth + HorizontalSpacing;
            countInRow++;

            if (countInRow == itemsPerRow || i == Children.Count - 1)
            {
                y += rowMaxHeight + VerticalSpacing;
                x = 0;
                rowMaxHeight = 0;
                countInRow = 0;
            }
        }

        return finalSize;
    }

    private int CalculateItemsPerRow(double availableWidth)
    {
        int byWidth = Math.Max(1, (int)((availableWidth + HorizontalSpacing) / (MinItemWidth + HorizontalSpacing)));
        return MaxColumns > 0 ? Math.Min(byWidth, MaxColumns) : byWidth;
    }

    private double CalculateItemWidth(double availableWidth, int itemsPerRow)
    {
        return (availableWidth - (itemsPerRow - 1) * HorizontalSpacing) / itemsPerRow;
    }
}
