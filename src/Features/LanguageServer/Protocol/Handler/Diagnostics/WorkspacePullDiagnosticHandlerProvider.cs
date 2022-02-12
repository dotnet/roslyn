// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics
{
    [ExportRoslynLanguagesLspRequestHandlerProvider(), Shared]
    internal class WorkspacePullDiagnosticHandlerProvider : AbstractRequestHandlerProvider
    {
        private readonly Lazy<IDiagnosticService> _diagnosticService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public WorkspacePullDiagnosticHandlerProvider(Lazy<IDiagnosticService> diagnosticService)
        {
            _diagnosticService = diagnosticService;
        }

        public override ImmutableArray<LazyRequestHandler> CreateRequestHandlers(WellKnownLspServerKinds serverKind)
            => CreateSingleRequestHandler(() => new WorkspacePullDiagnosticHandler(serverKind, _diagnosticService.Value));
    }
}
