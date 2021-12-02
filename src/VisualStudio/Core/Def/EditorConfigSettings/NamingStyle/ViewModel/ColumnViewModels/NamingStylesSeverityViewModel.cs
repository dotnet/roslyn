// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.NamingStyle.ViewModel
{
    internal class NamingStylesSeverityViewModel
    {
        private readonly NamingStyleSetting _setting;

        public NamingStylesSeverityViewModel(NamingStyleSetting setting)
        {
            _setting = setting;
            var selectedSeverityIndex = _setting.Severity switch
            {
                ReportDiagnostic.Hidden => 0,
                ReportDiagnostic.Info => 1,
                ReportDiagnostic.Warn => 2,
                ReportDiagnostic.Error => 3,
                _ => throw new InvalidOperationException(),
            };

            SelectedSeverityValue = Severities[selectedSeverityIndex];
        }

        internal void SelectionChanged(int selectedIndex)
        {
            var severity = selectedIndex switch
            {
                0 => ReportDiagnostic.Hidden,
                1 => ReportDiagnostic.Info,
                2 => ReportDiagnostic.Warn,
                3 => ReportDiagnostic.Error,
                _ => throw new InvalidOperationException(),
            };
            _setting.ChangeSeverity(severity);
        }

        public static string SeverityToolTip => ServicesVSResources.Severity;

        public static string SeverityAutomationName => ServicesVSResources.Severity;

        public string SelectedSeverityValue { get; set; }

        public static ImmutableArray<string> Severities { get; } =
            ImmutableArray.Create(
                ServicesVSResources.Disabled,
                ServicesVSResources.Suggestion,
                ServicesVSResources.Warning,
                ServicesVSResources.Error
            );
    }
}
