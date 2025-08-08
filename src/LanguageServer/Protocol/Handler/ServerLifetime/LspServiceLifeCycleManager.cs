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
using Roslyn.LanguageServer.Protocol;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.ServerLifetime;

internal sealed class LspServiceLifeCycleManager : ILifeCycleManager, ILspService
{
    private readonly IClientLanguageServerManager _clientLanguageServerManager;
    private readonly LspWorkspaceRegistrationService _lspWorkspaceRegistrationService;

    [ExportCSharpVisualBasicLspServiceFactory(typeof(LspServiceLifeCycleManager)), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal class LspLifeCycleManagerFactory(LspWorkspaceRegistrationService lspWorkspaceRegistrationService) : ILspServiceFactory
    {
        public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
        {
            var clientLanguageServerManager = lspServices.GetRequiredService<IClientLanguageServerManager>();
            return new LspServiceLifeCycleManager(clientLanguageServerManager, lspWorkspaceRegistrationService);
        }
    }

    private LspServiceLifeCycleManager(IClientLanguageServerManager clientLanguageServerManager, LspWorkspaceRegistrationService lspWorkspaceRegistrationService)
    {
        _clientLanguageServerManager = clientLanguageServerManager;
        _lspWorkspaceRegistrationService = lspWorkspaceRegistrationService;
    }

    public async Task ShutdownAsync(string message = "Shutting down")
    {
        // Shutting down is not cancellable.
        var cancellationToken = CancellationToken.None;

        // HACK: we're doing FirstOrDefault rather than SingleOrDefault because right now in unit tests we might have more than one. Tests that derive from
        // AbstractLanguageServerProtocolTests create a TestLspWorkspace, even if the ExportProvider already has some other workspace registered.
        // Since we're only using this as a proxy to fetch a workspace service that won't differ between the workspaces, we can pick any of them.
        var hostWorkspace = _lspWorkspaceRegistrationService.GetAllRegistrations().FirstOrDefault(w => w.Kind == WorkspaceKind.Host);
        if (hostWorkspace is not null)
        {
            var service = hostWorkspace.Services.GetRequiredService<IExtensionMessageHandlerService>();
            await service.ResetAsync(cancellationToken).ConfigureAwait(false);
        }

        try
        {
            var messageParams = new LogMessageParams()
            {
                MessageType = MessageType.Info,
                Message = message
            };
            await _clientLanguageServerManager.SendNotificationAsync("window/logMessage", messageParams, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is ObjectDisposedException or ConnectionLostException)
        {
            //Don't fail shutdown just because jsonrpc has already been cancelled.
        }
    }

    public Task ExitAsync()
    {
        // We don't need any custom logic to run on exit.
        return Task.CompletedTask;
    }
}
