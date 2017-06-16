// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
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
        private static int s_sessionId = 1;

        // current session id
        private readonly int _currentSessionId;

        // communication channel related to service information
        private readonly ServiceJsonRpcClient _serviceClient;

        // communication channel related to snapshot information
        private readonly SnapshotJsonRpcClient _snapshotClientOpt;

        // close connection when cancellation has raised
        private readonly CancellationTokenRegistration _cancellationRegistration;

        public static async Task<JsonRpcSession> CreateAsync(
            Optional<Func<CancellationToken, Task<PinnedRemotableDataScope>>> getSnapshotAsync,
            object callbackTarget,
            Stream serviceStream,
            Stream snapshotStreamOpt,
            CancellationToken cancellationToken)
        {
            var snapshot = getSnapshotAsync.Value == null ? null : await getSnapshotAsync.Value(cancellationToken).ConfigureAwait(false);

            JsonRpcSession session;
            try
            {
                session = new JsonRpcSession(snapshot, callbackTarget, serviceStream, snapshotStreamOpt, cancellationToken);
            }
            catch
            {
                snapshot?.Dispose();
                throw;
            }

            try
            {
                await session.InitializeAsync().ConfigureAwait(false);
            }
            catch when (!cancellationToken.IsCancellationRequested)
            {
                // The session disposes of itself when cancellation is requested.
                session.Dispose();
                throw;
            }

            return session;
        }

        private JsonRpcSession(
            PinnedRemotableDataScope snapshot,
            object callbackTarget,
            Stream serviceStream,
            Stream snapshotStreamOpt,
            CancellationToken cancellationToken) :
            base(snapshot, cancellationToken)
        {
            Contract.Requires((snapshot == null) == (snapshotStreamOpt == null));

            // get session id
            _currentSessionId = Interlocked.Increment(ref s_sessionId);

            _serviceClient = new ServiceJsonRpcClient(serviceStream, callbackTarget, cancellationToken);
            _snapshotClientOpt = snapshot == null ? null : new SnapshotJsonRpcClient(this, snapshotStreamOpt, cancellationToken);

            // dispose session when cancellation has raised
            _cancellationRegistration = CancellationToken.Register(Dispose);
        }

        private async Task InitializeAsync()
        {
            // All roslyn remote service must based on ServiceHubServiceBase which implements Initialize method
            // This will set this session's solution and whether that solution is for primary branch or not
            var primaryBranch = PinnedScopeOpt?.ForPrimaryBranch ?? false;
            var solutionChecksum = PinnedScopeOpt?.SolutionChecksum;

            if (_snapshotClientOpt != null)
            {
                await _snapshotClientOpt.InvokeAsync(WellKnownServiceHubServices.ServiceHubServiceBase_Initialize, _currentSessionId, primaryBranch, solutionChecksum).ConfigureAwait(false);
            }

            await _serviceClient.InvokeAsync(WellKnownServiceHubServices.ServiceHubServiceBase_Initialize, _currentSessionId, primaryBranch, solutionChecksum).ConfigureAwait(false);
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
            // dispose cancellation registration
            _cancellationRegistration.Dispose();

            // dispose service and snapshot channels
            _serviceClient.Dispose();
            _snapshotClientOpt?.Dispose();
        }

        /// <summary>
        /// Communication channel between VS feature and roslyn service in remote host.
        /// 
        /// this is the channel consumer of remote host client will playing with
        /// </summary>
        private class ServiceJsonRpcClient : JsonRpcClient
        {
            private readonly object _callbackTarget;

            public ServiceJsonRpcClient(Stream stream, object callbackTarget, CancellationToken cancellationToken)
                : base(stream, callbackTarget, useThisAsCallback: false, cancellationToken: cancellationToken)
            {
                // this one doesn't need cancellation token since it has nothing to cancel
                _callbackTarget = callbackTarget;

                StartListening();
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

            public SnapshotJsonRpcClient(JsonRpcSession owner, Stream stream, CancellationToken cancellationToken)
                : base(stream, callbackTarget: null, useThisAsCallback: true, cancellationToken: cancellationToken)
            {
                Contract.ThrowIfNull(owner.PinnedScopeOpt);

                _owner = owner;
                _source = new CancellationTokenSource();

                StartListening();
            }

            private PinnedRemotableDataScope PinnedScope => _owner.PinnedScopeOpt;

            /// <summary>
            /// this is callback from remote host side to get asset associated with checksum from VS.
            /// </summary>
            public async Task RequestAssetAsync(int sessionId, Checksum[] checksums, string streamName)
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

            private async Task WriteAssetAsync(ObjectWriter writer, Checksum[] checksums)
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

            private async Task WriteOneAssetAsync(ObjectWriter writer, Checksum checksum)
            {
                var remotableData = PinnedScope.GetRemotableData(checksum, _source.Token) ?? RemotableData.Null;
                writer.WriteInt32(1);

                checksum.WriteTo(writer);
                writer.WriteInt32((int)remotableData.Kind);

                await remotableData.WriteObjectToAsync(writer, _source.Token).ConfigureAwait(false);
            }

            private async Task WriteMultipleAssetsAsync(ObjectWriter writer, Checksum[] checksums)
            {
                var remotableDataMap = PinnedScope.GetRemotableData(checksums, _source.Token);
                writer.WriteInt32(remotableDataMap.Count);

                foreach (var kv in remotableDataMap)
                {
                    var checksum = kv.Key;
                    var remotableData = kv.Value;

                    checksum.WriteTo(writer);
                    writer.WriteInt32((int)remotableData.Kind);

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