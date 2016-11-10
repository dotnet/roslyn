// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using StreamJsonRpc;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.VisualStudio.LanguageServices.Remote
{
    /// <summary>
    /// Helper type that abstract out JsonRpc communication with extra capability of
    /// using raw stream to move over big chunk of data
    /// </summary>
    internal class JsonRpcClient : IDisposable
    {
        private readonly JsonRpc _rpc;
        private readonly CancellationToken _cancellationToken;

        public JsonRpcClient(
            Stream stream, object callbackTarget, bool useThisAsCallback, CancellationToken cancellationToken)
        {
            var target = useThisAsCallback ? this : callbackTarget;
            _cancellationToken = cancellationToken;

            _rpc = JsonRpc.Attach(stream, target);
            _rpc.Disconnected += OnDisconnected;
        }

        public async Task InvokeAsync(string targetName, params object[] arguments)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await _rpc.InvokeAsync(targetName, arguments).ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
                // object disposed exception can be thrown from StreamJsonRpc if JsonRpc is disposed in the middle of read/write.
                // the way we added cancellation support to the JsonRpc which doesn't support cancellation natively
                // can cause this exception to happen. newer version supports cancellation token natively, but
                // we can't use it now, so we will catch object disposed exception and check cancellation token
                _cancellationToken.ThrowIfCancellationRequested();
                throw;
            }
        }

        public async Task<T> InvokeAsync<T>(string targetName, params object[] arguments)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return await _rpc.InvokeAsync<T>(targetName, arguments).ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
                // object disposed exception can be thrown from StreamJsonRpc if JsonRpc is disposed in the middle of read/write.
                // the way we added cancellation support to the JsonRpc which doesn't support cancellation natively
                // can cause this exception to happen. newer version supports cancellation token natively, but
                // we can't use it now, so we will catch object disposed exception and check cancellation token
                _cancellationToken.ThrowIfCancellationRequested();
                throw;
            }
        }

        public Task InvokeAsync(string targetName, IEnumerable<object> arguments, Func<Stream, CancellationToken, Task> funcWithDirectStreamAsync)
        {
            return Extensions.InvokeAsync(_rpc, targetName, arguments, funcWithDirectStreamAsync, _cancellationToken);
        }

        public Task<T> InvokeAsync<T>(string targetName, IEnumerable<object> arguments, Func<Stream, CancellationToken, Task<T>> funcWithDirectStreamAsync)
        {
            return Extensions.InvokeAsync(_rpc, targetName, arguments, funcWithDirectStreamAsync, _cancellationToken);
        }

        public void Dispose()
        {
            OnDisposed();

            _rpc.Dispose();
        }

        protected virtual void OnDisposed()
        {
            // do nothing
        }

        protected virtual void OnDisconnected(object sender, JsonRpcDisconnectedEventArgs e)
        {
            // do nothing
        }
    }
}
