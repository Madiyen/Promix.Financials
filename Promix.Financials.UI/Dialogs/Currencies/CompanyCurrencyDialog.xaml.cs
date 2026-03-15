using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Promix.Financials.Application.Abstractions;
using Promix.Financials.Application.Features.Currencies.Commands;
using Promix.Financials.UI.ViewModels.Currencies.Models;
using System;
using System.Linq;
using WinUIApplication = Microsoft.UI.Xaml.Application;

namespace Promix.Financials.UI.Dialogs.Currencies;

public sealed partial class CompanyCurrencyDialog : ContentDialog
{
    private readonly Guid _companyId;
    private readonly CompanyCurrencyRowVm? _editTarget;

    public CompanyCurrencyDialog(Guid companyId, CompanyCurrencyRowVm? editTarget = null)
    {
        InitializeComponent();
        _companyId = companyId;
        _editTarget = editTarget;

        Loaded += async (_, _) =>
        {
            await LoadCurrenciesAsync();

            if (_editTarget is not null)
                FillEditMode();

            ValidateAll();
        };

        PrimaryButtonClick += (_, args) =>
        {
            ValidateAll();
            if (!_isValid) args.Cancel = true;
        };
    }

    // ─── تحميل العملات الافتراضية من DB ──────────────────────────
    private async System.Threading.Tasks.Task LoadCurrenciesAsync()
    {
        try
        {
            var services = ((App)WinUIApplication.Current).Services;
            var repo = services.GetRequiredService<ICurrencyRepository>();
            var list = await repo.GetAllActiveAsync();

            CurrencyComboBox.ItemsSource = list
                .Select(c => new DefaultCurrencyOptionVm(c.Code, c.NameAr, c.NameEn, c.Symbol))
                .ToList();
        }
        catch { /* تجاهل — القائمة ستكون فارغة */ }
    }

    // ─── وضع التعديل ──────────────────────────────────────────────
    private void FillEditMode()
    {
        DialogTitleText.Text = "تعديل العملة";

        // في وضع التعديل — الكود لا يتغير
        CurrencyComboBox.IsEnabled = false;

        // نعرض العملة الحالية كـ item مؤقت
        var item = new DefaultCurrencyOptionVm(
            _editTarget!.CurrencyCode,
            _editTarget.NameAr,
            _editTarget.NameEn,
            _editTarget.Symbol);

        CurrencyComboBox.ItemsSource = new[] { item };
        CurrencyComboBox.SelectedIndex = 0;

        SymbolBox.Text = _editTarget.Symbol;
        NameArBox.Text = _editTarget.NameAr;
        NameEnBox.Text = _editTarget.NameEn ?? "";
        ExchangeRateBox.Text = _editTarget.ExchangeRate.ToString("F4");
        DecimalPlacesBox.SelectedIndex = _editTarget.DecimalPlaces;
        IsBaseCurrencyToggle.IsOn = _editTarget.IsBaseCurrency;

        if (_editTarget.IsBaseCurrency)
        {
            ExchangeRateBox.IsReadOnly = true;
            ExchangeRateBox.Opacity = 0.6;
            IsBaseCurrencyToggle.IsEnabled = false;
        }
    }

    // ─── عند اختيار عملة من القائمة ───────────────────────────────
    private void CurrencyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CurrencyComboBox.SelectedItem is not DefaultCurrencyOptionVm selected) return;

        // تعبئة تلقائية
        PreviewCodeText.Text = selected.Code;
        PreviewNameArText.Text = selected.NameAr;
        PreviewNameEnText.Text = selected.NameEn ?? "";
        CurrencyPreviewBorder.Visibility = Visibility.Visible;

        // تعبئة الاسم إذا فارغ
        if (string.IsNullOrWhiteSpace(NameArBox.Text))
            NameArBox.Text = selected.NameAr;

        if (string.IsNullOrWhiteSpace(NameEnBox.Text))
            NameEnBox.Text = selected.NameEn ?? "";

        // تع��ئة الرمز تلقائياً
        SymbolBox.Text = selected.Symbol ?? selected.Code;

        ValidateAll();
    }

    // ─── Validation ───────────────────────────────────────────────
    private bool _isValid;

    private void ValidateAll()
    {
        var currencyOk = CurrencyComboBox.SelectedItem is not null;
        var nameOk = !string.IsNullOrWhiteSpace(NameArBox.Text);
        var rateOk = decimal.TryParse(ExchangeRateBox.Text, out var rate) && rate > 0;

        NameArError.Visibility = nameOk
            ? Visibility.Collapsed
            : Visibility.Visible;

        ExchangeRateError.Visibility = rateOk
            ? Visibility.Collapsed
            : Visibility.Visible;

        _isValid = currencyOk && nameOk && rateOk;
        IsPrimaryButtonEnabled = _isValid;
    }

    private void NameArBox_TextChanged(object sender, TextChangedEventArgs e)
        => ValidateAll();

    private void ExchangeRateBox_TextChanged(object sender, TextChangedEventArgs e)
        => ValidateAll();

    private void IsBaseCurrencyToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (IsBaseCurrencyToggle.IsOn)
        {
            ExchangeRateBox.Text = "1";
            ExchangeRateBox.IsReadOnly = true;
            ExchangeRateBox.Opacity = 0.6;
        }
        else
        {
            ExchangeRateBox.IsReadOnly = false;
            ExchangeRateBox.Opacity = 1;
        }
        ValidateAll();
    }

    // ─── Build Commands ───────────────────────────────────────────
    public CreateCompanyCurrencyCommand BuildCreateCommand()
    {
        var selected = (DefaultCurrencyOptionVm)CurrencyComboBox.SelectedItem!;
        decimal.TryParse(ExchangeRateBox.Text, out var rate);

        return new CreateCompanyCurrencyCommand(
            CompanyId: _companyId,
            CurrencyCode: selected.Code,
            NameAr: NameArBox.Text.Trim(),
            NameEn: string.IsNullOrWhiteSpace(NameEnBox.Text) ? null : NameEnBox.Text.Trim(),
            Symbol: string.IsNullOrWhiteSpace(SymbolBox.Text) ? null : SymbolBox.Text.Trim(),
            DecimalPlaces: (byte)DecimalPlacesBox.SelectedIndex,
            ExchangeRate: rate,
            IsBaseCurrency: IsBaseCurrencyToggle.IsOn
        );
    }

    public EditCompanyCurrencyCommand BuildEditCommand()
    {
        decimal.TryParse(ExchangeRateBox.Text, out var rate);

        return new EditCompanyCurrencyCommand(
            Id: _editTarget!.Id,
            CompanyId: _companyId,
            NameAr: NameArBox.Text.Trim(),
            NameEn: string.IsNullOrWhiteSpace(NameEnBox.Text) ? null : NameEnBox.Text.Trim(),
            Symbol: string.IsNullOrWhiteSpace(SymbolBox.Text) ? null : SymbolBox.Text.Trim(),
            DecimalPlaces: (byte)DecimalPlacesBox.SelectedIndex,
            ExchangeRate: rate
        );
    }
}