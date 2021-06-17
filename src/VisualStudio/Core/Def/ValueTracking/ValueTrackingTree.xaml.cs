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

        private void ValueTrackingTreeView_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = e.Handled || e.Key switch
            {
                Key.Down => TrySelectItem(GetNextItem(expandNode: false), navigate: false),
                Key.Up => TrySelectItem(GetPreviousItem(), navigate: false),
                Key.F8 => e.KeyboardDevice.Modifiers == ModifierKeys.Shift ? TrySelectItem(GetPreviousItem(), navigate: true) : TrySelectItem(GetNextItem(expandNode: true), navigate: true),
                Key.Enter => TrySelectItem(ValueTrackingTreeView.SelectedItem as TreeViewItemBase, navigate: true),
                Key.Right => TrySetExpanded(ValueTrackingTreeView.SelectedItem as TreeViewItemBase, true),
                Key.Left => TrySetExpanded(ValueTrackingTreeView.SelectedItem as TreeViewItemBase, false),
                Key.Space => TryToggleExpanded(ValueTrackingTreeView.SelectedItem as TreeViewItemBase),
                _ => false
            };

            // Local Functions

            static bool TrySelectItem(TreeViewItemBase? node, bool navigate)
            {
                if (node is null)
                {
                    return false;
                }

                SelectItem(node, navigate);
                return true;
            }

            static bool TrySetExpanded(TreeViewItemBase? node, bool expanded)
            {
                if (node is null)
                {
                    return false;
                }

                node.IsNodeExpanded = expanded;
                return true;
            }

            static bool TryToggleExpanded(TreeViewItemBase? node)
                => TrySetExpanded(node, node is null ? false : !node.IsNodeExpanded);
        }

        private void ValueTrackingTreeView_MouseDoubleClickPreview(object sender, MouseButtonEventArgs e)
        {
            if (sender is TreeViewItemBase viewModel)
            {
                SelectItem(viewModel, true);
                e.Handled = true;
            }
        }

        private static void SelectItem(TreeViewItemBase? item, bool navigate = false)
        {
            if (item is null)
            {
                return;
            }

            item.IsNodeSelected = true;
            if (navigate && item is TreeItemViewModel navigatableItem)
            {
                navigatableItem.NavigateTo();
            }
        }

        private TreeViewItemBase GetNextItem(bool expandNode)
        {
            if (ValueTrackingTreeView.SelectedItem is null)
            {
                return (TreeViewItemBase)ValueTrackingTreeView.Items[0];
            }

            var item = (TreeViewItemBase)ValueTrackingTreeView.SelectedItem;

            if (expandNode)
            {
                item.IsNodeExpanded = true;
            }

            return item.GetNextInTree();
        }

        private TreeViewItemBase GetPreviousItem()
        {
            if (ValueTrackingTreeView.SelectedItem is null)
            {
                return (TreeViewItemBase)ValueTrackingTreeView.Items[ValueTrackingTreeView.Items.Count - 1];
            }

            var item = (TreeViewItemBase)ValueTrackingTreeView.SelectedItem;
            return item.GetPreviousInTree();
        }
    }
}
