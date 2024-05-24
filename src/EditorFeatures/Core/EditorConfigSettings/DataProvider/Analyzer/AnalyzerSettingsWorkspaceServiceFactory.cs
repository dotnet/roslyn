// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.DataProvider.Analyzer;

[ExportWorkspaceServiceFactory(typeof(IWorkspaceSettingsProviderFactory<AnalyzerSetting>)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class AnalyzerSettingsWorkspaceServiceFactory(IDiagnosticAnalyzerService analyzerService) : IWorkspaceServiceFactory
{
    private readonly IDiagnosticAnalyzerService _analyzerService = analyzerService;

    public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        => new AnalyzerSettingsProviderFactory(workspaceServices.Workspace, _analyzerService);
}
