// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Reflection;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Services;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.Extensions.Logging;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

[Export(typeof(VSCodeAnalyzerLoader)), Shared]
internal class VSCodeAnalyzerLoader
{
    private readonly IDiagnosticAnalyzerService _analyzerService;
    private readonly DiagnosticService _diagnosticService;
    private readonly IGlobalOptionService _globalOptionService;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public VSCodeAnalyzerLoader(IDiagnosticAnalyzerService analyzerService, IDiagnosticService diagnosticService, IGlobalOptionService globalOptionService)
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

    public static IAnalyzerAssemblyLoader CreateAnalyzerAssemblyLoader(ExtensionAssemblyManager extensionAssemblyManager, ILogger logger)
    {
        return new VSCodeExtensionAssemblyAnalyzerLoader(extensionAssemblyManager, logger);
    }

    /// <summary>
    /// Analyzer loader that will re-use already loaded assemblies from the extension load context.
    /// </summary>
    private class VSCodeExtensionAssemblyAnalyzerLoader(ExtensionAssemblyManager extensionAssemblyManager, ILogger logger) : IAnalyzerAssemblyLoader
    {
        private readonly DefaultAnalyzerAssemblyLoader _defaultLoader = new();

        public void AddDependencyLocation(string fullPath)
        {
            _defaultLoader.AddDependencyLocation(fullPath);
        }

        public Assembly LoadFromPath(string fullPath)
        {
            var assembly = extensionAssemblyManager.TryLoadAssemblyInExtensionContext(fullPath);
            if (assembly is not null)
            {
                logger.LogTrace("Loaded analyzer {fullPath} from extension context", fullPath);
                return assembly;
            }

            return _defaultLoader.LoadFromPath(fullPath);
        }
    }
}
