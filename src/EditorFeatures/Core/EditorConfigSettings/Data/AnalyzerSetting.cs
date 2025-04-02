// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Updater;
using Microsoft.CodeAnalysis.EditorConfig;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;

internal sealed class AnalyzerSetting
{
    private readonly DiagnosticDescriptor _descriptor;
    private readonly AnalyzerSettingsUpdater _settingsUpdater;

    public AnalyzerSetting(DiagnosticDescriptor descriptor,
                           ReportDiagnostic effectiveSeverity,
                           AnalyzerSettingsUpdater settingsUpdater,
                           Language language,
                           SettingLocation location)
    {
        _descriptor = descriptor;
        _settingsUpdater = settingsUpdater;
        if (effectiveSeverity == ReportDiagnostic.Default)
        {
            effectiveSeverity = descriptor.DefaultSeverity.ToReportDiagnostic();
        }

        var enabled = effectiveSeverity != ReportDiagnostic.Suppress;
        IsEnabled = enabled;
        Severity = effectiveSeverity;
        Language = language;
        IsNotConfigurable = descriptor.CustomTags.Any(t => t == WellKnownDiagnosticTags.NotConfigurable);
        Location = location;
    }

    public string Id => _descriptor.Id;
    public string Title => _descriptor.Title.ToString(CultureInfo.CurrentUICulture);
    public string Description => _descriptor.Description.ToString(CultureInfo.CurrentUICulture);
    public string Category => _descriptor.Category;
    public ReportDiagnostic Severity { get; private set; }
    public bool IsEnabled { get; private set; }
    public Language Language { get; }
    public bool IsNotConfigurable { get; set; }
    public SettingLocation Location { get; }

    internal void ChangeSeverity(ReportDiagnostic severity)
    {
        if (severity == Severity)
            return;

        Severity = severity;
        _settingsUpdater.QueueUpdate(this, severity);
    }
}
