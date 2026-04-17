using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Promix.Financials.UI.Models;
using Windows.System;

namespace Promix.Financials.UI.Dialogs;

public sealed partial class CommandPaletteDialog : ContentDialog
{
    private readonly List<CommandPaletteItem> _allItems;

    public CommandPaletteDialog(IEnumerable<CommandPaletteItem> items)
    {
        InitializeComponent();
        _allItems = items.ToList();
        Results = new ObservableCollection<CommandPaletteItem>(_allItems);
        ResultsList.ItemsSource = Results;
        Loaded += OnLoaded;
    }

    public ObservableCollection<CommandPaletteItem> Results { get; }

    public CommandPaletteItem? SelectedCommand { get; private set; }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        CommandSearchBox.Focus(FocusState.Programmatic);
        ResultsList.SelectedIndex = Results.Count > 0 ? 0 : -1;
        UpdateEmptyState();
    }

    private void CommandSearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput
            && args.Reason != AutoSuggestionBoxTextChangeReason.ProgrammaticChange)
            return;

        ApplyFilter(sender.Text);
    }

    private void CommandSearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (args.ChosenSuggestion is CommandPaletteItem item)
        {
            Complete(item);
            return;
        }

        var selected = ResultsList.SelectedItem as CommandPaletteItem ?? Results.FirstOrDefault();
        if (selected is not null)
            Complete(selected);
    }

    private void ResultsList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is CommandPaletteItem item)
            Complete(item);
    }

    private void ResultsList_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key != VirtualKey.Enter)
            return;

        if (ResultsList.SelectedItem is not CommandPaletteItem item)
            return;

        e.Handled = true;
        Complete(item);
    }

    private void ApplyFilter(string? query)
    {
        var normalized = query?.Trim() ?? string.Empty;
        var filtered = string.IsNullOrWhiteSpace(normalized)
            ? _allItems
            : _allItems
                .Where(item =>
                    item.Title.Contains(normalized, StringComparison.CurrentCultureIgnoreCase)
                    || item.Subtitle.Contains(normalized, StringComparison.CurrentCultureIgnoreCase)
                    || item.SearchText.Contains(normalized, StringComparison.CurrentCultureIgnoreCase))
                .ToList();

        Results.Clear();
        foreach (var item in filtered)
            Results.Add(item);

        ResultsList.SelectedIndex = Results.Count > 0 ? 0 : -1;
        UpdateEmptyState();
    }

    private void UpdateEmptyState()
    {
        EmptyState.Visibility = Results.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Complete(CommandPaletteItem item)
    {
        SelectedCommand = item;
        Hide();
    }
}
