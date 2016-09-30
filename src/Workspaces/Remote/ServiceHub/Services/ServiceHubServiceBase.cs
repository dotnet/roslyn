// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.Remote
{
    // TODO: all service hub service should be extract to interface so that it can support multiple hosts.
    //       right now, tightly coupled to service hub
    internal abstract class ServiceHubServiceBase : IDisposable
    {
        private static int s_instanceId;

        private readonly CancellationTokenSource _cancellationTokenSource;

        protected readonly JsonRpc Rpc;
        protected readonly CancellationToken CancellationToken;
        protected readonly int InstanceId;
        protected readonly TraceSource Logger;
        protected readonly Stream Stream;

        protected ServiceHubServiceBase(Stream stream, IServiceProvider serviceProvider)
        {
            InstanceId = Interlocked.Add(ref s_instanceId, 1);

            Logger = (TraceSource)serviceProvider.GetService(typeof(TraceSource));
            Logger.TraceInformation($"{DebugInstanceString} Service instance created");

            Stream = stream;

            _cancellationTokenSource = new CancellationTokenSource();
            CancellationToken = _cancellationTokenSource.Token;

            Rpc = JsonRpc.Attach(stream, this);
            Rpc.Disconnected += OnRpcDisconnected;
        }

        protected string DebugInstanceString => $"{GetType()} ({InstanceId})";

        protected virtual void Dispose(bool disposing)
        {
            // do nothing here
        }

        protected void LogError(string message)
        {
            Logger.TraceEvent(TraceEventType.Error, 0, $"{DebugInstanceString} : " + message);
        }

        public void Dispose()
        {
            Rpc.Dispose();
            Stream.Dispose();

            Dispose(false);

            Logger.TraceInformation($"{DebugInstanceString} Service instance disposed");
        }

        protected virtual void OnDisconnected(JsonRpcDisconnectedEventArgs e)
        {
            // do nothing
        }

        private void OnRpcDisconnected(object sender, JsonRpcDisconnectedEventArgs e)
        {
            // raise cancellation
            _cancellationTokenSource.Cancel();

            OnDisconnected(e);

            if (e.Reason != DisconnectedReason.Disposed)
            {
                LogError($"Client stream disconnected unexpectedly: {e.Exception?.GetType().Name} {e.Exception?.Message}");
            }
        }
    }
}