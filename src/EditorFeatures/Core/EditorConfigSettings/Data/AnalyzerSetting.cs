﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Updater;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data
{
    internal class AnalyzerSetting
    {
        private readonly DiagnosticDescriptor _descriptor;
        private readonly AnalyzerSettingsUpdater _settingsUpdater;

        public AnalyzerSetting(DiagnosticDescriptor descriptor,
                               ReportDiagnostic effectiveSeverity,
                               AnalyzerSettingsUpdater settingsUpdater,
                               Language language)
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
        }

        public string Id => _descriptor.Id;
        public string Title => _descriptor.Title.ToString(CultureInfo.CurrentCulture);
        public string Description => _descriptor.Description.ToString(CultureInfo.CurrentCulture);
        public string Category => _descriptor.Category;
        public DiagnosticSeverity Severity { get; private set; }
        public bool IsEnabled { get; private set; }
        public Language Language { get; }

        internal void ChangeSeverity(DiagnosticSeverity severity)
        {
            if (severity == Severity)
                return;

            Severity = severity;
            _settingsUpdater.QueueUpdate(this, severity);
        }
    }
}
