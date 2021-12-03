// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows.Automation;
using System.Windows.Controls;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.CodeStyle.View
{
    /// <summary>
    /// Interaction logic for CodeStyleSeverityControl.xaml
    /// </summary>
    internal partial class CodeStyleSeverityControl : UserControl
    {
        private readonly ComboBox _comboBox;
        private readonly CodeStyleSetting _setting;

        public CodeStyleSeverityControl(CodeStyleSetting setting)
        {
            InitializeComponent();
            _setting = setting;
            _comboBox = new ComboBox()
            {
                ItemsSource = new[]
                {
                    ServicesVSResources.Refactoring_Only,
                    ServicesVSResources.Suggestion,
                    ServicesVSResources.Warning,
                    ServicesVSResources.Error
                }
            };

            _comboBox.SelectedIndex = setting.Severity switch
            {
                DiagnosticSeverity.Hidden => 0,
                DiagnosticSeverity.Info => 1,
                DiagnosticSeverity.Warning => 2,
                DiagnosticSeverity.Error => 3,
                _ => throw new InvalidOperationException(),
            };

            _comboBox.SelectionChanged += ComboBox_SelectionChanged;
            _comboBox.SetValue(AutomationProperties.NameProperty, ServicesVSResources.Severity);
            _ = RootGrid.Children.Add(_comboBox);
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var severity = _comboBox.SelectedIndex switch
            {
                0 => DiagnosticSeverity.Hidden,
                1 => DiagnosticSeverity.Info,
                2 => DiagnosticSeverity.Warning,
                3 => DiagnosticSeverity.Error,
                _ => throw new InvalidOperationException(),
            };

            if (_setting.Severity != severity)
            {
                _setting.ChangeSeverity(severity);
            }
        }
    }
}
