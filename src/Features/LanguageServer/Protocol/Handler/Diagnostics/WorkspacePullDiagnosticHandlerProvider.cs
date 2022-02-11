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
    [ExportRoslynLanguagesLspRequestHandlerProvider(), Shared]
    internal class WorkspacePullDiagnosticHandlerProvider : IRequestHandlerProvider<WorkspacePullDiagnosticHandler>
    {
        private readonly IDiagnosticService _diagnosticService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public WorkspacePullDiagnosticHandlerProvider(IDiagnosticService diagnosticService)
        {
            _diagnosticService = diagnosticService;
        }

        WorkspacePullDiagnosticHandler IRequestHandlerProvider<WorkspacePullDiagnosticHandler>.CreateRequestHandler(WellKnownLspServerKinds serverKind)
            => new(serverKind, _diagnosticService);
    }
}
