// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.LanguageServices.Implementation.MoveMembers.MainDialog;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.MoveMembers.Controls
{
    /// <summary>
    /// Interaction logic for MoveToAncestorTypeControl.xaml
    /// </summary>
    internal partial class MoveToAncestorTypeControl : UserControl
    {
        public MoveToAncestorTypeControlViewModel ViewModel { get; }
        public MoveToAncestorTypeControl(MoveToAncestorTypeControlViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = viewModel;
            InitializeComponent();
        }

        private void Destination_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (DestinationTreeView.SelectedItem is BaseTypeTreeNodeViewModel memberGraphNode)
            {
                ViewModel.SelectedDestination = memberGraphNode;
            }
        }
    }
}
