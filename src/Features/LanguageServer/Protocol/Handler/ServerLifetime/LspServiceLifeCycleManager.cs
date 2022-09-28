// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.ServerLifetime;

internal class LspServiceLifeCycleManager : ILifeCycleManager, ILspService
{
    private readonly ILspLogger _logger;
    private readonly IClientLanguageServerManager _clientLanguageServerManager;
    private readonly AbstractLanguageServer<RequestContext> _languageServerTarget;

    public LspServiceLifeCycleManager(AbstractLanguageServer<RequestContext> languageServerTarget, ILspLogger logger, IClientLanguageServerManager clientLanguageServerManager)
    {
        _logger = logger;
        _clientLanguageServerManager = clientLanguageServerManager;
        _languageServerTarget = languageServerTarget;
    }

    public async Task ShutdownAsync(string message = "Shutting down")
    {
        await _languageServerTarget.ShutdownAsync(message).ConfigureAwait(false);

        _logger.LogInformation("Shutting down language server.");

        try
        {
            var messageParams = new LogMessageParams()
            {
                MessageType = MessageType.Info,
                Message = message
            };
            await _clientLanguageServerManager.SendNotificationAsync("window/logMessage", messageParams, CancellationToken.None).ConfigureAwait(false);
        }
        catch (ConnectionLostException)
        {
            //Don't fail shutdown just because jsonrpc has already been cancelled.
        }
    }

    public async Task ExitAsync()
    {
        await _languageServerTarget.ExitAsync().ConfigureAwait(false);
    }
}
