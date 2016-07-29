﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Remote;
using Roslyn.Utilities;
using StreamJsonRpc;

namespace Microsoft.VisualStudio.LanguageServices.Remote
{
    internal partial class ServiceHubRemoteHostClient
    {
        private class JsonRpcSession : Session
        {
            // communication channel related to snapshot information
            private readonly SnapshotJsonRpcClient _snapshotClient;

            // communication channel related to service information
            private readonly JsonRpcClient _serviceClient;

            // close connection when cancellation has raised
            private readonly CancellationTokenRegistration _cancellationRegistration;

            public JsonRpcSession(
                ChecksumScope snapshot,
                Stream snapshotStream,
                object callbackTarget,
                Stream serviceStream,
                CancellationToken cancellationToken) :
                base(snapshot, cancellationToken)
            {
                _snapshotClient = new SnapshotJsonRpcClient(this, snapshotStream);
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

                // dispose service and snapshot channels
                _serviceClient.Dispose();
                _snapshotClient.Dispose();
            }

            private class ServiceJsonRpcClient : JsonRpcClient
            {
                private readonly object _callbackTarget;

                public ServiceJsonRpcClient(Stream stream, object callbackTarget) : base(stream)
                {
                    // this one doesn't need cancellation token since it has nothing to cancel
                    _callbackTarget = callbackTarget;
                }

                protected override object GetTarget() => _callbackTarget;
            }

            private class SnapshotJsonRpcClient : JsonRpcClient
            {
                private readonly JsonRpcSession _owner;
                private readonly CancellationTokenSource _source;

                public SnapshotJsonRpcClient(JsonRpcSession owner, Stream stream) :
                    base(stream)
                {
                    _owner = owner;
                    _source = new CancellationTokenSource();
                }

                private ChecksumScope ChecksumScope => _owner.ChecksumScope;
                protected override object GetTarget() => this;

                public async Task RequestAssetAsync(int serviceId, byte[] checksum, string streamName)
                {
                    try
                    {
                        // this is callback from remote host side to get asset associated with checksum from VS.
                        var service = ChecksumScope.Workspace.Services.GetRequiredService<ISolutionChecksumService>();

                        using (var stream = new ClientDirectStream(streamName))
                        {
                            await stream.ConnectAsync(_source.Token).ConfigureAwait(false);

                            using (var writer = new ObjectWriter(stream))
                            {
                                writer.WriteInt32(serviceId);
                                writer.WriteArray(checksum);

                                var checksumObject = service.GetChecksumObject(new Checksum(checksum), _source.Token);
                                writer.WriteString(checksumObject.Kind);

                                await checksumObject.WriteToAsync(writer, _source.Token).ConfigureAwait(false);
                            }

                            await stream.FlushAsync(_source.Token).ConfigureAwait(false);

                            // TODO: think of a way this is not needed
                            // wait for the other side to finish reading data I sent over
                            stream.WaitForServer();
                        }
                    }
                    catch (IOException)
                    {
                        // remote host side is cancelled (client stream connection is closed)
                        // can happen if pinned solution scope is disposed
                    }
                    catch (OperationCanceledException)
                    {
                        // rpc connection is closed. 
                        // can happen if pinned solution scope is disposed
                    }
                }

                protected override void OnDisconnected(object sender, JsonRpcDisconnectedEventArgs e)
                {
                    _source.Cancel();
                }
            }
        }
    }
}
