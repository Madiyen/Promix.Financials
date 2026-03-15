using Promix.Financials.Application.Abstractions;
using Promix.Financials.Application.Features.Currencies.Commands;
using Promix.Financials.Domain.Exceptions;

namespace Promix.Financials.Application.Features.Currencies.Services;

public sealed class EditCompanyCurrencyService
{
    private readonly ICompanyCurrencyRepository _currencies;

    public EditCompanyCurrencyService(ICompanyCurrencyRepository currencies)
    {
        _currencies = currencies;
    }

    public async Task EditAsync(EditCompanyCurrencyCommand cmd, CancellationToken ct = default)
    {
        if (cmd.CompanyId == Guid.Empty)
            throw new BusinessRuleException("CompanyId is required.");

        var nameAr = cmd.NameAr.Trim();
        if (string.IsNullOrWhiteSpace(nameAr))
            throw new BusinessRuleException("Arabic name is required.");

        var currency = await _currencies.GetByIdAsync(cmd.Id, cmd.CompanyId, ct)
            ?? throw new BusinessRuleException("Currency not found.");

        // تعديل البيانات التوصيفية
        currency.Update(
            nameAr: nameAr,
            nameEn: cmd.NameEn?.Trim(),
            symbol: cmd.Symbol?.Trim(),
            decimalPlaces: cmd.DecimalPlaces
        );

        // تعديل سعر التعادل — Domain يمنع تلقائياً تعديله للعملة الرئيسية
        if (cmd.ExchangeRate != currency.ExchangeRate)
            currency.UpdateRate(cmd.ExchangeRate);

        await _currencies.SaveChangesAsync(ct);
    }
}