// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Remote;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Remote
{
    internal class JsonRpcConnection : RemoteHostClient.Connection
    {
        // communication channel related to service information
        private readonly ServiceJsonRpcEx _serviceRpc;

        // communication channel related to snapshot information
        private readonly ReferenceCountedDisposable<RemotableDataJsonRpc> _remoteDataRpc;

        public JsonRpcConnection(
            TraceSource logger,
            object callbackTarget,
            Stream serviceStream,
            ReferenceCountedDisposable<RemotableDataJsonRpc> dataRpc)
        {
            Contract.ThrowIfNull(dataRpc);

            _serviceRpc = new ServiceJsonRpcEx(dataRpc.Target.Workspace, logger, serviceStream, callbackTarget);
            _remoteDataRpc = dataRpc;
        }

        public override Task InvokeAsync(string targetName, IReadOnlyList<object> arguments, CancellationToken cancellationToken)
        {
            return _serviceRpc.InvokeAsync(targetName, arguments, cancellationToken);
        }

        public override Task<T> InvokeAsync<T>(string targetName, IReadOnlyList<object> arguments, CancellationToken cancellationToken)
        {
            return _serviceRpc.InvokeAsync<T>(targetName, arguments, cancellationToken);
        }

        public override Task InvokeAsync(string targetName, IReadOnlyList<object> arguments, Func<Stream, CancellationToken, Task> funcWithDirectStreamAsync, CancellationToken cancellationToken)
        {
            return _serviceRpc.InvokeAsync(targetName, arguments, funcWithDirectStreamAsync, cancellationToken);
        }

        public override Task<T> InvokeAsync<T>(string targetName, IReadOnlyList<object> arguments, Func<Stream, CancellationToken, Task<T>> funcWithDirectStreamAsync, CancellationToken cancellationToken)
        {
            return _serviceRpc.InvokeAsync<T>(targetName, arguments, funcWithDirectStreamAsync, cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // dispose service and snapshot channels
                _serviceRpc.Dispose();
                _remoteDataRpc.Dispose();
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Communication channel between VS feature and roslyn service in remote host.
        /// 
        /// this is the channel consumer of remote host client will playing with
        /// </summary>
        private sealed class ServiceJsonRpcEx : JsonRpcEx
        {
            private readonly object _callbackTarget;

            public ServiceJsonRpcEx(Workspace workspace, TraceSource logger, Stream stream, object callbackTarget)
                : base(workspace, logger, stream, callbackTarget, useThisAsCallback: false)
            {
                // this one doesn't need cancellation token since it has nothing to cancel
                _callbackTarget = callbackTarget;

                StartListening();
            }

            protected override void Dispose(bool disposing)
            {
                Contract.ThrowIfFalse(disposing);
                Disconnect();
            }
        }
    }
}
