// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Remote;
using Roslyn.Utilities;
using StreamJsonRpc;

namespace Microsoft.VisualStudio.LanguageServices.Remote
{
    internal partial class ServiceHubRemoteHost
    {
        private class JsonRpcSnapshotSession : Session
        {
            // communication channel related to snapshot information
            private readonly SnapshotJsonRpcClient _snapshotClient;

            // communication channel related to service information
            private readonly JsonRpcClient _serviceClient;

            public JsonRpcSnapshotSession(SolutionSnapshot snapshot, Stream snapshotStream, object callbackTarget, Stream serviceStream) : base(snapshot)
            {
                _snapshotClient = new SnapshotJsonRpcClient(this, snapshotStream);
                _serviceClient = new JsonRpcClient(serviceStream, callbackTarget);
            }

            public override Task InvokeAsync(string targetName, params object[] arguments)
            {
                return _serviceClient.InvokeAsync(targetName, arguments);
            }

            public override Task<T> InvokeAsync<T>(string targetName, params object[] arguments)
            {
                return _serviceClient.InvokeAsync<T>(targetName, arguments);
            }

            public override Task InvokeAsync(string targetName, IEnumerable<object> arguments, Func<Stream, CancellationToken, Task> funcWithDirectStreamAsync, CancellationToken cancellationToken)
            {
                return _serviceClient.InvokeAsync(targetName, arguments, funcWithDirectStreamAsync, cancellationToken);
            }

            public override Task<T> InvokeAsync<T>(string targetName, IEnumerable<object> arguments, Func<Stream, CancellationToken, Task<T>> funcWithDirectStreamAsync, CancellationToken cancellationToken)
            {
                return _serviceClient.InvokeAsync<T>(targetName, arguments, funcWithDirectStreamAsync, cancellationToken);
            }

            public override void Dispose()
            {
                _snapshotClient.RaiseCancellation();

                // dispose service channel first
                _serviceClient.Dispose();

                // we don't care about when this actually run.
                // make sure we send "done", and close the stream.
                _snapshotClient.InvokeAsync(WellKnownServiceHubServices.SolutionSnapshotService_Done)
                    .SafeContinueWith(_ =>
                    {
                        _snapshotClient.Dispose();
                    }, TaskScheduler.Default);
            }

            private class SnapshotJsonRpcClient : JsonRpcClient
            {
                private readonly JsonRpcSnapshotSession _owner;
                private readonly CancellationTokenSource _source;

                public SnapshotJsonRpcClient(JsonRpcSnapshotSession owner, Stream stream) :
                    base(stream)
                {
                    _owner = owner;
                    _source = new CancellationTokenSource();
                }

                private SolutionSnapshot SolutionSnapshot => _owner.SolutionSnapshot;

                public async Task RequestAssetAsync(int serviceId, byte[] checksum, string pipeName)
                {
                    var stopWatch = Stopwatch.StartNew();

                    var service = SolutionSnapshot.Workspace.Services.GetRequiredService<ISolutionSnapshotService>();

                    using (var stream = new SlaveDirectStream(pipeName))
                    {
                        await stream.ConnectAsync(_source.Token).ConfigureAwait(false);

                        using (var writer = new ObjectWriter(stream))
                        {
                            writer.WriteInt32(serviceId);
                            writer.WriteArray(checksum);

                            var checksumObject = await service.GetChecksumObjectAsync(new Checksum(ImmutableArray.Create(checksum)), _source.Token).ConfigureAwait(false);
                            writer.WriteString(checksumObject.Kind);

                            Debug.WriteLine(checksumObject.Kind);

                            await checksumObject.WriteToAsync(writer, _source.Token).ConfigureAwait(false);
                        }

                        await stream.FlushAsync(_source.Token).ConfigureAwait(false);

                        // TODO: think of a way this is not needed
                        // wait for the other side to finish reading data I sent over
                        stream.WaitForMaster();
                    }

                    Debug.WriteLine(stopWatch.Elapsed);
                }

                public void RaiseCancellation()
                {
                    _source.Cancel();
                }

                protected override void OnDisconnected(object sender, JsonRpcDisconnectedEventArgs e)
                {
                    RaiseCancellation();
                }
            }
        }
    }
}
