// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Updater;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.DataProvider.Analyzer;

internal class AnalyzerSettingsProviderFactory(Workspace workspace, IDiagnosticAnalyzerService analyzerService) : IWorkspaceSettingsProviderFactory<AnalyzerSetting>
{
    private readonly Workspace _workspace = workspace;
    private readonly IDiagnosticAnalyzerService _analyzerService = analyzerService;

    public ISettingsProvider<AnalyzerSetting> GetForFile(string filePath)
    {
        var updater = new AnalyzerSettingsUpdater(_workspace, filePath);
        return new AnalyzerSettingsProvider(filePath, updater, _workspace, _analyzerService);
    }
}
