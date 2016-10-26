// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options.Style.NamingPreferences;
using Microsoft.VisualStudio.PlatformUI;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options.Style
{
    internal partial class NamingStyleOptionPageControl : AbstractOptionPageControl
    {
        private NamingStyleOptionPageViewModel _viewModel;
        private readonly string _languageName;
        private readonly INotificationService _notificationService;

        private readonly ImmutableArray<EnforcementLevel> _notifications = ImmutableArray.Create(
            new EnforcementLevel(DiagnosticSeverity.Hidden),
            new EnforcementLevel(DiagnosticSeverity.Info),
            new EnforcementLevel(DiagnosticSeverity.Warning),
            new EnforcementLevel(DiagnosticSeverity.Error));

        internal NamingStyleOptionPageControl(IServiceProvider serviceProvider, INotificationService notificationService, string languageName)
            : base(serviceProvider)
        {
            _languageName = languageName;
            _notificationService = notificationService;

            InitializeComponent();
            LoadSettings();
        }

        private NamingStyleOptionPageViewModel.NamingRuleViewModel CreateItemWithNoSelections()
        {
            return new NamingStyleOptionPageViewModel.NamingRuleViewModel()
            {
                Specifications = new ObservableCollection<SymbolSpecification>(_viewModel.Specifications),
                NamingStyles = new ObservableCollection<NamingStyle>(_viewModel.NamingStyles),
                NotificationPreferences = _notifications
            };
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.AddItem(CreateItemWithNoSelections());
        }

        private void ManageSpecificationsButton_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = new ManageSymbolSpecificationsDialogViewModel(_viewModel.Specifications, _viewModel.CodeStyleItems.ToList(), _languageName, _notificationService);
            var dialog = new ManageNamingStylesInfoDialog(viewModel);
            if (dialog.ShowDialog().Value == true)
            {
                _viewModel.UpdateSpecificationList(viewModel);
            }
        }

        private void ManageStylesButton_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = new ManageNamingStylesDialogViewModel(_viewModel.NamingStyles, _viewModel.CodeStyleItems.ToList(), _notificationService);
            var dialog = new ManageNamingStylesInfoDialog(viewModel);
            if (dialog.ShowDialog().Value == true)
            {
                _viewModel.UpdateStyleList(viewModel);
            }
        }

        private void MoveUp_Click(object sender, EventArgs e)
        {
            int oldSelectedIndex = CodeStyleMembers.SelectedIndex;
            if (oldSelectedIndex > 0)
            {
                _viewModel.MoveItem(oldSelectedIndex, oldSelectedIndex - 1);
                CodeStyleMembers.SelectedIndex = oldSelectedIndex - 1;
                SetFocusToSelectedRow();
            }
        }

        private void MoveDown_Click(object sender, EventArgs e)
        {
            int oldSelectedIndex = CodeStyleMembers.SelectedIndex;
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
                DataGridRow row = CodeStyleMembers.ItemContainerGenerator.ContainerFromIndex(CodeStyleMembers.SelectedIndex) as DataGridRow;
                if (row == null)
                {
                    CodeStyleMembers.ScrollIntoView(CodeStyleMembers.SelectedItem);
                    row = CodeStyleMembers.ItemContainerGenerator.ContainerFromIndex(CodeStyleMembers.SelectedIndex) as DataGridRow;
                }

                if (row != null)
                {
                    DataGridCell cell = row.FindDescendant<DataGridCell>();
                    if (cell != null)
                    {
                        cell.Focus();
                    }
                }
            }
        }

        internal override void SaveSettings()
        {
            var info = new SerializableNamingStylePreferencesInfo();

            foreach (var item in _viewModel.CodeStyleItems)
            {
                if (!item.IsComplete())
                {
                    continue;
                }

                var rule = new SerializableNamingRule()
                {
                    EnforcementLevel = item.SelectedNotificationPreference.Value,
                    NamingStyleID = item.SelectedStyle.ID,
                    SymbolSpecificationID = item.SelectedSpecification.ID
                };

                info.NamingRules.Add(rule);
            }

            foreach (var item in _viewModel.Specifications)
            {
                info.SymbolSpecifications.Add(item);
            }

            foreach (var item in _viewModel.NamingStyles)
            {
                info.NamingStyles.Add(item);
            }

            var oldOptions = OptionService.GetOptions();
            var newOptions = oldOptions.WithChangedOption(SimplificationOptions.NamingPreferences, _languageName, info.CreateXElement().ToString());
            OptionService.SetOptions(newOptions);
            OptionLogger.Log(oldOptions, newOptions);
        }

        internal override void LoadSettings()
        {
            base.LoadSettings();

            var options = OptionService.GetOption(SimplificationOptions.NamingPreferences, _languageName);
            if (string.IsNullOrEmpty(options))
            {
                return;
            }

            var namingPreferencesXml = this.OptionService.GetOption(SimplificationOptions.NamingPreferences, _languageName);
            var preferencesInfo = SerializableNamingStylePreferencesInfo.FromXElement(XElement.Parse(namingPreferencesXml));
            _viewModel = new NamingStyleOptionPageViewModel(preferencesInfo);
            this.DataContext = _viewModel;
        }

        internal bool ContainsErrors()
        {
            return _viewModel.CodeStyleItems.Any(i => !i.IsComplete());
        }

        private void CodeStyleMembers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _viewModel.SelectedIndex = CodeStyleMembers.SelectedIndex;
        }
    }
}