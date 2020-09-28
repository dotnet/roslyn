// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CommonControls
{
    /// <summary>
    /// Interaction logic for NewTypeDestinationSelection.xaml
    /// </summary>
    internal partial class NewTypeDestinationSelection : UserControl
    {
        public NewTypeDestinationSelectionViewModel ViewModel { get; }
        public string GeneratedName => ServicesVSResources.Generated_name_colon;
        public string SelectDestinationFile => ServicesVSResources.Select_destination;
        public string SelectCurrentFileAsDestination => ServicesVSResources.Add_to_current_file;
        public string SelectNewFileAsDestination => ServicesVSResources.New_file_name_colon;
        public string NewTypeName => "New Type Name:";
        public NewTypeDestinationSelection(NewTypeDestinationSelectionViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = ViewModel;

            GotFocus += NewTypeDestinationSelection_GotFocus;
            InitializeComponent();
        }

        private void NewTypeDestinationSelection_GotFocus(object sender, RoutedEventArgs e)
        {
            TypeNameTextBox.Focus();
            TypeNameTextBox.SelectAll();
        }

        private void SelectAllInTextBox(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is TextBox textbox && Mouse.LeftButton == MouseButtonState.Released)
            {
                textbox.SelectAll();
            }
        }
    }
}
