// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.NamingStyles;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options.Style.NamingPreferences;
using Microsoft.VisualStudio.PlatformUI;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options.Style;

internal partial class NamingStyleOptionPageControl : AbstractOptionPageControl
{
    public static string ReorderHeader => ServicesVSResources.Reorder;
    public static string SpecificationHeader => ServicesVSResources.Specification;
    public static string RequiredStyleHeader => ServicesVSResources.Required_Style;
    public static string SeverityHeader => ServicesVSResources.Severity;
    public static string ExplanatoryText => ServicesVSResources.For_a_given_symbol_only_the_topmost_rule_with_a_matching_Specification_will_be_applied_Violation_of_that_rules_Required_Style_will_be_reported_at_the_chosen_Severity_level;
    public static string AddRuleAutomationText => ServicesVSResources.Add_a_naming_rule;
    public static string RemoveAutomationText => ServicesVSResources.Remove_naming_rule;
    public static string SymbolSpecificationAutomationText => ServicesVSResources.Symbol_Specification;
    public static string NamingStyleAutomationText => ServicesVSResources.Naming_Style;
    public static string SeverityAutomationText => ServicesVSResources.Severity;

    private NamingStyleOptionPageViewModel _viewModel;
    private readonly string _languageName;
    private readonly INotificationService _notificationService;

    private readonly NotificationOptionViewModel[] _notifications =
    [
        new NotificationOptionViewModel(NotificationOption2.Silent, KnownMonikers.None),
        new NotificationOptionViewModel(NotificationOption2.Suggestion, KnownMonikers.StatusInformation),
        new NotificationOptionViewModel(NotificationOption2.Warning, KnownMonikers.StatusWarning),
        new NotificationOptionViewModel(NotificationOption2.Error, KnownMonikers.StatusError)
    ];

    internal NamingStyleOptionPageControl(OptionStore optionStore, INotificationService notificationService, string languageName)
        : base(optionStore)
    {
        _languageName = languageName;
        _notificationService = notificationService;

        InitializeComponent();
        OnLoad();
    }

    private NamingStyleOptionPageViewModel.NamingRuleViewModel CreateItemWithNoSelections()
    {
        return new NamingStyleOptionPageViewModel.NamingRuleViewModel()
        {
            Specifications = [.. _viewModel.Specifications],
            NamingStyles = [.. _viewModel.NamingStyles],
            NotificationPreferences = _notifications
        };
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
        => _viewModel.AddItem(CreateItemWithNoSelections());

    private void ManageSpecificationsButton_Click(object sender, RoutedEventArgs e)
    {
        var viewModel = new ManageSymbolSpecificationsDialogViewModel(_viewModel.Specifications, [.. _viewModel.CodeStyleItems], _languageName, _notificationService);
        var dialog = new ManageNamingStylesInfoDialog(viewModel);
        if (dialog.ShowModal().Value == true)
        {
            _viewModel.UpdateSpecificationList(viewModel);
        }
    }

    private void ManageStylesButton_Click(object sender, RoutedEventArgs e)
    {
        var viewModel = new ManageNamingStylesDialogViewModel(_viewModel.NamingStyles, [.. _viewModel.CodeStyleItems], _notificationService);
        var dialog = new ManageNamingStylesInfoDialog(viewModel);
        if (dialog.ShowModal().Value == true)
        {
            _viewModel.UpdateStyleList(viewModel);
        }
    }

    private void MoveUp_Click(object sender, EventArgs e)
    {
        var oldSelectedIndex = CodeStyleMembers.SelectedIndex;
        if (oldSelectedIndex > 0)
        {
            _viewModel.MoveItem(oldSelectedIndex, oldSelectedIndex - 1);
            CodeStyleMembers.SelectedIndex = oldSelectedIndex - 1;
            SetFocusToSelectedRow();
        }
    }

    private void MoveDown_Click(object sender, EventArgs e)
    {
        var oldSelectedIndex = CodeStyleMembers.SelectedIndex;
        if (oldSelectedIndex < CodeStyleMembers.Items.Count - 1)
        {
            _viewModel.MoveItem(oldSelectedIndex, oldSelectedIndex + 1);
            CodeStyleMembers.SelectedIndex = oldSelectedIndex + 1;
            SetFocusToSelectedRow();
        }
    }

    private void RemoveButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        var button = (Button)sender;
        var context = button.DataContext as NamingStyleOptionPageViewModel.NamingRuleViewModel;
        _viewModel.RemoveItem(context);
    }

    private void SetFocusToSelectedRow()
    {
        if (CodeStyleMembers.SelectedIndex >= 0)
        {
            if (CodeStyleMembers.ItemContainerGenerator.ContainerFromIndex(CodeStyleMembers.SelectedIndex) is not DataGridRow row)
            {
                CodeStyleMembers.ScrollIntoView(CodeStyleMembers.SelectedItem);
                row = CodeStyleMembers.ItemContainerGenerator.ContainerFromIndex(CodeStyleMembers.SelectedIndex) as DataGridRow;
            }

            if (row != null)
            {
                var cell = row.FindDescendant<DataGridCell>();
                cell?.Focus();
            }
        }
    }

    internal override void OnSave()
    {
        var symbolSpecifications = ArrayBuilder<SymbolSpecification>.GetInstance();
        var namingRules = ArrayBuilder<NamingRule>.GetInstance();
        var namingStyles = ArrayBuilder<NamingStyle>.GetInstance();

        foreach (var item in _viewModel.CodeStyleItems)
        {
            if (!item.IsComplete())
            {
                continue;
            }

            var rule = new NamingRule(
                item.SelectedSpecification,
                item.SelectedStyle.NamingStyle,
                item.SelectedNotificationPreference.Notification.Severity);

            namingRules.Add(rule);
        }

        foreach (var item in _viewModel.Specifications)
        {
            symbolSpecifications.Add(item);
        }

        foreach (var item in _viewModel.NamingStyles)
        {
            namingStyles.Add(item.NamingStyle);
        }

        var info = new NamingStylePreferences(
            symbolSpecifications.ToImmutableAndFree(),
            namingStyles.ToImmutableAndFree(),
            namingRules.ToImmutableAndFree());

        OptionStore.SetOption(NamingStyleOptions.NamingPreferences, _languageName, info);
    }

    internal override void OnLoad()
    {
        base.OnLoad();

        var preferences = OptionStore.GetOption<NamingStylePreferences>(NamingStyleOptions.NamingPreferences, _languageName);
        if (preferences == null)
        {
            return;
        }

        _viewModel = new NamingStyleOptionPageViewModel(preferences);
        DataContext = _viewModel;
    }

    internal bool ContainsErrors()
        => _viewModel.CodeStyleItems.Any(i => !i.IsComplete());

    private void CodeStyleMembers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        => _viewModel.SelectedIndex = CodeStyleMembers.SelectedIndex;
}
