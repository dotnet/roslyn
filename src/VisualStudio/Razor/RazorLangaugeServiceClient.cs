// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.VisualStudio.LanguageServices.Razor
{
    internal sealed class RazorLangaugeServiceClient
    {
        private const string RazorServiceName = "razorLanguageService";

        private readonly RemoteHostClient _client;

        internal RazorLangaugeServiceClient(RemoteHostClient client)
        {
            _client = client;
        }

        public async Task<Session> CreateSessionAsync(Solution solution, object callbackTarget = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var innerSession = await _client.TryCreateServiceSessionAsync(RazorServiceName, solution, callbackTarget, cancellationToken).ConfigureAwait(false);
            if (innerSession == null)
            {
                return null;
            }

            return new Session(innerSession);
        }

        public sealed class Session : IDisposable
        {
            private readonly RemoteHostClient.Session _inner;

            internal Session(RemoteHostClient.Session inner)
            {
                _inner = inner;
            }

            public Task InvokeWithCancellationAsync(string targetName, object[] arguments, CancellationToken cancellationToken)
            {
                return _inner.InvokeWithCancellationAsync(targetName, arguments, cancellationToken);
            }

            public Task<T> InvokeWithCancellationAsync<T>(string targetName, object[] arguments, CancellationToken cancellationToken)
            {
                return _inner.InvokeWithCancellationAsync<T>(targetName, arguments, cancellationToken);
            }

            public Task InvokeWithCancellationAsync(string targetName, IEnumerable<object> arguments, Func<Stream, CancellationToken, Task> funcWithDirectStreamAsync, CancellationToken cancellationToken)
            {
                return _inner.InvokeWithCancellationAsync(targetName, arguments, funcWithDirectStreamAsync, cancellationToken);
            }

            public Task<T> InvokeAsync<T>(string targetName, IEnumerable<object> arguments, Func<Stream, CancellationToken, Task<T>> funcWithDirectStreamAsync, CancellationToken cancellationToken)
            {
                return _inner.InvokeWithCancellationAsync<T>(targetName, arguments, funcWithDirectStreamAsync, cancellationToken);
            }

            public void Dispose()
            {
                _inner.Dispose();
            }
        }
    }
}
