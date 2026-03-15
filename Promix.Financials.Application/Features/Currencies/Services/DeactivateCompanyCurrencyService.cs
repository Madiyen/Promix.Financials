using Promix.Financials.Application.Abstractions;
using Promix.Financials.Application.Features.Currencies.Commands;
using Promix.Financials.Domain.Exceptions;

namespace Promix.Financials.Application.Features.Currencies.Services;

public sealed class DeactivateCompanyCurrencyService
{
    private readonly ICompanyCurrencyRepository _currencies;

    public DeactivateCompanyCurrencyService(ICompanyCurrencyRepository currencies)
    {
        _currencies = currencies;
    }

    public async Task DeactivateAsync(DeactivateCompanyCurrencyCommand cmd, CancellationToken ct = default)
    {
        var currency = await _currencies.GetByIdAsync(cmd.Id, cmd.CompanyId, ct)
            ?? throw new BusinessRuleException("Currency not found.");

        // لا يمكن إيقاف العملة الرئيسية
        if (currency.IsBaseCurrency)
            throw new BusinessRuleException("Cannot deactivate the base currency.");

        currency.Deactivate();

        await _currencies.SaveChangesAsync(ct);
    }
}