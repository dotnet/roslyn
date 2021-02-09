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
    [ExportLspRequestHandlerProvider, Shared]
    [ProvidesMethod(MSLSPMethods.WorkspacePullDiagnosticName)]
    internal class WorkspacePullDiagnosticHandlerProvider : AbstractRequestHandlerProvider
    {
        private readonly IDiagnosticService _diagnosticService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public WorkspacePullDiagnosticHandlerProvider(IDiagnosticService diagnosticService)
        {
            _diagnosticService = diagnosticService;
        }

        public override ImmutableArray<IRequestHandler> CreateRequestHandlers()
        {
            return ImmutableArray.Create<IRequestHandler>(new WorkspacePullDiagnosticHandler(_diagnosticService));
        }
    }
}
