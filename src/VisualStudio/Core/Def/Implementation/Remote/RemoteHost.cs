// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.ServiceHub.Client;
using Roslyn.Utilities;
using StreamJsonRpc;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Remote
{
    // TODO: this should be abstracted out so that it is not tied to Servicehub
    internal class RemoteHost
    {
        public static RemoteHost Instance;

        private readonly HubClient _primary;
        private readonly JsonRpc _rpc;

        public static Task StartAsync(CancellationToken cancellationToken)
        {
            if (Instance != null)
            {
                return SpecializedTasks.EmptyTask;
            }

            return Task.Run(() => StartInternalAsync(cancellationToken), cancellationToken);
        }

        private static async Task StartInternalAsync(CancellationToken cancellationToken)
        {
            var primary = new HubClient("Primary");
            var remoteHostStream = await primary.RequestServiceAsync("remotehostService", cancellationToken).ConfigureAwait(false);

            if (Interlocked.CompareExchange(ref Instance, new RemoteHost(primary, remoteHostStream), null) == null)
            {
                // this is the winner in case there is ever a race

                // TODO: add better logic here such as making system to prefer out of proc rather than in proc operations.

                // make sure connection is done right
                var current = $"VS ({Process.GetCurrentProcess().Id})";
                var host = await Instance.InvokeAsync<string>("Connect", current).ConfigureAwait(false);

                // TODO: change this to non fatal watson and make VS to use inproc implementation
                Contract.ThrowIfFalse(host == current.ToString());
            }
        }

        private RemoteHost(HubClient primary, Stream stream)
        {
            _primary = primary;
            _rpc = JsonRpc.Attach(stream, this);

            // handle disconnected situation
            _rpc.Disconnected += this.OnRpcDisconnected;
        }

        public async Task<IDisposable> SynchronizeAsync(SolutionSnapshot snapshot, CancellationToken cancellationToken)
        {
            // TODO: this should move into SolutionSnapshot once abstraction is done. for now separate call
            return new Disposable(this, snapshot, await _primary.RequestServiceAsync("solutionSnapshotService", cancellationToken).ConfigureAwait(false));
        }

        private Task<T> InvokeAsync<T>(string targetName, params object[] arguments)
        {
            return _rpc.InvokeAsync<T>(targetName, arguments);
        }

        private void OnRpcDisconnected(object sender, JsonRpcDisconnectedEventArgs e)
        {
            // TODO: make this logic better by making sure we don't endlessly retry to
            //       get out of proc connection and make sure when we failed to make connection,
            //       we chance operation to use in proc implementation
            if (Interlocked.CompareExchange(ref Instance, null, this) == this)
            {
                // re-start remote host
                var dummy = StartInternalAsync(CancellationToken.None);
            }
        }

        private class Disposable : IDisposable
        {
            private readonly RemoteHost _owner;
            private readonly SolutionSnapshot _snapshot;
            private readonly JsonRpc _rpc;

            public Disposable(RemoteHost owner, SolutionSnapshot snapshot, Stream stream)
            {
                // TODO: once this move to MEF, this whole thing should move into SolutionSnapshot
                _owner = owner;
                _snapshot = snapshot;
                _rpc = JsonRpc.Attach(stream, this);
            }

            public void Request(int serviceId, int requestId, byte[] checksum)
            {
                // run this in its own task so that it is detached from Rpc channel
                var dummy = Task.Run(() => SendAssetOverAsync(serviceId, requestId, checksum));
            }

            private async Task SendAssetOverAsync(int serviceId, int requestId, byte[] checksum)
            {
                // this work is independent to outter Rpc being closed. once started. it is now
                // detached from Rpc channel
                var service = _snapshot.Workspace.Services.GetRequiredService<ISolutionSnapshotService>();

                // REVIEW: service hub control flow is a bit wierd. stream is the important one.
                // TODO: figure out how to deal with cancellation
                using (var stream = await _owner._primary.RequestServiceAsync("assetService").ConfigureAwait(false))
                using (var buffer = new BufferedStream(stream, 256 * 1024))
                using (var writer = new ObjectWriter(stream))
                {
                    writer.WriteInt32(serviceId);
                    writer.WriteInt32(requestId);
                    writer.WriteArray(checksum);

                    var checksumObject = await service.GetChecksumObjectAsync(new Checksum(ImmutableArray.Create(checksum)), CancellationToken.None).ConfigureAwait(false);
                    writer.WriteString(checksumObject.Kind);

                    await checksumObject.WriteToAsync(writer, CancellationToken.None).ConfigureAwait(false);
                }
            }

            public void Dispose()
            {
                // we don't care about when this actually run.
                // make sure we send "done", and close the stream.
                _rpc.InvokeAsync("Done").SafeContinueWith(_ => _rpc.Dispose(), TaskScheduler.Default);
            }
        }
    }
}
