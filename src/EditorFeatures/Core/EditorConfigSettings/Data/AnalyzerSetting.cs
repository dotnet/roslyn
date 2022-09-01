﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Updater;
using Microsoft.CodeAnalysis.EditorConfig;
using Microsoft.CodeAnalysis.EditorConfigSettings;
using Microsoft.CodeAnalysis.EditorConfigSettings.Data;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data
{
    internal class AnalyzerSetting : IEditorConfigSettingInfo
    {
        private readonly DiagnosticDescriptor _descriptor;
        private readonly AnalyzerSettingsUpdater _settingsUpdater;

        public IEditorConfigData EditorConfigData;

        public AnalyzerSetting(DiagnosticDescriptor descriptor,
                               ReportDiagnostic effectiveSeverity,
                               AnalyzerSettingsUpdater settingsUpdater,
                               Language language,
                               SettingLocation location,
                               IEditorConfigData editorConfigData)
        {
            _descriptor = descriptor;
            _settingsUpdater = settingsUpdater;
            DiagnosticSeverity severity = default;
            if (effectiveSeverity == ReportDiagnostic.Default)
            {
                severity = descriptor.DefaultSeverity;
            }
            else if (effectiveSeverity.ToDiagnosticSeverity() is DiagnosticSeverity severity1)
            {
                severity = severity1;
            }

            var enabled = effectiveSeverity != ReportDiagnostic.Suppress;
            IsEnabled = enabled;
            Severity = severity;
            Language = language;
            IsNotConfigurable = descriptor.CustomTags.Any(t => t == WellKnownDiagnosticTags.NotConfigurable);
            Location = location;
            EditorConfigData = editorConfigData;
        }

        public string Id => _descriptor.Id;
        public string Title => _descriptor.Title.ToString(CultureInfo.CurrentUICulture);
        public string Description => _descriptor.Description.ToString(CultureInfo.CurrentUICulture);
        public string Category => _descriptor.Category;
        public DiagnosticSeverity Severity { get; private set; }
        public bool IsEnabled { get; private set; }
        public Language Language { get; }
        public bool IsNotConfigurable { get; set; }
        public SettingLocation Location { get; }

        internal void ChangeSeverity(DiagnosticSeverity severity)
        {
            if (severity == Severity)
                return;

            Severity = severity;
            _settingsUpdater.QueueUpdate(this, severity);
        }

        public string? GetSettingName() => EditorConfigData.GetSettingName().Replace("Id", Id);

        public string GetDocumentation() => $"{Description}\n\n{_descriptor.HelpLinkUri}";

        public ImmutableArray<string>? GetSettingValues() => EditorConfigData.GetAllSettingValues();

        public string? GetValueDocumentation(string value) => EditorConfigData.GetSettingValueDocumentation(value);

        public bool IsValueValid(string value) => EditorConfigData.IsValueValid(value);

        public bool SupportsSeverities() => false;

        public bool AllowsMultipleValues() => false;
    }
}
