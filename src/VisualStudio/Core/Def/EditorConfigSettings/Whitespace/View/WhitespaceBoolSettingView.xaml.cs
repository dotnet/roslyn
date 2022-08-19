// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Whitespace.View
{
    /// <summary>
    /// Interaction logic for WhitespaceValueSettingControl.xaml
    /// </summary>
    internal partial class WhitespaceBoolSettingView : UserControl
    {
        private readonly WhitespaceSetting _setting;

        public WhitespaceBoolSettingView(WhitespaceSetting setting)
        {
            InitializeComponent();
            RootCheckBox.SetValue(AutomationProperties.NameProperty, ServicesVSResources.Value);
            _setting = setting;

            if (setting.GetValue() is bool value)
            {
                RootCheckBox.IsChecked = value;
            }

            RootCheckBox.Checked += CheckBoxChanged;
            RootCheckBox.Unchecked += CheckBoxChanged;
        }

        private void CheckBoxChanged(object sender, RoutedEventArgs e)
        {
            var value = RootCheckBox.IsChecked == true;
            if (_setting.GetValue() is bool currentValue &&
                value != currentValue)
            {
                _setting.SetValue(value);
            }
        }
    }
}
