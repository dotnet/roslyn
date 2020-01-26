// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.VisualStudio.LanguageServices.Razor
{
    // Used in https://github.com/aspnet/AspNetCore-Tooling/tree/master/src/Razor/src/Microsoft.VisualStudio.LanguageServices.Razor/OOPTagHelperResolver.cs
    internal sealed class RazorLanguageServiceClient
    {
        private readonly RemoteHostClient _client;
        private readonly string _serviceName;

        internal RazorLanguageServiceClient(RemoteHostClient client, string serviceName)
        {
            _client = client;
            _serviceName = serviceName;
        }

        public async Task<Session> CreateSessionAsync(Solution solution, object callbackTarget = null, CancellationToken cancellationToken = default)
        {
            if (solution == null)
            {
                // keep old behavior for Razor
                return null;
            }

            var innerSession = await _client.TryCreateSessionAsync(_serviceName, solution, callbackTarget, cancellationToken).ConfigureAwait(false);
            if (innerSession == null)
            {
                return null;
            }

            return new Session(innerSession);
        }

        public sealed class Session : IDisposable
        {
            private readonly SessionWithSolution _inner;

            internal Session(SessionWithSolution inner)
            {
                _inner = inner;
            }

            public Task InvokeAsync(string targetName, IReadOnlyList<object> arguments, CancellationToken cancellationToken)
            {
                return _inner.Connection.InvokeAsync(targetName, arguments, cancellationToken);
            }

            public Task<T> InvokeAsync<T>(string targetName, IReadOnlyList<object> arguments, CancellationToken cancellationToken)
            {
                return _inner.Connection.InvokeAsync<T>(targetName, arguments, cancellationToken);
            }

            public void Dispose()
            {
                _inner.Dispose();
            }
        }
    }
}
