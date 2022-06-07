// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Updater;
using Microsoft.CodeAnalysis.EditorConfig.Parsing.NamingStyles;
using Microsoft.CodeAnalysis.NamingStyles;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data
{
    internal class NamingStyleSetting
    {
        private NamingStyle[] _allStyles;
        private readonly NamingStyleSettingsUpdater? _settingsUpdater;

        public NamingStyleSetting(
            NamingRule namingRule,
            NamingStyle[] allStyles,
            NamingStyleSettingsUpdater settingsUpdater,
            string? fileName = null)
        {
            Style = namingRule.NamingStyle;
            _allStyles = allStyles;
            Type = namingRule.SymbolSpecification;
            Severity = namingRule.EnforcementLevel;
            _settingsUpdater = settingsUpdater;
            Location = new SettingLocation(fileName is null ? LocationKind.VisualStudio : LocationKind.EditorConfig, fileName);
        }

        private NamingStyleSetting()
        {
            _allStyles = Array.Empty<NamingStyle>();
        }

        public event EventHandler<EventArgs>? SettingChanged;

        internal static NamingStyleSetting FromParseResult(NamingStyleOption namingStyleOption)
        {
            return new NamingStyleSetting
            {
                Style = namingStyleOption.NamingScheme.AsNamingStyle(),
                Type = namingStyleOption.ApplicableSymbolInfo.AsSymbolSpecification(),
                Severity = namingStyleOption.Severity,
                Location = new SettingLocation(LocationKind.EditorConfig, namingStyleOption.Section.FilePath)
            };
        }

        internal NamingStyle Style { get; set; }
        internal SymbolSpecification? Type { get; set; }

        public string StyleName => Style.Name;
        public string[] AllStyles => _allStyles.Select(style => style.Name).ToArray();
        public string TypeName => Type?.Name ?? string.Empty;
        public ReportDiagnostic Severity { get; private set; }
        public SettingLocation? Location { get; protected set; }

        private void OnSettingChanged((object, object?) setting)
        {
            if (setting is (ReportDiagnostic severity, _))
            {
                Severity = severity;
                SettingChanged?.Invoke(this, EventArgs.Empty);
            }

            if (setting is (NamingStyle style, NamingStyle[] allStyles))
            {
                Style = style;
                _allStyles = allStyles;
                SettingChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        internal void ChangeSeverity(ReportDiagnostic severity)
        {
            if (Location is not null)
            {
                Location = Location with { LocationKind = LocationKind.EditorConfig };
                _settingsUpdater?.QueueUpdate((OnSettingChanged, this), severity);
            }
        }

        internal void ChangeStyle(int selectedIndex)
        {
            if (selectedIndex > -1 && selectedIndex < _allStyles.Length && Location is not null)
            {
                Location = Location with { LocationKind = LocationKind.EditorConfig };
                _settingsUpdater?.QueueUpdate((OnSettingChanged, this), _allStyles[selectedIndex]);
            }
        }
    }
}
