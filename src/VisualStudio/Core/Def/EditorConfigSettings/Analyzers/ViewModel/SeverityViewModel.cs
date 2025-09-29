// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Analyzers.ViewModel;

internal sealed class SeverityViewModel
{
    private static readonly string[] s_severities =
    [
        ServicesVSResources.Disabled,
        WorkspacesResources.Refactoring_Only,
        WorkspacesResources.Suggestion,
        WorkspacesResources.Warning,
        WorkspacesResources.Error
    ];

    private readonly int _selectedSeverityIndex;

    private readonly AnalyzerSetting _setting;

    public string[] Severities => s_severities;

    public string SelectedSeverityValue
    {
        get
        {
            field ??= Severities[_selectedSeverityIndex];

            return field;
        }
        set;
    }

    public bool IsConfigurable { get; private set; }

    public string ToolTip { get; private set; }

    public static string AutomationName => ServicesVSResources.Severity;

    public SeverityViewModel(AnalyzerSetting setting)
    {
        _selectedSeverityIndex = setting.Severity switch
        {
            ReportDiagnostic.Suppress => 0,
            ReportDiagnostic.Hidden => 1,
            ReportDiagnostic.Info => 2,
            ReportDiagnostic.Warn => 3,
            ReportDiagnostic.Error => 4,
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
            0 => ReportDiagnostic.Suppress,
            1 => ReportDiagnostic.Hidden,
            2 => ReportDiagnostic.Info,
            3 => ReportDiagnostic.Warn,
            4 => ReportDiagnostic.Error,
            _ => throw new InvalidOperationException(),
        };

        if (_setting.Severity != severity)
        {
            _setting.ChangeSeverity(severity);
        }
    }
}
