using Microsoft.UI.Xaml.Media;

namespace Promix.Financials.UI.ViewModels.Journals.Models;

public sealed record VoucherPostingPreviewLineVm(
    string SideText,
    string AccountText,
    string AmountText,
    Brush AccentBrush,
    Brush AccentBackgroundBrush
);
