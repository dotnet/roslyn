// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Roslyn.LanguageServer.Protocol;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.ServerLifetime;

internal class LspServiceLifeCycleManager : ILifeCycleManager, ILspService
{
    private readonly IClientLanguageServerManager _clientLanguageServerManager;

    public LspServiceLifeCycleManager(IClientLanguageServerManager clientLanguageServerManager)
    {
        _clientLanguageServerManager = clientLanguageServerManager;
    }

    public async Task ShutdownAsync(string message = "Shutting down")
    {
        try
        {
            var messageParams = new LogMessageParams()
            {
                MessageType = MessageType.Info,
                Message = message
            };
            await _clientLanguageServerManager.SendNotificationAsync("window/logMessage", messageParams, CancellationToken.None).ConfigureAwait(false);
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
