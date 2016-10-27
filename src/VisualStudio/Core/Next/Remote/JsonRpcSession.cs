// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Remote;
using Roslyn.Utilities;
using StreamJsonRpc;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.VisualStudio.LanguageServices.Remote
{
    internal class JsonRpcSession : RemoteHostClient.Session
    {
        private static int s_sessionId = 0;

        // current session id
        private readonly int _currentSessionId;

        // communication channel related to snapshot information
        private readonly SnapshotJsonRpcClient _snapshotClient;

        // communication channel related to service information
        private readonly ServiceJsonRpcClient _serviceClient;

        // close connection when cancellation has raised
        private readonly CancellationTokenRegistration _cancellationRegistration;

        public static async Task<JsonRpcSession> CreateAsync(
            PinnedRemotableDataScope snapshot,
            Stream snapshotStream,
            object callbackTarget,
            Stream serviceStream,
            CancellationToken cancellationToken)
        {
            var session = new JsonRpcSession(snapshot, snapshotStream, callbackTarget, serviceStream, cancellationToken);

            await session.InitializeAsync().ConfigureAwait(false);

            return session;
        }

        private JsonRpcSession(
            PinnedRemotableDataScope snapshot,
            Stream snapshotStream,
            object callbackTarget,
            Stream serviceStream,
            CancellationToken cancellationToken) :
            base(snapshot, cancellationToken)
        {
            // get session id
            _currentSessionId = Interlocked.Increment(ref s_sessionId);

            _snapshotClient = new SnapshotJsonRpcClient(this, snapshotStream);
            _serviceClient = new ServiceJsonRpcClient(serviceStream, callbackTarget);

            // dispose session when cancellation has raised
            _cancellationRegistration = CancellationToken.Register(Dispose);
        }

        private async Task InitializeAsync()
        {
            // all roslyn remote service must based on ServiceHubServiceBase which implements Initialize method
            await _snapshotClient.InvokeAsync(WellKnownServiceHubServices.ServiceHubServiceBase_Initialize, _currentSessionId, PinnedScope.SolutionChecksum.ToArray()).ConfigureAwait(false);
            await _serviceClient.InvokeAsync(WellKnownServiceHubServices.ServiceHubServiceBase_Initialize, _currentSessionId, PinnedScope.SolutionChecksum.ToArray()).ConfigureAwait(false);
        }

        public override async Task InvokeAsync(string targetName, params object[] arguments)
        {
            CancellationToken.ThrowIfCancellationRequested();

            try
            {
                await _serviceClient.InvokeAsync(
                    targetName, arguments.Concat(PinnedScope.SolutionChecksum.ToArray()).ToArray()).ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
                // object disposed exception can be thrown from StreamJsonRpc if JsonRpc is disposed in the middle of read/write.
                // the way we added cancellation support to the JsonRpc which doesn't support cancellation natively
                // can cause this exception to happen. newer version supports cancellation token natively, but
                // we can't use it now, so we will catch object disposed exception and check cancellation token
                CancellationToken.ThrowIfCancellationRequested();
                throw;
            }
        }

        public override async Task<T> InvokeAsync<T>(string targetName, params object[] arguments)
        {
            CancellationToken.ThrowIfCancellationRequested();

            try
            {
                return await _serviceClient.InvokeAsync<T>(
                    targetName, arguments.Concat(PinnedScope.SolutionChecksum.ToArray()).ToArray()).ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
                // object disposed exception can be thrown from StreamJsonRpc if JsonRpc is disposed in the middle of read/write.
                // the way we added cancellation support to the JsonRpc which doesn't support cancellation natively
                // can cause this exception to happen. newer version supports cancellation token natively, but
                // we can't use it now, so we will catch object disposed exception and check cancellation token
                CancellationToken.ThrowIfCancellationRequested();
                throw;
            }
        }

        public override async Task InvokeAsync(string targetName, IEnumerable<object> arguments, Func<Stream, CancellationToken, Task> funcWithDirectStreamAsync)
        {
            CancellationToken.ThrowIfCancellationRequested();

            try
            {
                await _serviceClient.InvokeAsync(
                    targetName, arguments.Concat(PinnedScope.SolutionChecksum.ToArray()).ToArray(),
                    funcWithDirectStreamAsync, CancellationToken).ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
                // object disposed exception can be thrown from StreamJsonRpc if JsonRpc is disposed in the middle of read/write.
                // the way we added cancellation support to the JsonRpc which doesn't support cancellation natively
                // can cause this exception to happen. newer version supports cancellation token natively, but
                // we can't use it now, so we will catch object disposed exception and check cancellation token
                CancellationToken.ThrowIfCancellationRequested();
                throw;
            }
        }

        public override async Task<T> InvokeAsync<T>(string targetName, IEnumerable<object> arguments, Func<Stream, CancellationToken, Task<T>> funcWithDirectStreamAsync)
        {
            CancellationToken.ThrowIfCancellationRequested();

            try
            {
                return await _serviceClient.InvokeAsync<T>(
                    targetName, arguments.Concat(PinnedScope.SolutionChecksum.ToArray()).ToArray(),
                    funcWithDirectStreamAsync, CancellationToken).ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
                // object disposed exception can be thrown from StreamJsonRpc if JsonRpc is disposed in the middle of read/write.
                // the way we added cancellation support to the JsonRpc which doesn't support cancellation natively
                // can cause this exception to happen. newer version supports cancellation token natively, but
                // we can't use it now, so we will catch object disposed exception and check cancellation token
                CancellationToken.ThrowIfCancellationRequested();
                throw;
            }
        }

        protected override void OnDisposed()
        {
            // dispose cancellation registration
            _cancellationRegistration.Dispose();

            // dispose service and snapshot channels
            _serviceClient.Dispose();
            _snapshotClient.Dispose();
        }

        /// <summary>
        /// Communication channel between VS feature and roslyn service in remote host.
        /// 
        /// this is the channel consumer of remote host client will playing with
        /// </summary>
        private class ServiceJsonRpcClient : JsonRpcClient
        {
            private readonly object _callbackTarget;

            public ServiceJsonRpcClient(Stream stream, object callbackTarget)
                : base(stream, callbackTarget, useThisAsCallback: false)
            {
                // this one doesn't need cancellation token since it has nothing to cancel
                _callbackTarget = callbackTarget;
            }
        }

        /// <summary>
        /// Communication channel between remote host client and remote host.
        /// 
        /// this is framework's back channel to talk to remote host
        /// 
        /// for example, this will be used to deliver missing assets in remote host.
        /// 
        /// each remote host client will have its own back channel so that it can work isolated
        /// with other clients.
        /// </summary>
        private class SnapshotJsonRpcClient : JsonRpcClient
        {
            private readonly JsonRpcSession _owner;
            private readonly CancellationTokenSource _source;

            public SnapshotJsonRpcClient(JsonRpcSession owner, Stream stream)
                : base(stream, callbackTarget: null, useThisAsCallback: true)
            {
                _owner = owner;
                _source = new CancellationTokenSource();
            }

            private PinnedRemotableDataScope PinnedScope => _owner.PinnedScope;

            /// <summary>
            /// this is callback from remote host side to get asset associated with checksum from VS.
            /// </summary>
            public async Task RequestAssetAsync(int sessionId, byte[][] checksums, string streamName)
            {
                try
                {
                    Contract.ThrowIfFalse(_owner._currentSessionId == sessionId);

                    using (Logger.LogBlock(FunctionId.JsonRpcSession_RequestAssetAsync, streamName, _source.Token))
                    using (var stream = await DirectStream.GetAsync(streamName, _source.Token).ConfigureAwait(false))
                    {
                        using (var writer = new ObjectWriter(stream))
                        {
                            writer.WriteInt32(sessionId);

                            await WriteAssetAsync(writer, checksums).ConfigureAwait(false);
                        }

                        await stream.FlushAsync(_source.Token).ConfigureAwait(false);
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

            private async Task WriteAssetAsync(ObjectWriter writer, byte[][] checksums)
            {
                // special case
                if (checksums.Length == 0)
                {
                    await WriteNoAssetAsync(writer).ConfigureAwait(false);
                    return;
                }

                if (checksums.Length == 1)
                {
                    await WriteOneAssetAsync(writer, checksums[0]).ConfigureAwait(false);
                    return;
                }

                await WriteMultipleAssetsAsync(writer, checksums).ConfigureAwait(false);
            }

            private Task WriteNoAssetAsync(ObjectWriter writer)
            {
                writer.WriteInt32(0);
                return SpecializedTasks.EmptyTask;
            }

            private async Task WriteOneAssetAsync(ObjectWriter writer, byte[] checksum)
            {
                var remotableData = PinnedScope.GetRemotableData(new Checksum(checksum), _source.Token) ?? RemotableData.Null;
                writer.WriteInt32(1);

                writer.WriteValue(checksum);
                writer.WriteString(remotableData.Kind);

                await remotableData.WriteObjectToAsync(writer, _source.Token).ConfigureAwait(false);
            }

            private async Task WriteMultipleAssetsAsync(ObjectWriter writer, byte[][] checksums)
            {
                var remotableDataMap = PinnedScope.GetRemotableData(checksums.Select(c => new Checksum(c)), _source.Token);
                writer.WriteInt32(remotableDataMap.Count);

                foreach (var kv in remotableDataMap)
                {
                    var checksum = kv.Key;
                    var remotableData = kv.Value;

                    writer.WriteValue(checksum.ToArray());
                    writer.WriteString(remotableData.Kind);

                    await remotableData.WriteObjectToAsync(writer, _source.Token).ConfigureAwait(false);
                }
            }

            protected override void OnDisconnected(object sender, JsonRpcDisconnectedEventArgs e)
            {
                _source.Cancel();
            }
        }
    }
}