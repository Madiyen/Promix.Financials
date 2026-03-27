using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Promix.Financials.UI.ViewModels.Journals.Models;
using Windows.System;

namespace Promix.Financials.UI.Controls.Journals;

public sealed partial class AccountLookupBox : UserControl
{
    private bool _isUpdatingText;
    private bool _suspendSelectionReset;
    private bool _suppressSuggestionsOnFocusOnce;

    public AccountLookupBox()
    {
        InitializeComponent();
        Suggestions = new ObservableCollection<JournalAccountOptionVm>();
        Loaded += (_, _) => RefreshSelectionText();
    }

    public ObservableCollection<JournalAccountOptionVm> Suggestions { get; }

    public string Header
    {
        get => (string)GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(nameof(Header), typeof(string), typeof(AccountLookupBox), new PropertyMetadata("الحساب"));

    public string PlaceholderText
    {
        get => (string)GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    public static readonly DependencyProperty PlaceholderTextProperty =
        DependencyProperty.Register(nameof(PlaceholderText), typeof(string), typeof(AccountLookupBox), new PropertyMetadata("ابحث بالكود أو الاسم"));

    public IEnumerable<JournalAccountOptionVm>? ItemsSource
    {
        get => (IEnumerable<JournalAccountOptionVm>?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(
            nameof(ItemsSource),
            typeof(IEnumerable<JournalAccountOptionVm>),
            typeof(AccountLookupBox),
            new PropertyMetadata(null, OnItemsSourceChanged));

    public Guid? SelectedAccountId
    {
        get => ReadSelectedAccountId();
        set => SetValue(SelectedAccountIdProperty, value);
    }

    public static readonly DependencyProperty SelectedAccountIdProperty =
        DependencyProperty.Register(
            nameof(SelectedAccountId),
            typeof(object),
            typeof(AccountLookupBox),
            new PropertyMetadata(null, OnSelectedAccountIdChanged));

    public string SelectedCaption
    {
        get => (string)GetValue(SelectedCaptionProperty);
        private set => SetValue(SelectedCaptionProperty, value);
    }

    public static readonly DependencyProperty SelectedCaptionProperty =
        DependencyProperty.Register(nameof(SelectedCaption), typeof(string), typeof(AccountLookupBox), new PropertyMetadata(string.Empty));

    public bool HasSelection => SelectedAccountId is not null;

    public Visibility CaptionVisibility
    {
        get => (Visibility)GetValue(CaptionVisibilityProperty);
        private set => SetValue(CaptionVisibilityProperty, value);
    }

    public static readonly DependencyProperty CaptionVisibilityProperty =
        DependencyProperty.Register(nameof(CaptionVisibility), typeof(Visibility), typeof(AccountLookupBox), new PropertyMetadata(Visibility.Collapsed));

    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AccountLookupBox box)
        {
            box.RefreshSelectionText();
            box.PopulateSuggestions(box.SearchBox.Text);
        }
    }

    private static void OnSelectedAccountIdChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AccountLookupBox box)
        {
            box.RefreshSelectionText();
            box.UpdateHasSelection();
        }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs args)
    {
        if (_isUpdatingText)
            return;

        if (!_suspendSelectionReset && SelectedAccountId is not null)
        {
            var selected = FindSelectedOption();
            if (selected is null || !string.Equals(SearchBox.Text, selected.DisplayText, StringComparison.CurrentCulture))
            {
                SelectedAccountId = null;
                UpdateHasSelection();
            }
        }

        if (TryApplyExactMatch(SearchBox.Text))
            return;

        PopulateSuggestions(SearchBox.Text, openList: true);
    }

