// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows.Controls;
using Microsoft.VisualStudio.Text.Editor.SmartRename;

namespace Microsoft.CodeAnalysis.InlineRename.UI.SmartRename
{
    /// <summary>
    /// Interaction logic for SuggestedNamesControl.xaml
    /// </summary>
    internal partial class SuggestedNamesControl : UserControl
    {
        internal SuggestedNamesControl(SuggestedNamesControlViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
