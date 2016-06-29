// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using StreamJsonRpc;
using System;
using System.IO;
using System.Threading;

namespace Microsoft.CodeAnalysis.Remote
{
    internal abstract class ServiceHubJsonRpcServiceBase : ServiceHubServiceBase
    {
        private readonly CancellationTokenSource _source;

        protected readonly JsonRpc Rpc;
        protected readonly CancellationToken Token;

        public ServiceHubJsonRpcServiceBase(Stream stream, IServiceProvider serviceProvider) : base(stream, serviceProvider)
        {
            _source = new CancellationTokenSource();
            Token = _source.Token;

            Rpc = JsonRpc.Attach(stream, this);
            Rpc.Disconnected += OnRpcDisconnected;
        }

        protected virtual void OnDisconnected(JsonRpcDisconnectedEventArgs e)
        {
            // do nothing
        }

        private void OnRpcDisconnected(object sender, JsonRpcDisconnectedEventArgs e)
        {
            // raise cancellation
            _source.Cancel();

            OnDisconnected(e);

            if (e.Reason != DisconnectedReason.Disposed)
            {
                LogError($"Client stream disconnected unexpectedly: {e.Exception?.GetType().Name} {e.Exception?.Message}");
            }
        }
    }
}
