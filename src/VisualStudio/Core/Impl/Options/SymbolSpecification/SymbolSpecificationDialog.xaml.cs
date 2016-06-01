// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.VisualStudio.PlatformUI;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    internal partial class SymbolSpecificationDialog : DialogWindow
    {
        private readonly SymbolSpecificationViewModel _viewModel;

        public string DialogTitle => ServicesVSResources.SymbolSpecificationDialogTitle;
        public string SymbolSpecificationTitleLabelText => ServicesVSResources.SymbolSpecificationTitleLabel;
        public string SymbolKindsLabelText => ServicesVSResources.SymbolKindsLabel;
        public string AccessibilitiesLabelText => ServicesVSResources.AccessibilitiesLabel;
        public string CustomTagsLabelText => ServicesVSResources.CustomTagsLabel;
        public string ModifiersLabelText => ServicesVSResources.ModifiersLabel;
        public string SelectAllButtonText => ServicesVSResources.SelectAll;
        public string DeselectAllButtonText => ServicesVSResources.DeselectAll;
        public string OK => ServicesVSResources.OK;
        public string Cancel => ServicesVSResources.Cancel;

        internal SymbolSpecificationDialog(SymbolSpecificationViewModel viewModel)
        {
            _viewModel = viewModel;
            InitializeComponent();
            DataContext = viewModel;

            SymbolKinds.AddHandler(UIElement.PreviewKeyDownEvent, (KeyEventHandler)HandleSymbolKindsPreviewKeyDown, true);
            Accessibilities.AddHandler(UIElement.PreviewKeyDownEvent, (KeyEventHandler)HandleAccessibilitiesPreviewKeyDown, true);
            Modifiers.AddHandler(UIElement.PreviewKeyDownEvent, (KeyEventHandler)HandleModifiersPreviewKeyDown, true);
        }

        private void HandleSymbolKindsPreviewKeyDown(object sender, KeyEventArgs e)
        {
            HandlePreviewKeyDown(e, SymbolKinds.SelectedItems.OfType<SymbolSpecificationViewModel.SymbolKindViewModel>());
        }

        private void HandleAccessibilitiesPreviewKeyDown(object sender, KeyEventArgs e)
        {
            HandlePreviewKeyDown(e, Accessibilities.SelectedItems.OfType<SymbolSpecificationViewModel.AccessibilityViewModel>());
        }

        private void HandleModifiersPreviewKeyDown(object sender, KeyEventArgs e)
        {
            HandlePreviewKeyDown(e, Modifiers.SelectedItems.OfType<SymbolSpecificationViewModel.ModifierViewModel>());
        }

        private void HandlePreviewKeyDown<T>(KeyEventArgs e, IEnumerable<T> selectedItems) where T : SymbolSpecificationViewModel.ISymbolSpecificationViewModelPart
        {
            if (e.Key == Key.Space)
            {
                e.Handled = true;

                bool targetCheckedState = !selectedItems.All(d => d.IsChecked);
                foreach (var item in selectedItems)
                {
                    item.IsChecked = targetCheckedState;
                }
            }
        }

        private void SelectAllSymbolKinds(object sender, RoutedEventArgs e)
        {
            foreach (var item in SymbolKinds.Items.OfType<SymbolSpecificationViewModel.SymbolKindViewModel>())
            {
                item.IsChecked = true;
            }
        }

        private void DeselectAllSymbolKinds(object sender, RoutedEventArgs e)
        {
            foreach (var item in SymbolKinds.Items.OfType<SymbolSpecificationViewModel.SymbolKindViewModel>())
            {
                item.IsChecked = false;
            }
        }

        private void SelectAllAccessibilities(object sender, RoutedEventArgs e)
        {
            foreach (var item in Accessibilities.Items.OfType<SymbolSpecificationViewModel.AccessibilityViewModel>())
            {
                item.IsChecked = true;
            }
        }

        private void DeselectAllAccessibilities(object sender, RoutedEventArgs e)
        {
            foreach (var item in Accessibilities.Items.OfType<SymbolSpecificationViewModel.AccessibilityViewModel>())
            {
                item.IsChecked = false;
            }
        }

        private void SelectAllModifiers(object sender, RoutedEventArgs e)
        {
            foreach (var item in Modifiers.Items.OfType<SymbolSpecificationViewModel.ModifierViewModel>())
            {
                item.IsChecked = true;
            }
        }

        private void DeselectAllModifiers(object sender, RoutedEventArgs e)
        {
            foreach (var item in Modifiers.Items.OfType<SymbolSpecificationViewModel.ModifierViewModel>())
            {
                item.IsChecked = false;
            }
        }

        private void SelectAllCustomTags(object sender, RoutedEventArgs e)
        {
            foreach (var item in CustomTags.Items.OfType<SymbolSpecificationViewModel.CustomTagViewModel>())
            {
                item.IsChecked = true;
            }
        }

        private void DeselectAllCustomTags(object sender, RoutedEventArgs e)
        {
            foreach (var item in CustomTags.Items.OfType<SymbolSpecificationViewModel.CustomTagViewModel>())
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
        {
            DialogResult = false;
        }
    }
}
