// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Updater;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.DataProvider.Analyzer;

internal sealed class AnalyzerSettingsProviderFactory(
    IThreadingContext threadingContext,
    Workspace workspace,
    IGlobalOptionService globalOptionService) : IWorkspaceSettingsProviderFactory<AnalyzerSetting>
{
    public ISettingsProvider<AnalyzerSetting> GetForFile(string filePath)
    {
        var updater = new AnalyzerSettingsUpdater(workspace, filePath);
        return new AnalyzerSettingsProvider(threadingContext, filePath, updater, workspace, globalOptionService);
    }
}
