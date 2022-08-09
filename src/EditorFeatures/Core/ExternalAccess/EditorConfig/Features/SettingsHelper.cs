// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor.EditorConfigSettings;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Roslyn.Utilities;
using System.Linq;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.EditorConfigSettings.Data;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.ExternalAccess.EditorConfig.Features
{
    internal class SettingsHelper
    {
        public static ImmutableArray<IEditorConfigSettingInfo> GetSettingsSnapshots(Workspace workspace, string filePath)
        {
            var settingsAggregator = workspace.Services.GetRequiredService<ISettingsAggregator>();

            var codeStyleProvider = settingsAggregator.GetSettingsProvider<CodeStyleSetting>(filePath);
            var whitespaceProvider = settingsAggregator.GetSettingsProvider<WhitespaceSetting>(filePath);
            var analyzerProvider = settingsAggregator.GetSettingsProvider<AnalyzerSetting>(filePath);

            var codeStyleSnapshot = (IEditorConfigSettingInfo[])codeStyleProvider?.GetCurrentDataSnapshot().ToArray()!;
            var whitespaceSnapshot = (IEditorConfigSettingInfo[])whitespaceProvider?.GetCurrentDataSnapshot().ToArray()!;
            var analyzerSnapshot = (IEditorConfigSettingInfo[])analyzerProvider?.GetCurrentDataSnapshot().ToArray()!;

            return codeStyleSnapshot.Concat(whitespaceSnapshot).Concat(analyzerSnapshot).ToImmutableArray();
        }
    }
}
