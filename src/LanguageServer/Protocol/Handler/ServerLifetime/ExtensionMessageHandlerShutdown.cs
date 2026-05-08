// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CommonLanguageServerProtocol.Framework;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.ServerLifetime;

[ExportCSharpVisualBasicStatelessLspService(typeof(ExtensionMessageHandlerShutdown)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class ExtensionMessageHandlerShutdown(LspWorkspaceRegistrationService lspWorkspaceRegistrationService) : IOnServerShutdown, ILspService
{
    public virtual async Task ShutdownAsync()
    {
        // Shutting down is not cancellable.
        var cancellationToken = CancellationToken.None;

        // HACK: we're doing FirstOrDefault rather than SingleOrDefault because right now in unit tests we might have more than one. Tests that derive from
        // AbstractLanguageServerProtocolTests create a TestLspWorkspace, even if the ExportProvider already has some other workspace registered.
        // Since we're only using this as a proxy to fetch a workspace service that won't differ between the workspaces, we can pick any of them.
        var hostWorkspace = lspWorkspaceRegistrationService.GetAllRegistrations().FirstOrDefault(w => w.Kind == WorkspaceKind.Host);
        if (hostWorkspace is not null)
        {
            var service = hostWorkspace.Services.GetRequiredService<IExtensionMessageHandlerService>();
            await service.ResetAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public Task ExitAsync() => Task.CompletedTask;
}
