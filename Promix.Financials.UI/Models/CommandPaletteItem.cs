using Promix.Financials.UI.Controls;
using Promix.Financials.UI.Navigation;
using Promix.Financials.UI.Services.Journals;

namespace Promix.Financials.UI.Models;

public enum CommandPaletteActionKind
{
    Navigate,
    OpenAccountStatement,
    CreateJournal,
    CreateAccount,
    CreateParty
}

public sealed record CommandPaletteItem(
    CommandPaletteActionKind ActionKind,
    string Title,
    string Subtitle,
    string SearchText,
    string Glyph,
    SidebarDestination? Destination = null,
    LedgerWorkspaceTab? LedgerTab = null,
    JournalQuickAction? JournalAction = null,
    Guid? AccountId = null);
