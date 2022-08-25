// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Extensions;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Updater;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.DataProvider.NamingStyles
{
    internal class NamingStyleSettingsProvider : SettingsProviderBase<NamingStyleSetting, NamingStyleSettingsUpdater, (Action<(object, object?)>, NamingStyleSetting), object>
    {
        public NamingStyleSettingsProvider(string fileName, NamingStyleSettingsUpdater settingsUpdater, Workspace workspace)
            : base(fileName, settingsUpdater, workspace)
        {
            Update();
        }

        protected override void UpdateOptions(AnalyzerConfigOptions _, OptionSet optionSet)
        {
            var solution = Workspace.CurrentSolution;
            var projects = solution.GetProjectsUnderEditorConfigFile(FileName);
            var project = projects.FirstOrDefault();
            if (project is null)
            {
                return;
            }

            var document = project.Documents.FirstOrDefault();
            if (document is null)
            {
                return;
            }

            var sourceTree = document.GetRequiredSyntaxTreeSynchronously(CancellationToken.None);
            var configOptions = project.State.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(sourceTree);

            if (configOptions.TryGetEditorConfigOption(NamingStyleOptions.NamingPreferences, out NamingStylePreferences? namingPreferences) &&
                namingPreferences is not null)
            {
                AddNamingStylePreferences(namingPreferences, isInEditorConfig: true);
            }
            else
            {
                namingPreferences = optionSet.GetOption(NamingStyleOptions.NamingPreferences, project.Language);
                if (namingPreferences is null)
                {
                    return;
                }

                AddNamingStylePreferences(namingPreferences, isInEditorConfig: false);
            }

            void AddNamingStylePreferences(NamingStylePreferences namingPreferences, bool isInEditorConfig)
            {
                var namingRules = namingPreferences.NamingRules.Select(r => r.GetRule(namingPreferences));
                var allStyles = namingPreferences.NamingStyles.DistinctBy(s => s.Name).ToArray();
                var namingStyles = namingRules
                    .Select(namingRule =>
                    {
                        var style = namingRule.NamingStyle;
                        var type = namingRule.SymbolSpecification;
                        return isInEditorConfig
                            ? new NamingStyleSetting(namingRule, allStyles, SettingsUpdater, FileName)
                            : new NamingStyleSetting(namingRule, allStyles, SettingsUpdater);
                    });

                AddRange(namingStyles);
            }
        }
    }
}
