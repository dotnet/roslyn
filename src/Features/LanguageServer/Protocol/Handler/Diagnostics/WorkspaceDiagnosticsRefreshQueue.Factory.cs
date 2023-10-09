// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

internal sealed partial class WorkspaceDiagnosticsRefreshQueue
{
    [ExportCSharpVisualBasicLspServiceFactory(typeof(WorkspaceDiagnosticsRefreshQueue)), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    private sealed class Factory(
        IAsynchronousOperationListenerProvider asynchronousOperationListenerProvider,
        LspWorkspaceRegistrationService lspWorkspaceRegistrationService,
        Refresher refresher) : AbstractFactory(asynchronousOperationListenerProvider, lspWorkspaceRegistrationService, refresher)
    {
        protected override AbstractDiagnosticsRefreshQueue CreateDiagnosticsRefreshQueue(IAsynchronousOperationListenerProvider asyncListenerProvider, LspWorkspaceRegistrationService lspWorkspaceRegistrationService, LspWorkspaceManager lspWorkspaceManager, IClientLanguageServerManager notificationManager, Refresher refresher)
            => new WorkspaceDiagnosticsRefreshQueue(asyncListenerProvider, lspWorkspaceRegistrationService, lspWorkspaceManager, notificationManager, refresher);
    }
}
