// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows.Controls;
using Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.NamingStyle.ViewModel;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.NamingStyle.View;

/// <summary>
/// Interaction logic for NamingStylesLocationControl.xaml
/// </summary>
internal partial class NamingStylesLocationControl : UserControl
{
    public NamingStylesLocationControl(NamingStylesLocationViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
