using System;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Promix.Financials.UI.Converters;

public sealed class BoolToBalanceBackgroundBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush SuccessBrush = new(Color.FromArgb(255, 236, 253, 245));
    private static readonly SolidColorBrush DangerBrush = new(Color.FromArgb(255, 254, 242, 242));

    public object Convert(object value, Type targetType, object parameter, string language)
        => value is bool isBalanced && isBalanced ? SuccessBrush : DangerBrush;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}
