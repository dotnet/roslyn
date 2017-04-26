// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using StreamJsonRpc;
using Microsoft.CodeAnalysis.Remote;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Remote
{
    /// <summary>
    /// Helper type that abstract out JsonRpc communication with extra capability of
    /// using raw stream to move over big chunk of data
    /// </summary>
    internal class JsonRpcClient : IDisposable
    {
        private readonly JsonRpc _rpc;

        public JsonRpcClient(Stream stream, object callbackTarget, bool useThisAsCallback)
        {
            Contract.Requires(stream != null);

            var target = useThisAsCallback ? this : callbackTarget;

            _rpc = new JsonRpc(new JsonRpcMessageHandler(stream, stream), target);
            _rpc.JsonSerializer.Converters.Add(AggregateJsonConverter.Instance);

            _rpc.Disconnected += OnDisconnected;
        }

        public Task InvokeWithCancellationAsync(string targetName, object[] arguments, CancellationToken cancellationToken)
        {
            return _rpc.InvokeWithCancellationAsync(targetName, arguments, cancellationToken);
        }

        public Task<T> InvokeWithCancellationAsync<T>(string targetName, object[] arguments, CancellationToken cancellationToken)
        {
            return _rpc.InvokeWithCancellationAsync<T>(targetName, arguments, cancellationToken);
        }

        public Task InvokeWithCancellationAsync(string targetName, IEnumerable<object> arguments, Func<Stream, CancellationToken, Task> funcWithDirectStreamAsync, CancellationToken cancellationToken)
        {
            return Extensions.InvokeWithCancellationAsync(_rpc, targetName, arguments, funcWithDirectStreamAsync, cancellationToken);
        }

        public Task<T> InvokeWithCancellationAsync<T>(string targetName, IEnumerable<object> arguments, Func<Stream, CancellationToken, Task<T>> funcWithDirectStreamAsync, CancellationToken cancellationToken)
        {
            return Extensions.InvokeWithCancellationAsync(_rpc, targetName, arguments, funcWithDirectStreamAsync, cancellationToken);
        }

        public void Dispose()
        {
            OnDisposed();

            _rpc.Dispose();
        }

        protected void StartListening()
        {
            // due to this issue - https://github.com/dotnet/roslyn/issues/16900#issuecomment-277378950
            // _rpc need to be explicitly started
            _rpc.StartListening();
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
