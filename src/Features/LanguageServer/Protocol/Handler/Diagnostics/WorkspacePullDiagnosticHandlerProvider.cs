// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics
{
    [ExportRoslynLanguagesLspRequestHandlerProvider(), Shared]
    [ProvidesMethod(VSInternalMethods.WorkspacePullDiagnosticName)]
    internal class WorkspacePullDiagnosticHandlerProvider : AbstractRequestHandlerProvider
    {
        private readonly IDiagnosticService _diagnosticService;
        private readonly ILspWorkspaceRegistrationService _workspaceRegistrationService;
        private readonly IAsyncServiceProvider _serviceProvider;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public WorkspacePullDiagnosticHandlerProvider(IDiagnosticService diagnosticService, SVsServiceProvider serviceProvider, ILspWorkspaceRegistrationService workspaceRegistrationService)
        {
            _diagnosticService = diagnosticService;
            _workspaceRegistrationService = workspaceRegistrationService;
            _serviceProvider = (IAsyncServiceProvider)serviceProvider;
        }

        public override ImmutableArray<IRequestHandler> CreateRequestHandlers()
        {
            return ImmutableArray.Create<IRequestHandler>(new WorkspacePullDiagnosticHandler(_diagnosticService, _serviceProvider, _workspaceRegistrationService));
        }
    }
}
