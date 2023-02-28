// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor;

[Export(typeof(RazorTestAnalyzerLoader)), Shared]
internal class RazorTestAnalyzerLoader
{
    private readonly IDiagnosticAnalyzerService _analyzerService;
    private readonly DiagnosticService _diagnosticService;
    private readonly IGlobalOptionService _globalOptionService;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public RazorTestAnalyzerLoader(IDiagnosticAnalyzerService analyzerService, IDiagnosticService diagnosticService, IGlobalOptionService globalOptionService)
    {
        _analyzerService = analyzerService;
        _diagnosticService = (DiagnosticService)diagnosticService;
        _globalOptionService = globalOptionService;
    }

    public void InitializeDiagnosticsServices(Workspace workspace)
    {
        _globalOptionService.SetGlobalOption(InternalDiagnosticsOptionsStorage.NormalDiagnosticMode, DiagnosticMode.LspPull);
        _ = ((IIncrementalAnalyzerProvider)_analyzerService).CreateIncrementalAnalyzer(workspace);
        _diagnosticService.Register((IDiagnosticUpdateSource)_analyzerService);
    }

    public static IAnalyzerAssemblyLoader CreateAnalyzerAssemblyLoader()
    {
        return new DefaultAnalyzerAssemblyLoader();
    }
}
