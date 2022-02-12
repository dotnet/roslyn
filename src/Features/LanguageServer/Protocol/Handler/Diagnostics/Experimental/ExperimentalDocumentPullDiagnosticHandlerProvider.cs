// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics.Experimental;

[ExportRoslynLanguagesLspRequestHandlerProvider, Shared]
internal class ExperimentalDocumentPullDiagnosticHandlerProvider : AbstractRequestHandlerProvider
{
    private readonly Lazy<IDiagnosticService> _diagnosticService;
    private readonly Lazy<IDiagnosticAnalyzerService> _analyzerService;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public ExperimentalDocumentPullDiagnosticHandlerProvider(
        Lazy<IDiagnosticService> diagnosticService,
        Lazy<IDiagnosticAnalyzerService> analyzerService)
    {
        _diagnosticService = diagnosticService;
        _analyzerService = analyzerService;
    }

    public override ImmutableArray<LazyRequestHandler> CreateRequestHandlers(WellKnownLspServerKinds serverKind)
    {
        return CreateSingleRequestHandler(() => new ExperimentalDocumentPullDiagnosticsHandler(serverKind, _diagnosticService.Value, _analyzerService.Value));
    }
}
