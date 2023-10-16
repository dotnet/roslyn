// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows.Controls;

namespace Microsoft.CodeAnalysis.InlineRename.UI.SmartRename
{
    /// <summary>
    /// Interaction logic for SmartRenameControl.xaml
    /// </summary>
    internal partial class SmartRenameControl : UserControl
    {
        internal SmartRenameControl(SmartRenameViewModel viewModel)
        {
            this.DataContext = viewModel;
            InitializeComponent();
        }
    }
}
