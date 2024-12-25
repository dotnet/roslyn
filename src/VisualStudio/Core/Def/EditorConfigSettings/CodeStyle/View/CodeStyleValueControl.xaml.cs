// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows.Controls;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.CodeStyle.View;

/// <summary>
/// Interaction logic for CodeStyleValueControl.xaml
/// </summary>
internal partial class CodeStyleValueControl : UserControl
{
    private readonly CodeStyleValueViewModel _viewModel;

    public CodeStyleValueControl(CodeStyleValueViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        _viewModel = viewModel;
    }

    private void ValueComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        => _viewModel.SelectionChanged(ValueComboBox.SelectedIndex);
}
