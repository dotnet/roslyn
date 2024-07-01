// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics.Public;

[Export(typeof(IDiagnosticSourceProvider)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class PublicDocumentNonLocalDiagnosticSourceProvider(
    [Import] IGlobalOptionService globalOptions,
    [Import] IDiagnosticAnalyzerService diagnosticAnalyzerService)
    : IDiagnosticSourceProvider
{
    public const string NonLocal = nameof(NonLocal);
    public bool IsDocument => true;
    public string Name => NonLocal;

    public bool IsEnabled(ClientCapabilities clientCapabilities) => true;

    public ValueTask<ImmutableArray<IDiagnosticSource>> CreateDiagnosticSourcesAsync(RequestContext context, CancellationToken cancellationToken)
    {
        // Non-local document diagnostics are reported only when full solution analysis is enabled for analyzer execution.
        if (context.GetTrackedDocument<TextDocument>() is { } textDocument &&
            globalOptions.GetBackgroundAnalysisScope(textDocument.Project.Language) == BackgroundAnalysisScope.FullSolution)
        {
            // NOTE: Compiler does not report any non-local diagnostics, so we bail out for compiler analyzer.
            return new([new NonLocalDocumentDiagnosticSource(textDocument, diagnosticAnalyzerService, a => !a.IsCompilerAnalyzer())]);
        }

        return new([]);
    }
}
