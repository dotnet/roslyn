// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.VisualStudio.LanguageServices.Razor
{
    // Used in https://github.com/aspnet/AspNetCore-Tooling/tree/main/src/Razor/src/Microsoft.VisualStudio.LanguageServices.Razor/OOPTagHelperResolver.cs
    [Obsolete("Use Microsoft.CodeAnalysis.ExternalAccess.Razor.RazorRemoteHostClient instead")]
    internal sealed class RazorLanguageServiceClient
    {
        private readonly RemoteHostClient _client;
        private readonly RemoteServiceName _serviceName;

        internal RazorLanguageServiceClient(RemoteHostClient client, string serviceName)
        {
            _client = client;
            _serviceName = new RemoteServiceName(serviceName);
        }

        public async Task<Optional<T>> TryRunRemoteAsync<T>(string targetName, Solution? solution, IReadOnlyList<object?> arguments, object? callbackTarget, CancellationToken cancellationToken)
            => await _client.RunRemoteAsync<T>(_serviceName, targetName, solution, arguments, callbackTarget, cancellationToken).ConfigureAwait(false);

        [Obsolete("Use TryRunRemoteAsync instead")]
        public async Task<Session?> CreateSessionAsync(Solution solution, object? callbackTarget = null, CancellationToken cancellationToken = default)
        {
            if (solution == null)
            {
                // keep old behavior for Razor
                return null;
            }

            var connection = await _client.CreateConnectionAsync(_serviceName, callbackTarget: null, cancellationToken).ConfigureAwait(false);

            SessionWithSolution? session = null;
            try
            {
                // transfer ownership of the connection to the session object:
                session = await SessionWithSolution.CreateAsync(connection, solution, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (session == null)
                {
                    connection.Dispose();
                }
            }

            return new Session(session);
        }

        [Obsolete("Use TryRunRemoteAsync instead")]
        public sealed class Session : IDisposable
        {
            private readonly SessionWithSolution _inner;

            internal Session(SessionWithSolution inner)
            {
                _inner = inner;
            }

            public Task InvokeAsync(string targetName, IReadOnlyList<object> arguments, CancellationToken cancellationToken)
            {
                return _inner.KeepAliveSession.RunRemoteAsync(targetName, solution: null, arguments, cancellationToken);
            }

            public Task<T> InvokeAsync<T>(string targetName, IReadOnlyList<object> arguments, CancellationToken cancellationToken)
            {
                return _inner.KeepAliveSession.RunRemoteAsync<T>(targetName, solution: null, arguments, cancellationToken);
            }

            public void Dispose()
            {
                _inner.Dispose();
            }
        }
    }
}
