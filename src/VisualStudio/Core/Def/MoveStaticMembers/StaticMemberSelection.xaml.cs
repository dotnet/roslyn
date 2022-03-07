// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows;
using System.Windows.Controls;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.MoveStaticMembers
{
    /// <summary>
    /// Interaction logic for StaticMemberSelection.xaml
    /// </summary>
    internal partial class StaticMemberSelection : UserControl
    {

        public string SelectDependents => ServicesVSResources.Select_Dependents;
        public string MembersHeader => ServicesVSResources.Members;
        public string SelectAll => ServicesVSResources.Select_All;
        public string DeselectAll => ServicesVSResources.Deselect_All;

        internal StaticMemberSelectionViewModel ViewModel { get; }

        internal StaticMemberSelection(StaticMemberSelectionViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = ViewModel;
            InitializeComponent();
        }

        private void SelectDependentsButton_Click(object sender, RoutedEventArgs e)
            => ViewModel.SelectDependents();

        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
            => ViewModel.SelectAll();

        private void DeselectAllButton_Click(object sender, RoutedEventArgs e)
            => ViewModel.DeselectAll();
    }
}
