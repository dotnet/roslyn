// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows.Controls;
using Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Analyzers.ViewModel;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Analyzers.View
{
    /// <summary>
    /// Interaction logic for SeverityControl.xaml
    /// </summary>
    internal partial class SeverityControl : UserControl
    {
        private readonly SeverityViewModel _viewModel;

        public SeverityControl(SeverityViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            _viewModel = viewModel;
        }

        private void SeverityComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
            => _viewModel.SelectionChanged(SeverityComboBox.SelectedIndex);
    }
}
