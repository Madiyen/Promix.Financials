using System;
using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Promix.Financials.UI.Services;

public static class SmartInputParser
{
    private static readonly Regex AllowedExpressionPattern = new("^[0-9٠-٩.,+\\-*/\\s]+$", RegexOptions.Compiled);

    public static bool TryParseAmount(string? input, out double value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(input))
            return false;

        var normalized = NormalizeNumericText(input);
        if (double.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out value))
            return true;

        if (!AllowedExpressionPattern.IsMatch(normalized))
            return false;

        try
        {
            using var table = new DataTable();
            table.Locale = CultureInfo.InvariantCulture;
            var result = table.Compute(normalized, string.Empty);
            if (result is null)
                return false;

            value = Convert.ToDouble(result, CultureInfo.InvariantCulture);
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
        catch
        {
            return false;
        }
    }

    private static string NormalizeNumericText(string input)
        => input.Trim()
            .Replace("٠", "0", StringComparison.Ordinal)
            .Replace("١", "1", StringComparison.Ordinal)
            .Replace("٢", "2", StringComparison.Ordinal)
            .Replace("٣", "3", StringComparison.Ordinal)
            .Replace("٤", "4", StringComparison.Ordinal)
            .Replace("٥", "5", StringComparison.Ordinal)
            .Replace("٦", "6", StringComparison.Ordinal)
            .Replace("٧", "7", StringComparison.Ordinal)
            .Replace("٨", "8", StringComparison.Ordinal)
            .Replace("٩", "9", StringComparison.Ordinal)
            .Replace("٫", ".", StringComparison.Ordinal)
            .Replace(",", ".", StringComparison.Ordinal);
}
