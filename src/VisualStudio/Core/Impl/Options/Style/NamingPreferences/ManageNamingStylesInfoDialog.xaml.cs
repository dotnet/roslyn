// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Windows;
using System.Windows.Controls;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.VisualStudio.PlatformUI;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options.Style.NamingPreferences
{
    /// <summary>
    /// Interaction logic for NamingStyleDialog.xaml
    /// </summary>
    internal partial class ManageNamingStylesInfoDialog : DialogWindow
    {
        private readonly IManageNamingStylesInfoDialogViewModel _viewModel;

        public string OK => ServicesVSResources.OK;
        public string Cancel => ServicesVSResources.Cancel;
        public string CannotBeDeletedExplanation => ServicesVSResources.This_item_cannot_be_deleted_because_it_is_used_by_an_existing_Naming_Rule;
        public string AddItemAutomationText => ServicesVSResources.Add_item;
        public string EditButtonAutomationText => ServicesVSResources.Edit_item;
        public string RemoveButtonAutomationText => ServicesVSResources.Remove_item;

        internal ManageNamingStylesInfoDialog(IManageNamingStylesInfoDialogViewModel viewModel)
        {
            _viewModel = viewModel;

            InitializeComponent();
            DataContext = viewModel;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
            => _viewModel.AddItem();

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var item = button.DataContext as INamingStylesInfoDialogViewModel;
            _viewModel.RemoveItem(item);
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var item = button.DataContext as INamingStylesInfoDialogViewModel;
            _viewModel.EditItem(item);
        }

        private void OK_Click(object sender, RoutedEventArgs e)
            => DialogResult = true;

        private void Cancel_Click(object sender, RoutedEventArgs e)
            => DialogResult = false;
    }
}
