// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Windows.Automation;
using System.Windows.Controls;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.CodeStyle.View
{
    /// <summary>
    /// Interaction logic for CodeStyleValueControl.xaml
    /// </summary>
    internal partial class CodeStyleValueControl : UserControl
    {
        private readonly ComboBox _comboBox;
        private readonly CodeStyleSetting _setting;

        public CodeStyleValueControl(CodeStyleSetting setting)
        {
            InitializeComponent();
            var values = setting.GetValues().ToList();
            var index = values.IndexOf(setting.GetCurrentValue());
            _comboBox = new ComboBox()
            {
                ItemsSource = values
            };
            _comboBox.SelectedIndex = index;
            _comboBox.SetValue(AutomationProperties.NameProperty, ServicesVSResources.Value);
            _comboBox.SelectionChanged += ComboBox_SelectionChanged;
            _ = RootGrid.Children.Add(_comboBox);
            _setting = setting;
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _setting.ChangeValue(_comboBox.SelectedIndex);
        }
    }
}
