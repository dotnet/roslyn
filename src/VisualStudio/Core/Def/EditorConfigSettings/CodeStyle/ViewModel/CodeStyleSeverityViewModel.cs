// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.CodeStyle.ViewModel
{
    internal class CodeStyleSeverityViewModel
    {
        private static readonly string[] s_severities = new[]
        {
            ServicesVSResources.Disabled,
            ServicesVSResources.Suggestion,
            ServicesVSResources.Warning,
            ServicesVSResources.Error
        };

        private readonly int _selectedSeverityIndex;

        private string? _selectedSeverityValue;

        private readonly CodeStyleSetting _setting;

        public string[] Severities => s_severities;

        public string SelectedSeverityValue
        {
            get
            {
                _selectedSeverityValue ??= Severities[_selectedSeverityIndex];

                return _selectedSeverityValue;
            }
            set => _selectedSeverityValue = value;
        }

        public string ToolTip => ServicesVSResources.Severity;

        public static string AutomationName => ServicesVSResources.Severity;

        public CodeStyleSeverityViewModel(CodeStyleSetting setting)
        {
            _selectedSeverityIndex = setting.Severity switch
            {
                DiagnosticSeverity.Hidden => 0,
                DiagnosticSeverity.Info => 1,
                DiagnosticSeverity.Warning => 2,
                DiagnosticSeverity.Error => 3,
                _ => throw new InvalidOperationException(),
            };

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
