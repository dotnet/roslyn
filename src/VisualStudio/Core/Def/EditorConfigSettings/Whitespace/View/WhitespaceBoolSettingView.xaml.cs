// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows.Controls;

namespace Microsoft.CodeAnalysis.EditorConfigSettings
{
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
}
