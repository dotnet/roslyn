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
internal class ExperimentalDocumentPullDiagnosticHandlerProvider : IRequestHandlerProvider<ExperimentalDocumentPullDiagnosticsHandler>
{
    private readonly IDiagnosticService _diagnosticService;
    private readonly IDiagnosticAnalyzerService _analyzerService;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public ExperimentalDocumentPullDiagnosticHandlerProvider(
        IDiagnosticService diagnosticService,
        IDiagnosticAnalyzerService analyzerService)
    {
        _diagnosticService = diagnosticService;
        _analyzerService = analyzerService;
    }

    ExperimentalDocumentPullDiagnosticsHandler IRequestHandlerProvider<ExperimentalDocumentPullDiagnosticsHandler>.CreateRequestHandler(WellKnownLspServerKinds serverKind)
        => new(serverKind, _diagnosticService, _analyzerService);
}
