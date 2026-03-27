using System;
using System.Collections.Generic;
using System.Text.Json;
using Promix.Financials.Domain.Enums;
using Windows.Storage;

namespace Promix.Financials.UI.Services.Journals;

public interface IJournalQuickDefaultsStore
{
    JournalQuickDefaults Load(JournalEntryType type);
    void Save(JournalEntryType type, JournalQuickDefaults defaults);
}

public sealed record JournalQuickDefaults(
    Guid? CashAccountId,
    Guid? CounterpartyAccountId,
    Guid? SourceAccountId,
    Guid? TargetAccountId,
    string? CurrencyCode
);

public sealed class LocalSettingsJournalQuickDefaultsStore : IJournalQuickDefaultsStore
{
    private const string DefaultsKey = "JournalQuickDefaults";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.General);

    public JournalQuickDefaults Load(JournalEntryType type)
    {
        var allDefaults = LoadAll();
        return allDefaults.TryGetValue(type.ToString(), out var defaults)
            ? defaults
            : new JournalQuickDefaults(null, null, null, null, null);
    }

    public void Save(JournalEntryType type, JournalQuickDefaults defaults)
    {
        var allDefaults = LoadAll();
        allDefaults[type.ToString()] = defaults;
        ApplicationData.Current.LocalSettings.Values[DefaultsKey] = JsonSerializer.Serialize(allDefaults, JsonOptions);
    }

    private static Dictionary<string, JournalQuickDefaults> LoadAll()
    {
        if (ApplicationData.Current.LocalSettings.Values.TryGetValue(DefaultsKey, out var raw)
            && raw is string json
            && !string.IsNullOrWhiteSpace(json))
        {
            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, JournalQuickDefaults>>(json, JsonOptions)
                    ?? new Dictionary<string, JournalQuickDefaults>(StringComparer.OrdinalIgnoreCase);
            }
            catch
            {
            }
        }

        return new Dictionary<string, JournalQuickDefaults>(StringComparer.OrdinalIgnoreCase);
    }
}
