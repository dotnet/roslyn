// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

internal sealed class ClientLanguageServerManager : IClientLanguageServerManager
{
    private readonly JsonRpc _jsonRpc;

    public ClientLanguageServerManager(JsonRpc jsonRpc)
    {
        if (jsonRpc is null)
        {
            throw new ArgumentNullException(nameof(jsonRpc));
        }

        _jsonRpc = jsonRpc;
    }

    public Task<TResponse> SendRequestAsync<TParams, TResponse>(string methodName, TParams @params, CancellationToken cancellationToken)
        => _jsonRpc.InvokeWithParameterObjectAsync<TResponse>(methodName, @params, cancellationToken);

    public async ValueTask SendRequestAsync(string methodName, CancellationToken cancellationToken)
        => await _jsonRpc.InvokeWithParameterObjectAsync(methodName, cancellationToken: cancellationToken).ConfigureAwait(false);

    public async ValueTask SendRequestAsync<TParams>(string methodName, TParams @params, CancellationToken cancellationToken)
        => await _jsonRpc.InvokeWithParameterObjectAsync(methodName, @params, cancellationToken).ConfigureAwait(false);

    public async ValueTask SendNotificationAsync(string methodName, CancellationToken cancellationToken)
        => await _jsonRpc.NotifyWithParameterObjectAsync(methodName).ConfigureAwait(false);

    public async ValueTask SendNotificationAsync<TParams>(string methodName, TParams @params, CancellationToken cancellationToken)
        => await _jsonRpc.NotifyWithParameterObjectAsync(methodName, @params).ConfigureAwait(false);
}
