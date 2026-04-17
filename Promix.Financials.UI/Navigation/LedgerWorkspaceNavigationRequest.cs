using System;

namespace Promix.Financials.UI.Navigation;

public enum LedgerWorkspaceTab
{
    Accounts,
    Journals,
    AccountStatement,
    TrialBalance,
    ReceivablesPayables,
    FinancialYears
}

public sealed record LedgerWorkspaceNavigationRequest(
    LedgerWorkspaceTab InitialTab,
    Guid? AccountId = null,
    Guid? EntryId = null);
