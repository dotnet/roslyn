// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows.Controls;
using Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Whitespace.ViewModel;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Whitespace.View;

/// <summary>
/// Interaction logic for WhitespaceValueSettingControl.xaml
/// </summary>
internal partial class WhitespaceBoolSettingView : UserControl
{
    public WhitespaceBoolSettingView(WhitespaceSettingBoolViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
