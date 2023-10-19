// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Microsoft.CodeAnalysis.InlineRename.UI.SmartRename;

/// <summary>
/// Interaction logic for SmartRenameControl.xaml
/// </summary>
internal sealed partial class SmartRenameControl : UserControl
{
    private readonly SmartRenameViewModel _smartRenameViewModel;

    public SmartRenameControl(SmartRenameViewModel viewModel)
    {
        this.DataContext = _smartRenameViewModel = viewModel;
        InitializeComponent();
    }

    private void Suggestion_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 1)
        {
            var identifierName = ((FrameworkElement)sender).Tag.ToString();
            _smartRenameViewModel.SelectedSuggestedName = identifierName;
        }
    }
}
