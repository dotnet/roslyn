﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics
{
    [Shared]
    [ExportRoslynLanguagesLspRequestHandlerProvider(typeof(DocumentPullDiagnosticHandler), typeof(WorkspacePullDiagnosticHandler))]
    internal class PullDiagnosticHandlerProvider : AbstractRequestHandlerProvider
    {
        private readonly IDiagnosticService _diagnosticService;
        private readonly IDiagnosticAnalyzerService _analyzerService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public PullDiagnosticHandlerProvider(
            IDiagnosticService diagnosticService,
            IDiagnosticAnalyzerService analyzerService)
        {
            _diagnosticService = diagnosticService;
            _analyzerService = analyzerService;
        }

        public override ImmutableArray<IRequestHandler> CreateRequestHandlers(WellKnownLspServerKinds serverKind)
        {
            return ImmutableArray.Create<IRequestHandler>(
                new DocumentPullDiagnosticHandler(serverKind, _diagnosticService, _analyzerService),
                new WorkspacePullDiagnosticHandler(serverKind, _diagnosticService));
        }
    }
}
