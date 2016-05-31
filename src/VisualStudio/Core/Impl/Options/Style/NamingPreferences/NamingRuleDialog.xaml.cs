// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Windows;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.VisualStudio.PlatformUI;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options.Style.NamingPreferences
{
    /// <summary>
    /// Interaction logic for NamingRuleDialog.xaml
    /// </summary>
    internal partial class NamingRuleDialog : DialogWindow
    {
        private readonly NamingRuleDialogViewModel _viewModel;
        private readonly NamingStylesOptionPageControlViewModel _namingStylesViewModel;
        private readonly INotificationService _notificationService;
        private readonly ImmutableArray<string> _categories;

        public string DialogTitle => ServicesVSResources.NamingRuleDialogTitle;
        public string NameLabelText => ServicesVSResources.NameEntryLabel;
        public string SymbolSpecificationLabelText => ServicesVSResources.SymbolSpecificationEntryLabel;
        public string NamingStyleLabelText => ServicesVSResources.NamingStyleEntryLabel;
        public string ParentRuleLabelText => ServicesVSResources.ParentRuleEntryLabel;
        public string EnforcementLevelsLabelText => ServicesVSResources.EnforcementLevelEntryLabel;
        public string OK => ServicesVSResources.OK;
        public string Cancel => ServicesVSResources.Cancel;

        internal NamingRuleDialog(NamingRuleDialogViewModel viewModel, NamingStylesOptionPageControlViewModel outerViewModel, ImmutableArray<string> categories, INotificationService notificationService)
        {
            _notificationService = notificationService;
            _categories = categories;

            _viewModel = viewModel;
            _namingStylesViewModel = outerViewModel;

            InitializeComponent();
            DataContext = viewModel;
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.TrySubmit())
            {
                DialogResult = true;
            }
        }

        private void CreateSymbolSpecification(object sender, RoutedEventArgs e)
        {
            var newSymbolSpecificationViewModel = new SymbolSpecificationViewModel(_namingStylesViewModel.LanguageName, _categories, _notificationService);
            var dialog = new SymbolSpecificationDialog(newSymbolSpecificationViewModel);
            var result = dialog.ShowModal();
            if (result == true)
            {
                _namingStylesViewModel.AddSymbolSpecification(newSymbolSpecificationViewModel);
                _viewModel.SelectedSymbolSpecificationIndex = _viewModel.SymbolSpecificationList.IndexOf(newSymbolSpecificationViewModel);
            }
        }

        private void ConfigureSymbolSpecifications(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedSymbolSpecificationIndex >= 0)
            {
                var symbolSpecificationViewModel = _viewModel.SymbolSpecificationList.GetItemAt(_viewModel.SelectedSymbolSpecificationIndex) as SymbolSpecificationViewModel;
                var symbolSpecificationClone = new SymbolSpecificationViewModel(_namingStylesViewModel.LanguageName, _categories, symbolSpecificationViewModel.GetSymbolSpecification(), _notificationService);

                var dialog = new SymbolSpecificationDialog(symbolSpecificationClone);
                var result = dialog.ShowModal();
                if (result == true)
                {
                    symbolSpecificationViewModel.ModifierList = symbolSpecificationClone.ModifierList;
                    symbolSpecificationViewModel.SymbolKindList = symbolSpecificationClone.SymbolKindList;
                    symbolSpecificationViewModel.AccessibilityList = symbolSpecificationClone.AccessibilityList;
                    symbolSpecificationViewModel.SymbolSpecName = symbolSpecificationClone.SymbolSpecName;
                    symbolSpecificationViewModel.CustomTagList = symbolSpecificationClone.CustomTagList;
                }
            }
        }

        private void CreateNamingStyle(object sender, RoutedEventArgs e)
        {
            var newNamingStyleViewModel = new NamingStyleViewModel(new NamingStyle(), _notificationService);
            var dialog = new NamingStyleDialog(newNamingStyleViewModel);
            var result = dialog.ShowModal();
            if (result == true)
            {
                _namingStylesViewModel.AddNamingStyle(newNamingStyleViewModel);
                _viewModel.NamingStyleIndex = _viewModel.NamingStyleList.IndexOf(newNamingStyleViewModel);
            }
        }

        private void ConfigureNamingStyles(object sender, RoutedEventArgs e)
        {
            if (_viewModel.NamingStyleIndex >= 0)
            {
                var namingStyleMutable = _viewModel.NamingStyleList.GetItemAt(_viewModel.NamingStyleIndex) as NamingStyleViewModel;

                var style = namingStyleMutable.GetNamingStyle();
                var styleClone = style.Clone(); 

                var namingStyleClone = new NamingStyleViewModel(styleClone, _notificationService);

                var dialog = new NamingStyleDialog(namingStyleClone);
                var result = dialog.ShowModal();
                if (result == true)
                {
                    namingStyleMutable.NamingConventionName = namingStyleClone.NamingConventionName;
                    namingStyleMutable.RequiredPrefix = namingStyleClone.RequiredPrefix;
                    namingStyleMutable.RequiredSuffix = namingStyleClone.RequiredSuffix;
                    namingStyleMutable.WordSeparator = namingStyleClone.WordSeparator;
                    namingStyleMutable.FirstWordGroupCapitalization = namingStyleClone.FirstWordGroupCapitalization;
                }
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
