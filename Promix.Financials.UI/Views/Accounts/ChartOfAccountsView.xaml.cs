using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Promix.Financials.Application.Abstractions;
using Promix.Financials.Application.Features.Accounts;
using Promix.Financials.Application.Features.Accounts.Commands;
using Promix.Financials.Application.Features.Accounts.Services;
using Promix.Financials.Domain.Enums;
using Promix.Financials.UI.Dialogs.Accounts;
using Promix.Financials.UI.Services;
using Promix.Financials.UI.ViewModels.Accounts;
using Promix.Financials.UI.ViewModels.Accounts.Models;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.System;

namespace Promix.Financials.UI.Views.Accounts;

public sealed partial class ChartOfAccountsView : Page
{
    private readonly ChartOfAccountsViewModel _vm;
    private readonly IServiceScope _scope;
    private readonly TransientMessageService _messageService;

    public ChartOfAccountsView()
    {
        InitializeComponent();
        var app = (App)Microsoft.UI.Xaml.Application.Current;
        _scope = app.Services.CreateScope();
        _vm = _scope.ServiceProvider.GetRequiredService<ChartOfAccountsViewModel>();
        _messageService = _scope.ServiceProvider.GetRequiredService<TransientMessageService>();
        DataContext = _vm;
        _vm.PropertyChanged += ViewModel_PropertyChanged;
        _vm.AccountTree.CollectionChanged += AccountTree_CollectionChanged;
        Loaded += OnLoaded;
        RegisterKeyboardAccelerators();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var userContext = _scope.ServiceProvider.GetRequiredService<IUserContext>();
            var companyId = userContext.CompanyId ?? Guid.Empty;
            if (companyId == Guid.Empty) return;

            await _vm.InitializeAsync(companyId);
            ApplyFilterChipStyles();
            RebuildTreeNodes();
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("خطأ عند التحميل", ex.Message);
        }
    }

    private void AccountTree_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => RebuildTreeNodes();

    private async void NewAccount_Click(object sender, RoutedEventArgs e)
        => await OpenNewAccountDialogAsync(preselectedParentCode: null);

    private async void AddFirstAccount_Click(object sender, RoutedEventArgs e)
        => await OpenNewAccountDialogAsync(preselectedParentCode: null);

    private async void AddChildFromDetails_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetSelectedNode(out var node))
            return;

        await OpenNewAccountDialogAsync(node.Code);
    }

    private async void EditSelectedAccount_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetSelectedNode(out var node))
            return;

        await OpenEditAccountDialogAsync(node);
    }

    private async void DeleteSelectedAccount_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetSelectedNode(out var node))
            return;

        await DeleteNodeAsync(node);
    }

    private async void AccountListRowButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TryResolveAccountId(sender, out var accountId))
            return;

        _vm.HighlightAccount(accountId);
    }

    private async void TreeAccountRowButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TryResolveAccountId(sender, out var accountId))
            return;

        await _vm.SelectAccountAsync(accountId);
    }

    private async void AccountDetailsButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TryResolveAccountId(sender, out var accountId))
            return;

        await OpenAccountDetailsDialogAsync(accountId);
    }

    private async void AddChildAccount_Click(object sender, RoutedEventArgs e)
    {
        if (!TryResolveAccountNode(sender, out var node))
            return;

        await OpenNewAccountDialogAsync(node.Code);
    }

    private async void EditAccount_Click(object sender, RoutedEventArgs e)
    {
        if (!TryResolveAccountNode(sender, out var node))
            return;

        await OpenEditAccountDialogAsync(node);
    }

    private async void DeleteAccount_Click(object sender, RoutedEventArgs e)
    {
        if (!TryResolveAccountNode(sender, out var node))
            return;

        await DeleteNodeAsync(node);
    }

    private async Task OpenNewAccountDialogAsync(string? preselectedParentCode)
    {
        try
        {
            var userContext = _scope.ServiceProvider.GetRequiredService<IUserContext>();
            var companyId = userContext.CompanyId ?? Guid.Empty;
            if (companyId == Guid.Empty) return;

            using var scope = ((App)Microsoft.UI.Xaml.Application.Current).Services.CreateScope();
            var vm = scope.ServiceProvider.GetRequiredService<NewAccountDialogViewModel>();
            await vm.InitializeAsync(companyId);

            if (!string.IsNullOrWhiteSpace(preselectedParentCode))
            {
                foreach (var parent in vm.ParentAccounts)
                {
                    if (!string.Equals(parent.Code, preselectedParentCode, StringComparison.OrdinalIgnoreCase))
                        continue;

                    vm.SelectedParentAccount = parent;
                    break;
                }
            }

            var dialog = new NewAccountDialog(vm) { XamlRoot = XamlRoot };
            await dialog.ShowAsync();
            if (!dialog.IsSubmitted) return;

            var draft = vm.BuildDraft();
            var createService = scope.ServiceProvider.GetRequiredService<CreateAccountService>();
            var result = await createService.CreateAsync(new CreateAccountCommand(
                draft.CompanyId,
                draft.ParentId,
                draft.Code,
                draft.ArabicName,
                draft.EnglishName,
                draft.IsPosting,
                DeriveNature(draft.Code),
                draft.CurrencyCode,
                draft.SystemRole,
                draft.IsActive,
                draft.Notes));

            await _vm.InitializeAsync(companyId);
            await _vm.SelectAccountAsync(result.AccountId);
            _messageService.ShowSuccess("تم إنشاء الحساب الجديد.");
        }
        catch (Promix.Financials.Domain.Exceptions.BusinessRuleException ex)
        {
            _messageService.ShowError(ex.Message, "تعذّر إنشاء الحساب");
        }
        catch (Exception ex)
        {
            _messageService.ShowError(ex.Message, "خطأ غير متوقع");
        }
    }

    private async Task OpenEditAccountDialogAsync(AccountNodeVm node)
    {
        try
        {
            var userContext = _scope.ServiceProvider.GetRequiredService<IUserContext>();
            var companyId = userContext.CompanyId ?? Guid.Empty;
            if (companyId == Guid.Empty) return;

            using var scope = ((App)Microsoft.UI.Xaml.Application.Current).Services.CreateScope();
            var vm = scope.ServiceProvider.GetRequiredService<EditAccountDialogViewModel>();
            await vm.InitializeAsync(node.Id, companyId);

            var dialog = new EditAccountDialog(vm) { XamlRoot = XamlRoot };
            if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;

            var editService = scope.ServiceProvider.GetRequiredService<EditAccountService>();
            await editService.EditAsync(vm.BuildCommand());
            await _vm.InitializeAsync(companyId);
            await _vm.SelectAccountAsync(node.Id);
            _messageService.ShowSuccess("تم تحديث الحساب.");
        }
        catch (Promix.Financials.Domain.Exceptions.BusinessRuleException ex)
        {
            _messageService.ShowError(ex.Message, "تعذّر تعديل الحساب");
        }
        catch (Exception ex)
        {
            _messageService.ShowError(ex.Message, "خطأ غير متوقع");
        }
    }

    private async Task DeleteNodeAsync(AccountNodeVm node)
    {
        var confirm = new ContentDialog
        {
            Title = "تأكيد الحذف",
            Content = $"هل تريد حذف الحساب؟\n\nالكود: {node.Code}\nالاسم: {node.ArabicName}\n\nلا يمكن التراجع عن هذا الإجراء.",
            PrimaryButtonText = "حذف",
            CloseButtonText = "إلغاء",
            XamlRoot = XamlRoot,
            DefaultButton = ContentDialogButton.Close
        };

        if (await confirm.ShowAsync() != ContentDialogResult.Primary)
            return;

        try
        {
            var userContext = _scope.ServiceProvider.GetRequiredService<IUserContext>();
            var companyId = userContext.CompanyId ?? Guid.Empty;

            using var scope = ((App)Microsoft.UI.Xaml.Application.Current).Services.CreateScope();
            var deleteService = scope.ServiceProvider.GetRequiredService<DeleteAccountService>();
            await deleteService.DeleteAsync(node.Id, companyId);

            await _vm.InitializeAsync(companyId);
            _messageService.ShowSuccess("تم حذف الحساب.");
        }
        catch (Promix.Financials.Domain.Exceptions.BusinessRuleException ex)
        {
            _messageService.ShowError(ex.Message, "تعذّر حذف الحساب");
        }
        catch (Exception ex)
        {
            _messageService.ShowError(ex.Message, "خطأ غير متوقع");
        }
    }

    private void ExpandAll_Click(object sender, RoutedEventArgs e)
        => SetExpandStateAll(AccountsTreeView.RootNodes, true);

    private void CollapseAll_Click(object sender, RoutedEventArgs e)
        => SetExpandStateAll(AccountsTreeView.RootNodes, false);

    private static void SetExpandStateAll(IList<TreeViewNode> nodes, bool isExpanded)
    {
        foreach (var node in nodes)
        {
            node.IsExpanded = isExpanded;
            if (node.Children.Count > 0)
                SetExpandStateAll(node.Children, isExpanded);
        }
    }

    private bool TryGetSelectedNode(out AccountNodeVm node)
    {
        node = null!;
        var selectedId = _vm.SelectedAccountDetail?.Id;
        if (!selectedId.HasValue)
            return false;

        node = FindNodeById(_vm.AccountTree, selectedId.Value)!;
        return node is not null;
    }

    private bool TryResolveAccountId(object sender, out Guid accountId)
    {
        accountId = Guid.Empty;

        if (!TryResolveTag(sender, out var tag))
            return false;

        switch (tag)
        {
            case AccountNodeVm node:
                accountId = node.Id;
                return true;
            case AccountListRowVm row:
                accountId = row.Id;
                return true;
            default:
                return false;
        }
    }

    private bool TryResolveAccountNode(object sender, out AccountNodeVm node)
    {
        node = null!;

        if (!TryResolveTag(sender, out var tag))
            return false;

        if (tag is AccountNodeVm accountNode)
        {
            node = accountNode;
            return true;
        }

        if (tag is not AccountListRowVm row)
            return false;

        node = FindNodeById(_vm.AccountTree, row.Id)!;
        return node is not null;
    }

    private static bool TryResolveTag(object sender, out object? tag)
    {
        tag = sender is FrameworkElement fe ? fe.Tag : null;

        return tag is not null;
    }

    private static AccountNodeVm? FindNodeById(IEnumerable<AccountNodeVm> nodes, Guid id)
    {
        foreach (var node in nodes)
        {
            if (node.Id == id)
                return node;

            var child = FindNodeById(node.Children, id);
            if (child is not null)
                return child;
        }

        return null;
    }

    private void SystemFilterChipButton_Click(object sender, RoutedEventArgs e)
        => _vm.ShowSystemAccountsOnly = !_vm.ShowSystemAccountsOnly;

    private void InactiveFilterChipButton_Click(object sender, RoutedEventArgs e)
        => _vm.ShowInactiveAccountsOnly = !_vm.ShowInactiveAccountsOnly;

    private void PartyFilterChipButton_Click(object sender, RoutedEventArgs e)
        => _vm.ShowPartyAccountsOnly = !_vm.ShowPartyAccountsOnly;

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ChartOfAccountsViewModel.ShowSystemAccountsOnly)
            or nameof(ChartOfAccountsViewModel.ShowInactiveAccountsOnly)
            or nameof(ChartOfAccountsViewModel.ShowPartyAccountsOnly))
        {
            ApplyFilterChipStyles();
        }
    }

    private void ApplyFilterChipStyles()
    {
        ApplyFilterChipStyle(SystemFilterChipButton, _vm.ShowSystemAccountsOnly);
        ApplyFilterChipStyle(InactiveFilterChipButton, _vm.ShowInactiveAccountsOnly);
        ApplyFilterChipStyle(PartyFilterChipButton, _vm.ShowPartyAccountsOnly);
    }

    private void ApplyFilterChipStyle(Button button, bool isSelected)
    {
        if (Microsoft.UI.Xaml.Application.Current.Resources[isSelected
                ? "FilterChipButtonSelectedStyle"
                : "FilterChipButtonStyle"] is Style style)
        {
            button.Style = style;
        }
    }

    private void RebuildTreeNodes()
    {
        if (AccountsTreeView is null)
            return;

        AccountsTreeView.RootNodes.Clear();
        foreach (var account in _vm.AccountTree)
            AccountsTreeView.RootNodes.Add(BuildTreeNode(account));
    }

    private static TreeViewNode BuildTreeNode(AccountNodeVm account)
    {
        var node = new TreeViewNode
        {
            Content = account,
            IsExpanded = true
        };

        foreach (var child in account.Children)
            node.Children.Add(BuildTreeNode(child));

        return node;
    }

    private static AccountNature DeriveNature(string code)
    {
        var root = code?.Split('.')[0] ?? string.Empty;
        return root switch
        {
            "2" or "3" or "4" => AccountNature.Credit,
            _ => AccountNature.Debit
        };
    }

    private async Task ShowErrorAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "حسناً",
            XamlRoot = XamlRoot
        };

        await dialog.ShowAsync();
    }

    private void RegisterKeyboardAccelerators()
    {
        KeyboardAccelerators.Add(CreateAccelerator(VirtualKey.N, async (_, args) =>
        {
            args.Handled = true;
            await OpenNewAccountDialogAsync(preselectedParentCode: null);
        }));
    }

    private static KeyboardAccelerator CreateAccelerator(
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

    private async Task OpenAccountDetailsDialogAsync(Guid accountId)
    {
        try
        {
            var details = await _vm.SelectAccountAsync(accountId);
            if (details is null)
            {
                await ShowErrorAsync("تعذّر تحميل التفاصيل", "لم يتم العثور على بيانات الحساب المطلوبة.");
                return;
            }

            var detailsStack = new StackPanel { Spacing = 14 };

            var badges = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            badges.Children.Add(CreateBadge(details.ClassificationText, "#EFF6FF", "#1D4ED8"));
            badges.Children.Add(CreateBadge(details.NatureText, "#ECFDF5", "#166534"));
            badges.Children.Add(CreateBadge(details.OriginText, "#F5F3FF", "#6D28D9"));
            detailsStack.Children.Add(badges);

            detailsStack.Children.Add(CreateDetailGrid(
                ("الكود", details.Code),
                ("الاسم", details.ArabicName),
                ("الحساب الأب", details.ParentDisplay),
                ("الرصيد", details.BalanceText),
                ("نوع الحساب", details.TypeText),
                ("الحالة", details.StatusText),
                ("العملة", details.CurrencyText),
                ("الدور النظامي", details.SystemRoleText),
                ("الترحيل اليدوي", details.PostingModeText),
                ("الأطراف المرتبطة", details.LinkedPartiesText)));

            detailsStack.Children.Add(new TextBlock
            {
                Text = details.UsageHeadline,
                TextWrapping = TextWrapping.Wrap,
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Microsoft.UI.Xaml.Application.Current.Resources["SecondaryTextBrush"]
            });

            if (details.BlockingReasons.Count > 0)
            {
                var reasonsPanel = new StackPanel { Spacing = 8 };
                reasonsPanel.Children.Add(new TextBlock
                {
                    Text = "قيود الاستخدام",
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
                });

                foreach (var reason in details.BlockingReasons)
                {
                    reasonsPanel.Children.Add(new Border
                    {
                        Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 254, 242, 242)),
                        BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 254, 202, 202)),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(10),
                        Padding = new Thickness(10, 8, 10, 8),
                        Child = new TextBlock
                        {
                            Text = reason,
                            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 185, 28, 28)),
                            TextWrapping = TextWrapping.Wrap
                        }
                    });
                }

                detailsStack.Children.Add(reasonsPanel);
            }

            var dialog = new ContentDialog
            {
                Title = $"تفاصيل الحساب - {details.ArabicName}",
                PrimaryButtonText = "إغلاق",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = XamlRoot,
                Content = new ScrollViewer
                {
                    MaxHeight = 560,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Content = detailsStack
                }
            };

            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("تعذّر عرض التفاصيل", ex.Message);
        }
    }

    private static Border CreateBadge(string text, string backgroundHex, string foregroundHex)
    {
        return new Border
        {
            Background = CreateBrush(backgroundHex),
            CornerRadius = new CornerRadius(999),
            Padding = new Thickness(10, 5, 10, 5),
            Child = new TextBlock
            {
                Text = text,
                Foreground = CreateBrush(foregroundHex),
                FontSize = 11.5,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            }
        };
    }

    private static Grid CreateDetailGrid(params (string Label, string Value)[] items)
    {
        var grid = new Grid
        {
            ColumnSpacing = 16,
            RowSpacing = 10
        };

        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        for (var i = 0; i < items.Length; i += 2)
        {
            var rowIndex = i / 2;
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            AddDetailCell(grid, rowIndex, 0, items[i].Label, items[i].Value);

            if (i + 1 < items.Length)
                AddDetailCell(grid, rowIndex, 1, items[i + 1].Label, items[i + 1].Value);
        }

        return grid;
    }

    private static void AddDetailCell(Grid grid, int row, int column, string label, string value)
    {
        var panel = new StackPanel { Spacing = 4 };
        panel.Children.Add(new TextBlock
        {
            Text = label,
            Foreground = (Microsoft.UI.Xaml.Media.Brush)Microsoft.UI.Xaml.Application.Current.Resources["SecondaryTextBrush"],
            FontSize = 12
        });
        panel.Children.Add(new TextBlock
        {
            Text = value,
            TextWrapping = TextWrapping.Wrap,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        });

        Grid.SetRow(panel, row);
        Grid.SetColumn(panel, column);
        grid.Children.Add(panel);
    }

    private static Microsoft.UI.Xaml.Media.SolidColorBrush CreateBrush(string hex)
    {
        var value = hex.TrimStart('#');
        return new Microsoft.UI.Xaml.Media.SolidColorBrush(
            Microsoft.UI.ColorHelper.FromArgb(
                255,
                Convert.ToByte(value[..2], 16),
                Convert.ToByte(value.Substring(2, 2), 16),
                Convert.ToByte(value.Substring(4, 2), 16)));
    }
}
