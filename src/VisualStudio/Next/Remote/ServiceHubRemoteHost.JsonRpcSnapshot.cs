// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            private readonly CancellationTokenSource _source;
            private readonly Stream _stream;
            private readonly JsonRpc _rpc;

            public JsonRpcSnapshotSession(SolutionSnapshot snapshot, Stream stream) : base(snapshot)
            {
                _source = new CancellationTokenSource();
                _stream = stream;

                _rpc = JsonRpc.Attach(stream, this);
                _rpc.Disconnected += OnDisconnected;
            }

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
                    // wait for the other side to done reading data I sent over
                    stream.WaitForMaster();
                }

                Debug.WriteLine(stopWatch.Elapsed);
            }

            public override void Dispose()
            {
                _source.Cancel();

                // we don't care about when this actually run.
                // make sure we send "done", and close the stream.
                _rpc.InvokeAsync(WellKnownServiceHubServices.SolutionSnapshotService_Done)
                    .SafeContinueWith(_ =>
                    {
                        _rpc.Dispose();
                        _stream.Dispose();
                    }, TaskScheduler.Default);
            }

            private void OnDisconnected(object sender, JsonRpcDisconnectedEventArgs e)
            {
                _source.Cancel();
            }
        }
    }
}
