// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Controls;

namespace Microsoft.VisualStudio.LanguageServices.StackTraceExplorer
{
    /// <summary>
    /// Interaction logic for CallstackExplorer.xaml
    /// </summary>
    internal partial class StackTraceExplorer : UserControl
    {
        private readonly StackTraceExplorerViewModel _viewModel;

        public StackTraceExplorer(StackTraceExplorerViewModel viewModel)
        {
            DataContext = _viewModel = viewModel;
            InitializeComponent();

            DataObject.AddPastingHandler(this, OnPaste);
        }

        private void OnPaste(object sender, DataObjectPastingEventArgs e)
            => OnPaste();

        private void CommandBinding_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
            => OnPaste();

        public void OnPaste()
        {
            _viewModel.OnPaste();
        }

        private void ListViewItem_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_viewModel.Selection is StackFrameViewModel stackFrameViewModel)
            {
                stackFrameViewModel.NavigateToSymbol();
            }
        }

        internal void OnClear()
        {
            _viewModel.OnClear();
        }
    }
}
