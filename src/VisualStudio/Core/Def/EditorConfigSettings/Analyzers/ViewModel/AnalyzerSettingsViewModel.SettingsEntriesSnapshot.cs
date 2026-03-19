// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Common;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Analyzers.ViewModel;

internal sealed partial class AnalyzerSettingsViewModel : SettingsViewModelBase<
    AnalyzerSetting,
    AnalyzerSettingsViewModel.SettingsSnapshotFactory,
    AnalyzerSettingsViewModel.SettingsEntriesSnapshot>
{
    internal sealed class SettingsEntriesSnapshot : SettingsEntriesSnapshotBase<AnalyzerSetting>
    {
        public SettingsEntriesSnapshot(ImmutableArray<AnalyzerSetting> data, int currentVersionNumber) : base(data, currentVersionNumber) { }

        protected override bool TryGetValue(AnalyzerSetting result, string keyName, out object? content)
        {
            content = keyName switch
            {
                ColumnDefinitions.Analyzer.Enabled => result.IsEnabled,
                ColumnDefinitions.Analyzer.Id => result.Id,
                ColumnDefinitions.Analyzer.Title => result.Title,
                ColumnDefinitions.Analyzer.Description => result.Description,
                ColumnDefinitions.Analyzer.Category => result.Category,
                ColumnDefinitions.Analyzer.Severity => result,
                ColumnDefinitions.Analyzer.Location => GetLocationString(result.Location),
                _ => null,
            };

            return content is not null;
        }

        private static string? GetLocationString(SettingLocation location)
        {
            return location.LocationKind switch
            {
                LocationKind.EditorConfig or LocationKind.GlobalConfig => location.Path,
                _ => ServicesVSResources.Analyzer_Defaults
            };
        }
    }
}
