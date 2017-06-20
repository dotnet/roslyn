// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.VisualStudio.LanguageServices.Remote
{
    internal class ServiceHubJsonRpcConnection : RemoteHostClient.Connection
    {
        // communication channel related to service information
        private readonly ServiceJsonRpcClient _serviceClient;

        // communication channel related to snapshot information
        private readonly RemotableDataJsonRpcEx _remoteDataRpc;

        // close connection when cancellation has raised
        private readonly CancellationTokenRegistration _cancellationRegistration;

        public ServiceHubJsonRpcConnection(
            object callbackTarget,
            Stream serviceStream,
            RemotableDataJsonRpcEx dataRpc,
            CancellationToken cancellationToken) :
            base(cancellationToken)
        {
            _serviceClient = new ServiceJsonRpcClient(serviceStream, callbackTarget, cancellationToken);
            _remoteDataRpc = dataRpc;

            // dispose session when cancellation has raised
            _cancellationRegistration = CancellationToken.Register(Dispose);
        }

        protected override async Task OnRegisterPinnedRemotableDataScopeAsync(PinnedRemotableDataScope scope)
        {
            await _serviceClient.InvokeAsync(WellKnownServiceHubServices.ServiceHubServiceBase_Initialize, scope.SolutionInfo).ConfigureAwait(false);
        }

        public override Task InvokeAsync(string targetName, params object[] arguments)
        {
            return _serviceClient.InvokeAsync(targetName, arguments);
        }

        public override Task<T> InvokeAsync<T>(string targetName, params object[] arguments)
        {
            return _serviceClient.InvokeAsync<T>(targetName, arguments);
        }

        public override Task InvokeAsync(string targetName, IEnumerable<object> arguments, Func<Stream, CancellationToken, Task> funcWithDirectStreamAsync)
        {
            return _serviceClient.InvokeAsync(targetName, arguments, funcWithDirectStreamAsync);
        }

        public override Task<T> InvokeAsync<T>(string targetName, IEnumerable<object> arguments, Func<Stream, CancellationToken, Task<T>> funcWithDirectStreamAsync)
        {
            return _serviceClient.InvokeAsync<T>(targetName, arguments, funcWithDirectStreamAsync);
        }

        protected override void OnDisposed()
        {
            base.OnDisposed();

            // dispose cancellation registration
            _cancellationRegistration.Dispose();

            // dispose service and snapshot channels
            _serviceClient.Dispose();
            _remoteDataRpc.Dispose();
        }

        /// <summary>
        /// Communication channel between VS feature and roslyn service in remote host.
        /// 
        /// this is the channel consumer of remote host client will playing with
        /// </summary>
        private class ServiceJsonRpcClient : JsonRpcEx
        {
            private readonly object _callbackTarget;

            public ServiceJsonRpcClient(Stream stream, object callbackTarget, CancellationToken cancellationToken)
                : base(stream, callbackTarget, useThisAsCallback: false, cancellationToken: cancellationToken)
            {
                // this one doesn't need cancellation token since it has nothing to cancel
                _callbackTarget = callbackTarget;

                StartListening();
            }

            public override void Dispose()
            {
                Disconnect();
            }
        }
    }
}