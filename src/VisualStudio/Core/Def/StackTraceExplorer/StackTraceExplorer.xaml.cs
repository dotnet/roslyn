// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.CodeAnalysis.StackTraceExplorer;

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
            var text = Clipboard.GetText();
            _viewModel.OnPaste_CallOnUIThread(text);
        }

        public Task OnAnalysisResultAsync(StackTraceAnalysisResult result, CancellationToken cancellationToken)
            => _viewModel.SetStackTraceResultAsync(result, cancellationToken);

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
