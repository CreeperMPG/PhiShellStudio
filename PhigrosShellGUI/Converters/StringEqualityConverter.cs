using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace PhigrosShellGUI.Converters;

/// <summary>
/// 将字符串值与 ConverterParameter 比较的转换器。
/// 用于 RadioButton IsChecked 双向绑定：
///   IsChecked="{Binding SelectedTheme, Converter={StaticResource StringEqualityConverter}, ConverterParameter=Dark}"
/// </summary>
public sealed class StringEqualityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value?.ToString() == parameter?.ToString();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b && b)
            return parameter?.ToString();
        // 不改变绑定源
        return Avalonia.Data.BindingNotification.UnsetValue;
    }
}
