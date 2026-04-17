using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace Promix.Financials.UI.Converters;

public sealed class BoolToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var isVisible = value switch
        {
            bool booleanValue => booleanValue,
            _ => false
        };

        if (Invert)
            isVisible = !isVisible;

        return isVisible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => value is Visibility visibility
            ? Invert ? visibility != Visibility.Visible : visibility == Visibility.Visible
            : false;
}
