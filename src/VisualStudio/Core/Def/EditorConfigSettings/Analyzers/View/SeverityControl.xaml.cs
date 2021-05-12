// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Analyzers.View
{
    /// <summary>
    /// Interaction logic for SeverityControl.xaml
    /// </summary>
    internal partial class SeverityControl : UserControl
    {
        private readonly ComboBox _comboBox;
        private readonly AnalyzerSetting _setting;

        public SeverityControl(AnalyzerSetting setting)
        {
            InitializeComponent();
            _comboBox = new ComboBox()
            {
                ItemsSource = new[]
                {
                    ServicesVSResources.Disabled,
                    ServicesVSResources.Suggestion,
                    ServicesVSResources.Warning,
                    ServicesVSResources.Error
                }
            };

            switch (setting.Severity)
            {
                case DiagnosticSeverity.Hidden:
                    _comboBox.SelectedIndex = 0;
                    break;
                case DiagnosticSeverity.Info:
                    _comboBox.SelectedIndex = 1;
                    break;
                case DiagnosticSeverity.Warning:
                    _comboBox.SelectedIndex = 2;
                    break;
                case DiagnosticSeverity.Error:
                    _comboBox.SelectedIndex = 3;
                    break;
                default:
                    break;
            }

            _comboBox.SelectionChanged += ComboBox_SelectionChanged;
            _comboBox.SetValue(AutomationProperties.NameProperty, ServicesVSResources.Severity);
            _ = RootGrid.Children.Add(_comboBox);
            _setting = setting;
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
