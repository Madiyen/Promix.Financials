using System;
using System.Globalization;

namespace Promix.Financials.UI.Services;

public static class SmartDateParser
{
    public static bool TryParse(string? input, out DateTimeOffset value)
    {
        value = default;
        if (string.IsNullOrWhiteSpace(input))
            return false;

        var normalized = input.Trim().ToLowerInvariant();
        var today = DateTime.Today;

        if (normalized is "t" or "today" or "ي")
        {
            value = new DateTimeOffset(today);
            return true;
        }

        if ((normalized.StartsWith('+') || normalized.StartsWith('-'))
            && int.TryParse(normalized, NumberStyles.Integer, CultureInfo.InvariantCulture, out var offsetDays))
        {
            value = new DateTimeOffset(today.AddDays(offsetDays));
            return true;
        }

        if (DateTime.TryParse(normalized, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out var parsed))
        {
            value = new DateTimeOffset(parsed.Date);
            return true;
        }

        if (DateTime.TryParse(normalized, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out parsed))
        {
            value = new DateTimeOffset(parsed.Date);
            return true;
        }

        return false;
    }
}