    private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
    {
        InputBorder.BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 37, 99, 235));

        if (_suppressSuggestionsOnFocusOnce)
        {
            _suppressSuggestionsOnFocusOnce = false;
            SuggestionsPanel.Visibility = Visibility.Collapsed;
            return;
        }

        var selected = FindSelectedOption();
        if (selected is not null &&
            string.Equals(SearchBox.Text, selected.DisplayText, StringComparison.CurrentCulture))
        {
            SuggestionsPanel.Visibility = Visibility.Collapsed;
            return;
        }

        PopulateSuggestions(SearchBox.Text, openList: ShouldShowDefaultSuggestions(SearchBox.Text));
        SearchBox.SelectAll();
    }

    private async void SearchBox_LostFocus(object sender, RoutedEventArgs e)
    {
        await Task.Delay(120);

        if (IsFocusWithinControl())
            return;

        InputBorder.BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 148, 163, 184));
        SuggestionsPanel.Visibility = Visibility.Collapsed;
    }

    private void SearchBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        switch (e.Key)
        {
            case VirtualKey.Enter:
            {
                var match = Suggestions.FirstOrDefault() ?? FindBestMatch(SearchBox.Text);
                if (match is not null)
                    ApplySelection(match);

                e.Handled = true;
                break;
            }

            case VirtualKey.Down:
            {
                if (Suggestions.Count == 0)
                    return;

                SuggestionsPanel.Visibility = Visibility.Visible;
                SuggestionsList.SelectedIndex = SuggestionsList.SelectedIndex < 0 ? 0 : SuggestionsList.SelectedIndex;
                if (SuggestionsList.ContainerFromIndex(SuggestionsList.SelectedIndex) is ListViewItem item)
                    item.Focus(FocusState.Keyboard);

                e.Handled = true;
                break;
            }

            case VirtualKey.Escape:
            {
                SuggestionsPanel.Visibility = Visibility.Collapsed;
                e.Handled = true;
                break;
            }
        }
    }

    private void SuggestionsList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is JournalAccountOptionVm option)
            ApplySelection(option);
    }

    private void SuggestionsList_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        switch (e.Key)
        {
            case VirtualKey.Enter:
            {
                if (SuggestionsList.SelectedItem is JournalAccountOptionVm option)
                {
                    ApplySelection(option);
                    e.Handled = true;
                }

                break;
            }

            case VirtualKey.Escape:
            {
                SuggestionsPanel.Visibility = Visibility.Collapsed;
                SearchBox.Focus(FocusState.Programmatic);
                e.Handled = true;
                break;
            }
        }
    }

    private void PopulateSuggestions(string? query, bool openList = false)
    {
        if (string.IsNullOrWhiteSpace(query) && !ShouldShowDefaultSuggestions(query))
        {
            Suggestions.Clear();
            SuggestionsPanel.Visibility = Visibility.Collapsed;
            SuggestionsList.SelectedIndex = -1;
            return;
        }

        var matches = (ItemsSource ?? Enumerable.Empty<JournalAccountOptionVm>())
            .Select(x => new { Account = x, Rank = x.GetMatchRank(query) })
            .Where(x => x.Rank < int.MaxValue)
            .OrderBy(x => x.Rank)
            .ThenBy(x => x.Account.Code.Length)
            .ThenBy(x => x.Account.Code)
            .Take(12)
            .Select(x => x.Account)
            .ToList();

        Suggestions.Clear();
        foreach (var match in matches)
            Suggestions.Add(match);

        SuggestionsPanel.Visibility = openList && Suggestions.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        SuggestionsList.SelectedIndex = Suggestions.Count > 0 ? 0 : -1;
    }

    private void RefreshSelectionText()
    {
        var option = FindSelectedOption();

        SelectedCaption = option is null ? string.Empty : $"المحدد: {option.DisplayText}";

        if (option is not null)
            UpdateDisplayedText(option.DisplayText);
        else if (!_isUpdatingText)
            UpdateDisplayedText(string.Empty);
    }

    private void UpdateDisplayedText(string text)
    {
        _isUpdatingText = true;
        _suspendSelectionReset = true;
        SearchBox.Text = text;
        SearchBox.Select(text.Length, 0);
        _suspendSelectionReset = false;
        _isUpdatingText = false;
    }

    private void UpdateHasSelection()
        => CaptionVisibility = HasSelection ? Visibility.Visible : Visibility.Collapsed;

    private Guid? ReadSelectedAccountId()
        => GetValue(SelectedAccountIdProperty) is Guid accountId
            ? accountId
            : null;

    private void ApplySelection(JournalAccountOptionVm option)
    {
        _suppressSuggestionsOnFocusOnce = true;
        SelectedAccountId = option.Id;
        UpdateDisplayedText(option.DisplayText);
        UpdateHasSelection();
        SuggestionsPanel.Visibility = Visibility.Collapsed;
        SearchBox.Focus(FocusState.Programmatic);
    }

    private JournalAccountOptionVm? FindSelectedOption()
        => (ItemsSource ?? Enumerable.Empty<JournalAccountOptionVm>())
            .FirstOrDefault(x => x.Id == SelectedAccountId);

    private JournalAccountOptionVm? FindBestMatch(string? query)
        => (ItemsSource ?? Enumerable.Empty<JournalAccountOptionVm>())
            .Select(x => new { Account = x, Rank = x.GetMatchRank(query) })
            .Where(x => x.Rank < int.MaxValue)
            .OrderBy(x => x.Rank)
            .ThenBy(x => x.Account.Code.Length)
            .ThenBy(x => x.Account.Code)
            .Select(x => x.Account)
            .FirstOrDefault();

    private bool TryApplyExactMatch(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return false;

        var normalizedQuery = query.Trim();
        var exactMatches = (ItemsSource ?? Enumerable.Empty<JournalAccountOptionVm>())
            .Where(x =>
                string.Equals(x.Code, normalizedQuery, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(x.NameAr, normalizedQuery, StringComparison.CurrentCultureIgnoreCase) ||
                string.Equals(x.DisplayText, normalizedQuery, StringComparison.CurrentCultureIgnoreCase))
            .Take(2)
            .ToList();

        if (exactMatches.Count != 1)
            return false;

        ApplySelection(exactMatches[0]);
        return true;
    }

    private bool ShouldShowDefaultSuggestions(string? query)
    {
        if (!string.IsNullOrWhiteSpace(query))
            return true;

        return (ItemsSource ?? Enumerable.Empty<JournalAccountOptionVm>()).Take(9).Count() <= 8;
    }

    private bool IsFocusWithinControl()
    {
        if (XamlRoot is null)
            return false;

        var focused = FocusManager.GetFocusedElement(XamlRoot) as DependencyObject;
        while (focused is not null)
        {
            if (ReferenceEquals(focused, this))
                return true;

            focused = VisualTreeHelper.GetParent(focused);
        }

        return false;
    }
}
