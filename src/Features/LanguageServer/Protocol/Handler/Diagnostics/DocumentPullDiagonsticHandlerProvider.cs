// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics
{
    [ExportRoslynLanguagesLspRequestHandlerProvider, Shared]
    [ProvidesMethod(VSInternalMethods.DocumentPullDiagnosticName)]
    internal class DocumentPullDiagonsticHandlerProvider : AbstractRequestHandlerProvider
    {
        private readonly IDiagnosticService _diagnosticService;
        private readonly IDiagnosticAnalyzerService _analyzerService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DocumentPullDiagonsticHandlerProvider(
            IDiagnosticService diagnosticService,
            IDiagnosticAnalyzerService analyzerService)
        {
            _diagnosticService = diagnosticService;
            _analyzerService = analyzerService;
        }

        public override ImmutableArray<IRequestHandler> CreateRequestHandlers()
        {
            return ImmutableArray.Create<IRequestHandler>(new DocumentPullDiagnosticHandler(_diagnosticService, _analyzerService));
        }
    }
}
