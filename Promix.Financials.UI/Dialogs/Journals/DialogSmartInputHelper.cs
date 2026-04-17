using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Promix.Financials.UI.Services;
using Windows.Foundation;
using Windows.System;

namespace Promix.Financials.UI.Dialogs.Journals;

internal static class DialogSmartInputHelper
{
    public static bool TryApplyAmount(NumberBox? numberBox, Action<string> showError)
    {
        if (numberBox is null)
            return false;

        var rawText = numberBox.Text;
        if (string.IsNullOrWhiteSpace(rawText))
            return false;

        if (SmartInputParser.TryParseAmount(rawText, out var value))
        {
            numberBox.Value = value;
            return true;
        }

        showError("تعذر فهم الصيغة الحسابية المدخلة. استخدم أرقامًا وعمليات مثل 1500+250 أو 3000/3.");
        return false;
    }

    public static bool TryApplyDate(TextBox? textBox, Action<DateTimeOffset> applyDate, Action<string> showError)
    {
        if (textBox is null)
            return false;

        var rawText = textBox.Text;
        if (string.IsNullOrWhiteSpace(rawText))
            return false;

        if (SmartDateParser.TryParse(rawText, out var value))
        {
            applyDate(value);
            textBox.Text = string.Empty;
            return true;
        }

        showError("لم أفهم اختصار التاريخ. استخدم t أو ي لليوم، أو -1 للأمس، أو +1 للغد.");
        return false;
    }

    public static KeyboardAccelerator CreateAccelerator(
        VirtualKey key,
        TypedEventHandler<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs> handler,
        VirtualKeyModifiers modifiers = VirtualKeyModifiers.Control)
    {
        var accelerator = new KeyboardAccelerator
        {
            Key = key,
            Modifiers = modifiers
        };
        accelerator.Invoked += handler;
        return accelerator;
    }
}
