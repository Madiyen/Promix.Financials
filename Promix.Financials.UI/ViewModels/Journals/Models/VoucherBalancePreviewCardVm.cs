using Microsoft.UI.Xaml.Media;

namespace Promix.Financials.UI.ViewModels.Journals.Models;

public sealed record VoucherBalancePreviewCardVm(
    string Label,
    string AccountText,
    string MovementText,
    string BeforeBalanceText,
    string AfterBalanceText,
    Brush AccentBrush,
    Brush AccentBackgroundBrush
);
