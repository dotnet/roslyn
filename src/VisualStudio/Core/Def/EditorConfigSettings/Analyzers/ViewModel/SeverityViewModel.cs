// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Analyzers.ViewModel
{
    internal class SeverityViewModel
    {
        private static readonly string[] s_severities = new[]
        {
            ServicesVSResources.Disabled,
            ServicesVSResources.Suggestion,
            ServicesVSResources.Warning,
            ServicesVSResources.Error
        };

        private readonly int _selectedSeverityIndex;

        private readonly AnalyzerSetting _setting;

        public string[] Severities => s_severities;

        private string? _selectedSeverityValue;

        public string SelectedSeverityValue
        {
            get
            {
                _selectedSeverityValue ??= Severities[_selectedSeverityIndex];

                return _selectedSeverityValue;
            }
            set => _selectedSeverityValue = value;
        }

        public bool IsConfigurable { get; private set; }

        public string ToolTip { get; private set; }

        public static string AutomationName => ServicesVSResources.Severity;

        public SeverityViewModel(AnalyzerSetting setting)
        {
            _selectedSeverityIndex = setting.Severity switch
            {
                DiagnosticSeverity.Hidden => 0,
                DiagnosticSeverity.Info => 1,
                DiagnosticSeverity.Warning => 2,
                DiagnosticSeverity.Error => 3,
                _ => throw new InvalidOperationException(),
            };

            IsConfigurable = !setting.IsNotConfigurable;

            ToolTip = IsConfigurable
                        ? ServicesVSResources.Severity
                        : ServicesVSResources.This_rule_is_not_configurable;

            _setting = setting;
        }

        public void SelectionChanged(int selectedIndex)
        {
            var severity = selectedIndex switch
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
