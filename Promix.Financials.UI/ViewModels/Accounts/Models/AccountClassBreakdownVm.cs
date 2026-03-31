using System;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Promix.Financials.Domain.Enums;

namespace Promix.Financials.UI.ViewModels.Accounts.Models;

public sealed class AccountClassBreakdownVm
{
    public string ClassificationText { get; init; } = "—";
    public string AccountsCountText { get; init; } = "0";
    public string PercentageText { get; init; } = "0%";
    public string BalanceText { get; init; } = "0.00";
    public Brush IndicatorBrush { get; init; } = CreateBrush("#CBD5E1");
    public Brush TrackBrush { get; init; } = CreateBrush("#E2E8F0");
    public double PercentageWidth { get; init; }

    public static AccountClassBreakdownVm Create(AccountClass classification, int accountsCount, decimal totalBalance, int grandTotal)
    {
        var percentage = grandTotal <= 0
            ? 0d
            : Math.Round(accountsCount * 100d / grandTotal, 1, MidpointRounding.AwayFromZero);

        return new AccountClassBreakdownVm
        {
            ClassificationText = classification switch
            {
                AccountClass.Assets => "الأصول",
                AccountClass.Liabilities => "الخصوم",
                AccountClass.Equity => "حقوق الملكية",
                AccountClass.Revenue => "الإيرادات",
                _ => "المصروفات"
            },
            AccountsCountText = accountsCount.ToString("N0"),
            PercentageText = $"{percentage:0.#}%",
            BalanceText = Math.Abs(totalBalance).ToString("#,##0.00", System.Globalization.CultureInfo.InvariantCulture),
            IndicatorBrush = CreateBrush(classification switch
            {
                AccountClass.Assets => "#2563EB",
                AccountClass.Liabilities => "#EF4444",
                AccountClass.Equity => "#8B5CF6",
                AccountClass.Revenue => "#10B981",
                _ => "#F59E0B"
            }),
            TrackBrush = CreateBrush("#E2E8F0"),
            PercentageWidth = Math.Max(percentage, 6)
        };
    }

    private static SolidColorBrush CreateBrush(string hex)
    {
        var value = hex.TrimStart('#');
        var color = ColorHelper.FromArgb(
            255,
            Convert.ToByte(value[..2], 16),
            Convert.ToByte(value.Substring(2, 2), 16),
            Convert.ToByte(value.Substring(4, 2), 16));

        return new SolidColorBrush(color);
    }
}
