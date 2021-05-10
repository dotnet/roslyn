// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Microsoft.VisualStudio.LanguageServices.ValueTracking
{
    /// <summary>
    /// Interaction logic for ValueTrackingTree.xaml
    /// </summary>
    internal partial class ValueTrackingTree : UserControl
    {
        private readonly ValueTrackingTreeViewModel _viewModel;

        public ValueTrackingTree(ValueTrackingTreeViewModel viewModel)
        {
            DataContext = _viewModel = viewModel;
            InitializeComponent();
        }

        private void ValueTrackingTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            _viewModel.SelectedItem = (TreeItemViewModel)e.NewValue;
        }
    }
}
