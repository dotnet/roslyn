// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Execution;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Remote
{
    internal partial class ServiceHubRemoteHostClient
    {
        private class JsonRpcSession : Session
        {
            // communication channel related to service information
            private readonly ServiceJsonRpcClient _serviceClient;

            // close connection when cancellation has raised
            private readonly CancellationTokenRegistration _cancellationRegistration;

            public JsonRpcSession(
                ChecksumScope snapshot,
                object callbackTarget,
                Stream serviceStream,
                CancellationToken cancellationToken) :
                base(snapshot, cancellationToken)
            {
                _serviceClient = new ServiceJsonRpcClient(serviceStream, callbackTarget);

                // dispose session when cancellation has raised
                _cancellationRegistration = CancellationToken.Register(Dispose);
            }

            public override Task InvokeAsync(string targetName, params object[] arguments)
            {
                CancellationToken.ThrowIfCancellationRequested();

                return _serviceClient.InvokeAsync(targetName, arguments.Concat(ChecksumScope.SolutionChecksum.Checksum.ToArray()).ToArray());
            }

            public override Task<T> InvokeAsync<T>(string targetName, params object[] arguments)
            {
                CancellationToken.ThrowIfCancellationRequested();

                return _serviceClient.InvokeAsync<T>(targetName, arguments.Concat(ChecksumScope.SolutionChecksum.Checksum.ToArray()).ToArray());
            }

            public override Task InvokeAsync(string targetName, IEnumerable<object> arguments, Func<Stream, CancellationToken, Task> funcWithDirectStreamAsync)
            {
                CancellationToken.ThrowIfCancellationRequested();

                return _serviceClient.InvokeAsync(targetName, arguments.Concat(ChecksumScope.SolutionChecksum.Checksum.ToArray()).ToArray(), funcWithDirectStreamAsync, CancellationToken);
            }

            public override Task<T> InvokeAsync<T>(string targetName, IEnumerable<object> arguments, Func<Stream, CancellationToken, Task<T>> funcWithDirectStreamAsync)
            {
                CancellationToken.ThrowIfCancellationRequested();

                return _serviceClient.InvokeAsync<T>(targetName, arguments.Concat(ChecksumScope.SolutionChecksum.Checksum.ToArray()).ToArray(), funcWithDirectStreamAsync, CancellationToken);
            }

            protected override void OnDisposed()
            {
                // dispose cancellation registration
                _cancellationRegistration.Dispose();

                // dispose service channels
                _serviceClient.Dispose();
            }

            /// <summary>
            /// Communication channel between VS feature and roslyn service in remote host.
            /// 
            /// this is the channel consumer of remote host client will playing with
            /// </summary>
            private class ServiceJsonRpcClient : JsonRpcClient
            {
                private readonly object _callbackTarget;

                public ServiceJsonRpcClient(Stream stream, object callbackTarget) : base(stream)
                {
                    // this one doesn't need cancellation token since it has nothing to cancel
                    _callbackTarget = callbackTarget;
                }

                protected override object GetCallbackTarget() => _callbackTarget;
            }
        }
    }
}
