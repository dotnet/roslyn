// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
        private readonly CodeStyleSetting _setting;

        // internal resource string cannot be statically bound in WPF 
        public static string Severity => ServicesVSResources.Severity;
        public static string Refactoring_Only => ServicesVSResources.Refactoring_Only;
        public static string Suggestion => ServicesVSResources.Suggestion;
        public static string Warning => ServicesVSResources.Warning;
        public static string Error => ServicesVSResources.Error;

        public CodeStyleSeverityControl(CodeStyleSetting setting)
        {
            InitializeComponent();
            _setting = setting;
            SeverityComboBox.SelectedIndex = setting.Severity switch
            {
                DiagnosticSeverity.Hidden => 0,
                DiagnosticSeverity.Info => 1,
                DiagnosticSeverity.Warning => 2,
                DiagnosticSeverity.Error => 3,
                _ => throw new InvalidOperationException(),
            };
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var severity = SeverityComboBox.SelectedIndex switch
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
