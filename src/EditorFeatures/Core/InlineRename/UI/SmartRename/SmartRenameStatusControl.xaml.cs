// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows.Controls;

namespace Microsoft.CodeAnalysis.InlineRename.UI.SmartRename;

/// <summary>
/// Interaction logic for SmartRenameStatusControl.xaml
/// </summary>
internal sealed partial class SmartRenameStatusControl : UserControl
{
    public SmartRenameStatusControl(SmartRenameViewModel viewModel)
    {
        this.DataContext = viewModel;
        InitializeComponent();
    }
}
