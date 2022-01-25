// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Microsoft.VisualStudio.PlatformUI;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options.Style.NamingPreferences
{
    internal partial class SymbolSpecificationDialog : DialogWindow
    {
        private readonly SymbolSpecificationViewModel _viewModel;

        public string DialogTitle => ServicesVSResources.Symbol_Specification;
        public string SymbolSpecificationTitleLabelText => ServicesVSResources.Symbol_Specification_Title_colon;
        public string SymbolKindsLabelText => ServicesVSResources.Symbol_Kinds_can_match_any;
        public string AccessibilitiesLabelText => ServicesVSResources.Accessibilities_can_match_any;
        public string ModifiersLabelText => ServicesVSResources.Modifiers_must_match_all;
        public string SelectAllButtonText => ServicesVSResources.Select_All;
        public string DeselectAllButtonText => ServicesVSResources.Deselect_All;
        public string OK => ServicesVSResources.OK;
        public string Cancel => ServicesVSResources.Cancel;

        private readonly AutomationDelegatingListView symbolKindsListView;
        private readonly AutomationDelegatingListView accessibilitiesListView;
        private readonly AutomationDelegatingListView modifiersListView;

        internal SymbolSpecificationDialog(SymbolSpecificationViewModel viewModel)
        {
            _viewModel = viewModel;
            InitializeComponent();
            DataContext = viewModel;

            // AutomationDelegatingListView is defined in ServicesVisualStudio, which has
            // InternalsVisibleTo this project. But, the markup compiler doesn't consider the IVT 
            // relationship, so declaring the AutomationDelegatingListView in XAML would require 
            // duplicating that type in this project. Declaring and setting it here avoids the 
            // markup compiler completely, allowing us to reference the internal 
            // AutomationDelegatingListView without issue.

            symbolKindsListView = CreateAutomationDelegatingListView(nameof(SymbolSpecificationViewModel.SymbolKindList));
            symbolKindsContentControl.Content = symbolKindsListView;

            accessibilitiesListView = CreateAutomationDelegatingListView(nameof(SymbolSpecificationViewModel.AccessibilityList));
            accessibilitiesContentControl.Content = accessibilitiesListView;

            modifiersListView = CreateAutomationDelegatingListView(nameof(SymbolSpecificationViewModel.ModifierList));
            modifiersContentControl.Content = modifiersListView;

#pragma warning disable IDE0004 // Remove unnecessary cast - without the cast the delegate type would be Action<object, KeyEventArgs>.
            symbolKindsListView.AddHandler(PreviewKeyDownEvent, (KeyEventHandler)HandleSymbolKindsPreviewKeyDown, true);
            accessibilitiesListView.AddHandler(PreviewKeyDownEvent, (KeyEventHandler)HandleAccessibilitiesPreviewKeyDown, true);
            modifiersListView.AddHandler(PreviewKeyDownEvent, (KeyEventHandler)HandleModifiersPreviewKeyDown, true);
#pragma warning restore
        }

        private static AutomationDelegatingListView CreateAutomationDelegatingListView(string itemsSourceName)
        {
            var listView = new AutomationDelegatingListView();
            listView.SelectionMode = SelectionMode.Extended;
            listView.SetBinding(ItemsControl.ItemsSourceProperty, new Binding(itemsSourceName));
            listView.SetResourceReference(ItemsControl.ItemTemplateProperty, "listViewDataTemplate");
            return listView;
        }

        private void HandleSymbolKindsPreviewKeyDown(object sender, KeyEventArgs e)
            => HandlePreviewKeyDown(e, symbolKindsListView.SelectedItems.OfType<SymbolSpecificationViewModel.SymbolKindViewModel>());

        private void HandleAccessibilitiesPreviewKeyDown(object sender, KeyEventArgs e)
            => HandlePreviewKeyDown(e, accessibilitiesListView.SelectedItems.OfType<SymbolSpecificationViewModel.AccessibilityViewModel>());

        private void HandleModifiersPreviewKeyDown(object sender, KeyEventArgs e)
            => HandlePreviewKeyDown(e, modifiersListView.SelectedItems.OfType<SymbolSpecificationViewModel.ModifierViewModel>());

        private static void HandlePreviewKeyDown<T>(KeyEventArgs e, IEnumerable<T> selectedItems) where T : SymbolSpecificationViewModel.ISymbolSpecificationViewModelPart
        {
            if (e.Key == Key.Space)
            {
                e.Handled = true;

                var targetCheckedState = !selectedItems.All(d => d.IsChecked);
                foreach (var item in selectedItems)
                {
                    item.IsChecked = targetCheckedState;
                }
            }
        }

        private void SelectAllSymbolKinds(object sender, RoutedEventArgs e)
        {
            foreach (var item in symbolKindsListView.Items.OfType<SymbolSpecificationViewModel.SymbolKindViewModel>())
            {
                item.IsChecked = true;
            }
        }

        private void DeselectAllSymbolKinds(object sender, RoutedEventArgs e)
        {
            foreach (var item in symbolKindsListView.Items.OfType<SymbolSpecificationViewModel.SymbolKindViewModel>())
            {
                item.IsChecked = false;
            }
        }

        private void SelectAllAccessibilities(object sender, RoutedEventArgs e)
        {
            foreach (var item in accessibilitiesListView.Items.OfType<SymbolSpecificationViewModel.AccessibilityViewModel>())
            {
                item.IsChecked = true;
            }
        }

        private void DeselectAllAccessibilities(object sender, RoutedEventArgs e)
        {
            foreach (var item in accessibilitiesListView.Items.OfType<SymbolSpecificationViewModel.AccessibilityViewModel>())
            {
                item.IsChecked = false;
            }
        }

        private void SelectAllModifiers(object sender, RoutedEventArgs e)
        {
            foreach (var item in modifiersListView.Items.OfType<SymbolSpecificationViewModel.ModifierViewModel>())
            {
                item.IsChecked = true;
            }
        }

        private void DeselectAllModifiers(object sender, RoutedEventArgs e)
        {
            foreach (var item in modifiersListView.Items.OfType<SymbolSpecificationViewModel.ModifierViewModel>())
            {
                item.IsChecked = false;
            }
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.TrySubmit())
            {
                DialogResult = true;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
            => DialogResult = false;
    }
}
