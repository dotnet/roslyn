// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace Microsoft.CodeAnalysis.Editor.InheritanceMargin.MarginGlyph
{
    internal partial class InheritanceMargin
    {
        public InheritanceMargin(SingleMemberMarginViewModel viewModel)
        {
            InitializeComponent();
            this.DataContext = viewModel;

            // This is created in the xaml file.
            var contextMenu = this.ContextMenu!;
            contextMenu.DataContext = viewModel;
            contextMenu.Style = (Style)FindResource("SingleMemberContextMenuStyle");
        }

        public InheritanceMargin(MultipleMembersMarginViewModel viewModel)
        {
            InitializeComponent();
            this.DataContext = viewModel;

            // This is created in the xaml file.
            var contextMenu = this.ContextMenu!;
            contextMenu.DataContext = viewModel;
            contextMenu.Style = (Style)FindResource("MultipleMembersContextMenuStyle");
        }

        private void Margin_OnClick(object sender, RoutedEventArgs e)
        {
            if (this.ContextMenu != null)
            {
                this.ContextMenu.IsOpen = true;
                e.Handled = true;
            }
        }

        private void MemberMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is MenuItem menuItem && menuItem.ContextMenu != null)
            {
                menuItem.IsSubmenuOpen = true;
                menuItem.StaysOpenOnClick = true;
                menuItem.ContextMenu.IsOpen = true;
                e.Handled = true;
            }
        }
    }
}

