// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.NamingStyle.ViewModel;

internal sealed class NamingStylesSeverityViewModel
{
    private readonly NamingStyleSetting _setting;

    public NamingStylesSeverityViewModel(NamingStyleSetting setting)
    {
        _setting = setting;
        var selectedSeverityIndex = _setting.Severity switch
        {
            ReportDiagnostic.Suppress => 0,
            ReportDiagnostic.Hidden => 1,
            ReportDiagnostic.Info => 2,
            ReportDiagnostic.Warn => 3,
            ReportDiagnostic.Error => 4,
            _ => throw new InvalidOperationException(),
        };

        SelectedSeverityValue = Severities[selectedSeverityIndex];
    }

    internal void SelectionChanged(int selectedIndex)
    {
        var severity = selectedIndex switch
        {
            0 => ReportDiagnostic.Suppress,
            1 => ReportDiagnostic.Hidden,
            2 => ReportDiagnostic.Info,
            3 => ReportDiagnostic.Warn,
            4 => ReportDiagnostic.Error,
            _ => throw new InvalidOperationException(),
        };
        _setting.ChangeSeverity(severity);
    }

    public static string SeverityToolTip => ServicesVSResources.Severity;

    public static string SeverityAutomationName => ServicesVSResources.Severity;

    public string SelectedSeverityValue { get; set; }

    public static ImmutableArray<string> Severities { get; } =
        [
            ServicesVSResources.Disabled,
            WorkspacesResources.Refactoring_Only,
            WorkspacesResources.Suggestion,
            WorkspacesResources.Warning,
            WorkspacesResources.Error,
        ];
}
