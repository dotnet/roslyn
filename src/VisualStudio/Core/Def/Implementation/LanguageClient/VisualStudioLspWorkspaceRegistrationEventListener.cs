// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageClient
{
    [ExportEventListener(WellKnownEventListeners.Workspace, WorkspaceKind.Host, WorkspaceKind.MiscellaneousFiles, WorkspaceKind.MetadataAsSource), Shared]
    internal class VisualStudioLspWorkspaceRegistrationEventListener : IEventListener<object>
    {
        private readonly ILspWorkspaceRegistrationService _lspWorkspaceRegistrationService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioLspWorkspaceRegistrationEventListener(ILspWorkspaceRegistrationService lspWorkspaceRegistrationService)
        {
            _lspWorkspaceRegistrationService = lspWorkspaceRegistrationService;
        }

        public void StartListening(Workspace workspace, object _)
        {
            _lspWorkspaceRegistrationService.Register(workspace);
        }
    }
}
