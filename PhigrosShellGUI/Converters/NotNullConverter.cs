using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace PhigrosShellGUI.Converters;

/// <summary>
/// 判空转换器。用在行 ViewModel 中判断卡片是否为 null 来控制可见性。
/// Convert(value) → value != null
/// </summary>
public sealed class NotNullConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value != null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
