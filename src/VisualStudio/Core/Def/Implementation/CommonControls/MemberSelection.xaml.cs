// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows;
using System.Windows.Controls;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CommonControls
{
    /// <summary>
    /// Interaction logic for MemberSelection.xaml
    /// </summary>
    internal partial class MemberSelection : UserControl
    {
        public string SelectDependents => ServicesVSResources.Select_Dependents;
        public string SelectPublic => ServicesVSResources.Select_Public;
        public string MembersHeader => ServicesVSResources.Members;
        public string MakeAbstractHeader => ServicesVSResources.Make_abstract;

        public MemberSelectionViewModel ViewModel { get; }

        public MemberSelection(MemberSelectionViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = ViewModel;

            InitializeComponent();
        }

        private void SelectDependentsButton_Click(object sender, RoutedEventArgs e)
            => ViewModel.SelectDependents();

        private void SelectPublic_Click(object sender, RoutedEventArgs e)
            => ViewModel.SelectPublic();

        private void SelectAllCheckBox_Checked(object sender, RoutedEventArgs e)
            => ViewModel.SelectAll();

        private void SelectAllCheckBox_Unchecked(object sender, RoutedEventArgs e)
            => ViewModel.DeselectAll();

        private void MemberSelectionCheckBox_Checked(object sender, RoutedEventArgs e)
            => ViewModel.UpdateSelectAllCheckBoxState();

        private void MemberSelectionCheckBox_Unchecked(object sender, RoutedEventArgs e)
            => ViewModel.UpdateSelectAllCheckBoxState();
    }
}
