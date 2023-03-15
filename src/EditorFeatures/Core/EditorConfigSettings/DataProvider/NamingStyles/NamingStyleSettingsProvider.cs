// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Extensions;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Updater;
using Microsoft.CodeAnalysis.Options;
using RoslynEnumerableExtensions = Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Extensions.EnumerableExtensions;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.DataProvider.NamingStyles
{
    internal class NamingStyleSettingsProvider : SettingsProviderBase<NamingStyleSetting, NamingStyleSettingsUpdater, (Action<(object, object?)>, NamingStyleSetting), object>
    {
        public NamingStyleSettingsProvider(string fileName, NamingStyleSettingsUpdater settingsUpdater, Workspace workspace, IGlobalOptionService globalOptions)
            : base(fileName, settingsUpdater, workspace, globalOptions)
        {
            Update();
        }

        protected override void UpdateOptions(TieredAnalyzerConfigOptions options, ImmutableArray<Project> projectsInScope)
        {
            options.GetInitialLocationAndValue<NamingStylePreferences>(NamingStyleOptions.NamingPreferences, out var location, out var namingPreferences);

            var fileName = (location.LocationKind != LocationKind.VisualStudio) ? options.EditorConfigFileName : null;
            var namingRules = namingPreferences.NamingRules.Select(r => r.GetRule(namingPreferences));
            var allStyles = RoslynEnumerableExtensions.DistinctBy(namingPreferences.NamingStyles, s => s.Name).ToArray();
            var namingStyles = namingRules.Select(namingRule => new NamingStyleSetting(namingRule, allStyles, SettingsUpdater, fileName));

            AddRange(namingStyles);
        }
    }
}
