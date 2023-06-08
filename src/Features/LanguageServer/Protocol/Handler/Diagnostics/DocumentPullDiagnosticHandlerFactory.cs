// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics
{
    [ExportCSharpVisualBasicLspServiceFactory(typeof(DocumentPullDiagnosticHandler)), Shared]
    internal class DocumentPullDiagnosticHandlerFactory : ILspServiceFactory
    {
        private readonly IDiagnosticAnalyzerService _analyzerService;
        private readonly IDiagnosticsRefresher _diagnosticsRefresher;
        private readonly IGlobalOptionService _globalOptions;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DocumentPullDiagnosticHandlerFactory(
            IDiagnosticAnalyzerService analyzerService,
            IDiagnosticsRefresher diagnosticsRefresher,
            IGlobalOptionService globalOptions)
        {
            _analyzerService = analyzerService;
            _diagnosticsRefresher = diagnosticsRefresher;
            _globalOptions = globalOptions;
        }

        public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
            => new DocumentPullDiagnosticHandler(_analyzerService, _diagnosticsRefresher, _globalOptions);
    }
}
