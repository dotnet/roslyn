// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Execution;
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

        // close connection when cancellation has raised
        private readonly CancellationTokenRegistration _cancellationRegistration;

        public JsonRpcConnection(
            object callbackTarget,
            Stream serviceStream,
            ReferenceCountedDisposable<RemotableDataJsonRpc> dataRpc,
            CancellationToken cancellationToken) :
            base(cancellationToken)
        {
            _serviceRpc = new ServiceJsonRpcEx(serviceStream, callbackTarget, cancellationToken);
            _remoteDataRpc = dataRpc;

            // dispose session when cancellation has raised
            _cancellationRegistration = CancellationToken.Register(Dispose);
        }

        protected override async Task OnRegisterPinnedRemotableDataScopeAsync(PinnedRemotableDataScope scope)
        {
            await _serviceRpc.InvokeAsync(WellKnownServiceHubServices.ServiceHubServiceBase_Initialize, scope.SolutionInfo).ConfigureAwait(false);
        }

        public override Task InvokeAsync(string targetName, params object[] arguments)
        {
            return _serviceRpc.InvokeAsync(targetName, arguments);
        }

        public override Task<T> InvokeAsync<T>(string targetName, params object[] arguments)
        {
            return _serviceRpc.InvokeAsync<T>(targetName, arguments);
        }

        public override Task InvokeAsync(string targetName, IEnumerable<object> arguments, Func<Stream, CancellationToken, Task> funcWithDirectStreamAsync)
        {
            return _serviceRpc.InvokeAsync(targetName, arguments, funcWithDirectStreamAsync);
        }

        public override Task<T> InvokeAsync<T>(string targetName, IEnumerable<object> arguments, Func<Stream, CancellationToken, Task<T>> funcWithDirectStreamAsync)
        {
            return _serviceRpc.InvokeAsync<T>(targetName, arguments, funcWithDirectStreamAsync);
        }

        protected override void OnDisposed()
        {
            base.OnDisposed();

            // dispose cancellation registration
            _cancellationRegistration.Dispose();

            // dispose service and snapshot channels
            _serviceRpc.Dispose();
            _remoteDataRpc.Dispose();
        }

        /// <summary>
        /// Communication channel between VS feature and roslyn service in remote host.
        /// 
        /// this is the channel consumer of remote host client will playing with
        /// </summary>
        private class ServiceJsonRpcEx : JsonRpcEx
        {
            private readonly object _callbackTarget;

            public ServiceJsonRpcEx(Stream stream, object callbackTarget, CancellationToken cancellationToken)
                : base(stream, callbackTarget, useThisAsCallback: false, cancellationToken: cancellationToken)
            {
                // this one doesn't need cancellation token since it has nothing to cancel
                _callbackTarget = callbackTarget;

                StartListening();
            }

            protected override void Dispose(bool disposing)
            {
                Disconnect();
            }
        }
    }
}