// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CommonLanguageServerProtocol.Framework;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

internal class ClientLanguageServerManager : IClientLanguageServerManager
{
    private readonly JsonRpc _jsonRpc;
    private readonly AbstractLspLogger _logger;

    public ClientLanguageServerManager(JsonRpc jsonRpc, AbstractLspLogger logger)
    {
        _jsonRpc = jsonRpc;
        _logger = logger;
    }

    public async Task<TResponse?> SendRequestAsync<TParams, TResponse>(string methodName, TParams @params, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            return await _jsonRpc.InvokeWithParameterObjectAsync<TResponse>(methodName, @params, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ConnectionLostFilter(ex, methodName))
        {
        }

        return default;
    }

    public async ValueTask SendRequestAsync(string methodName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            await _jsonRpc.InvokeWithCancellationAsync(methodName, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ConnectionLostFilter(ex, methodName))
        {
        }
    }

    public async ValueTask SendRequestAsync<TParams>(string methodName, TParams @params, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            await _jsonRpc.InvokeWithParameterObjectAsync(methodName, @params, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ConnectionLostFilter(ex, methodName))
        {
        }
    }

    public async ValueTask SendNotificationAsync(string methodName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            await _jsonRpc.NotifyAsync(methodName).ConfigureAwait(false);
        }
        catch (Exception ex) when (ConnectionLostFilter(ex, methodName))
        {
        }
    }

    public async ValueTask SendNotificationAsync<TParams>(string methodName, TParams @params, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            await _jsonRpc.NotifyWithParameterObjectAsync(methodName, @params).ConfigureAwait(false);
        }
        catch (Exception ex) when (ConnectionLostFilter(ex, methodName))
        {
        }
    }

    private bool ConnectionLostFilter(Exception exception, string methodName)
    {
        if (exception is ObjectDisposedException or ConnectionLostException)
        {
            // It is very possible for the jsonRpc instance to go away while we are trying to send a request to the client.
            // The majority of client notifications happen outside of the queue, and even if they are in the queue, the jsonRpc
            // instance could be killed at any moment.  It is not necessary to have these exceptions bubble up and get reported as NFW.
            //
            // We already report in the server when the jsonRpc instance goes away unexpectedly, so we'll just swallow these here and move on.
            _logger.LogDebug($"Failed to send request {methodName} due to connection lost or object disposed exception.");
            return true;
        }

        return false;
    }
}
