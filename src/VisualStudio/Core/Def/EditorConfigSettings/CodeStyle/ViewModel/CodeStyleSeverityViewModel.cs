// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.CodeStyle.ViewModel;

internal sealed class CodeStyleSeverityViewModel
{
    // NOTE: 'ServicesVSResources.Disabled' severity is not supported for code style settings.
    //       Code styles can instead be disabled by setting the option value that turns off the style.
    //       Theoretically, we can support the 'Disabled' severity, which translates to 'none' value in
    //       editorconfig. However, adding this support would require us to update all our IDE code style
    //       analyzers to turn themselves off when 'option.Notification.Severity is ReportDiagnostic.Suppress'
    private static readonly string[] s_severities =
    [
        WorkspacesResources.Refactoring_Only,
        WorkspacesResources.Suggestion,
        WorkspacesResources.Warning,
        WorkspacesResources.Error
    ];

    private readonly int _selectedSeverityIndex;
    private readonly CodeStyleSetting _setting;

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

    public string ToolTip => ServicesVSResources.Severity;

    public static string AutomationName => ServicesVSResources.Severity;

    public CodeStyleSeverityViewModel(CodeStyleSetting setting)
    {
        _selectedSeverityIndex = setting.GetSeverity() switch
        {
            ReportDiagnostic.Hidden => 0,
            ReportDiagnostic.Info => 1,
            ReportDiagnostic.Warn => 2,
            ReportDiagnostic.Error => 3,
            _ => throw new InvalidOperationException(),
        };

        _setting = setting;
    }

    public void SelectionChanged(int selectedIndex)
    {
        var severity = selectedIndex switch
        {
            0 => ReportDiagnostic.Hidden,
            1 => ReportDiagnostic.Info,
            2 => ReportDiagnostic.Warn,
            3 => ReportDiagnostic.Error,
            _ => throw new InvalidOperationException(),
        };

        if (_setting.GetSeverity() != severity)
        {
            _setting.ChangeSeverity(severity);
        }
    }
}
