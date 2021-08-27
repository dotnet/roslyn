// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows;
using System.Windows.Controls;

namespace Microsoft.VisualStudio.LanguageServices.CallstackExplorer
{
    /// <summary>
    /// Interaction logic for CallstackExplorer.xaml
    /// </summary>
    internal partial class CallstackExplorer : UserControl
    {
        private readonly CallstackExplorerViewModel _viewModel;

        public CallstackExplorer(CallstackExplorerViewModel viewModel)
        {
            DataContext = _viewModel = viewModel;
            InitializeComponent();

            DataObject.AddPastingHandler(this, OnPaste);
        }

        private void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            var textObject = e.SourceDataObject.GetData(DataFormats.Text);

            if (textObject is string text)
            {
                _viewModel.OnPaste(text);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var textObject = Clipboard.GetData(DataFormats.Text);

            if (textObject is string text)
            {
                _viewModel.OnPaste(text);
            }
        }
    }
}
