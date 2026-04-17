using System;
using Microsoft.UI.Xaml.Controls;

namespace Promix.Financials.UI.Services;

public sealed record TransientMessageRequest(
    InfoBarSeverity Severity,
    string Title,
    string Message,
    TimeSpan? AutoDismissAfter = null);

public sealed class TransientMessageService
{
    public event EventHandler<TransientMessageRequest>? MessageRaised;

    public void ShowSuccess(string message, string title = "تم التنفيذ")
        => MessageRaised?.Invoke(
            this,
            new TransientMessageRequest(
                InfoBarSeverity.Success,
                title,
                message,
                TimeSpan.FromSeconds(3)));

    public void ShowInfo(string message, string title = "معلومة")
        => MessageRaised?.Invoke(
            this,
            new TransientMessageRequest(
                InfoBarSeverity.Informational,
                title,
                message,
                TimeSpan.FromSeconds(3)));

    public void ShowError(string message, string title = "تعذر إكمال العملية")
        => MessageRaised?.Invoke(
            this,
            new TransientMessageRequest(
                InfoBarSeverity.Error,
                title,
                message,
                null));
}
