// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows;
using System.Windows.Controls;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Formatting.View
{
    /// <summary>
    /// Interaction logic for WhitespaceValueSettingControl.xaml
    /// </summary>
    internal partial class FormattingBoolSettingView : UserControl
    {
        private readonly FormattingSetting _setting;
        private readonly CheckBox _checkBox;

        public FormattingBoolSettingView(FormattingSetting setting)
        {
            InitializeComponent();
            _setting = setting;
            _checkBox = new CheckBox();

            if (setting.GetValue() is bool value)
            {
                _checkBox.IsChecked = value;
            }

            _checkBox.Checked += CheckBoxChanged;
            _checkBox.Unchecked += CheckBoxChanged;

            _ = RootGrid.Children.Add(_checkBox);
        }

        private void CheckBoxChanged(object sender, RoutedEventArgs e)
        {
            _setting.SetValue(_checkBox.IsChecked == true);
        }
    }
}
