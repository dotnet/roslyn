// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows.Controls;
using Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.NamingStyle.ViewModel;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.NamingStyle.View;

/// <summary>
/// Interaction logic for NamingStylesSeverityControl.xaml
/// </summary>
internal partial class NamingStylesSeverityControl : UserControl
{
    private readonly NamingStylesSeverityViewModel _viewModel;

    public NamingStylesSeverityControl(NamingStylesSeverityViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    private void SeverityComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        => _viewModel.SelectionChanged(SeverityComboBox.SelectedIndex);
}
