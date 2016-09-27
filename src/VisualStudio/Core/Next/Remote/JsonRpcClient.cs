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
        private readonly Stream _stream;
        private readonly JsonRpc _rpc;

        public JsonRpcClient(Stream stream, object callbackTarget, bool useThisAsCallback)
        {
            _stream = stream;

            var target = useThisAsCallback ? this : callbackTarget;
            _rpc = JsonRpc.Attach(stream, target);
            _rpc.Disconnected += OnDisconnected;
        }

        public Task InvokeAsync(string targetName, params object[] arguments)
        {
            return _rpc.InvokeAsync(targetName, arguments);
        }

        public Task<T> InvokeAsync<T>(string targetName, params object[] arguments)
        {
            return _rpc.InvokeAsync<T>(targetName, arguments);
        }

        public Task InvokeAsync(string targetName, IEnumerable<object> arguments, Func<Stream, CancellationToken, Task> funcWithDirectStreamAsync, CancellationToken cancellationToken)
        {
            return Extensions.InvokeAsync(_rpc, targetName, arguments, funcWithDirectStreamAsync, cancellationToken);
        }

        public Task<T> InvokeAsync<T>(string targetName, IEnumerable<object> arguments, Func<Stream, CancellationToken, Task<T>> funcWithDirectStreamAsync, CancellationToken cancellationToken)
        {
            return Extensions.InvokeAsync(_rpc, targetName, arguments, funcWithDirectStreamAsync, cancellationToken);
        }

        public void Dispose()
        {
            OnDisposed();

            _rpc.Dispose();
            _stream.Dispose();
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
