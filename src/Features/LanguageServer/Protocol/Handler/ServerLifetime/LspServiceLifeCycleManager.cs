// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.ServerLifetime;

internal class LspServiceLifeCycleManager : ILifeCycleManager, ILspService
{
    private readonly ILspLogger _logger;
    private readonly IClientLanguageServerManager _clientLanguageServerManager;
    private readonly ILifeCycleManager _lifeCycleManager;

    public LspServiceLifeCycleManager(AbstractLanguageServer<RequestContext> languageServerTarget, ILspLogger logger, IClientLanguageServerManager clientLanguageServerManager)
    {
        _logger = logger;
        _clientLanguageServerManager = clientLanguageServerManager;
        _lifeCycleManager = languageServerTarget;
    }

    public async Task ShutdownAsync(string message = "Shutting down")
    {
        await _lifeCycleManager.ShutdownAsync(message).ConfigureAwait(false);

        _logger.LogInformation("Shutting down language server.");

        var messageParams = new LogMessageParams()
        {
            MessageType = MessageType.Error,
            Message = message
        };
        await _clientLanguageServerManager.SendNotificationAsync("window/logMessage", messageParams, CancellationToken.None).ConfigureAwait(false);
    }

    public async Task ExitAsync()
    {
        await _lifeCycleManager.ExitAsync().ConfigureAwait(false);
    }
}
