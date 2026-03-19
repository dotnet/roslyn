// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows.Controls;
using Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.CodeStyle.ViewModel;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.CodeStyle.View;

/// <summary>
/// Interaction logic for CodeStyleSeverityControl.xaml
/// </summary>
internal partial class CodeStyleSeverityControl : UserControl
{
    private readonly CodeStyleSeverityViewModel _viewModel;

    public CodeStyleSeverityControl(CodeStyleSeverityViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    private void SeverityComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        => _viewModel.SelectionChanged(SeverityComboBox.SelectedIndex);
}
