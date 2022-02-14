// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.CodeAnalysis.LanguageServer.ExternalAccess.VSCode.API;

[Export(typeof(VSCodeAnalyzerLoader)), Shared]
internal class VSCodeAnalyzerLoader
{
    private readonly IDiagnosticAnalyzerService _analyzerService;
    private readonly DiagnosticService _diagnosticService;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public VSCodeAnalyzerLoader(IDiagnosticAnalyzerService analyzerService, IDiagnosticService diagnosticService)
    {
        _analyzerService = analyzerService;
        _diagnosticService = (DiagnosticService)diagnosticService;
    }

    public void InitializeDiagnosticsServices(Workspace workspace)
    {
        _ = ((IIncrementalAnalyzerProvider)_analyzerService).CreateIncrementalAnalyzer(workspace);
        _diagnosticService.Register((IDiagnosticUpdateSource)_analyzerService);
    }

    public static IAnalyzerAssemblyLoader CreateAnalyzerAssemblyLoader()
    {
        return new DefaultAnalyzerAssemblyLoader();
    }
}
