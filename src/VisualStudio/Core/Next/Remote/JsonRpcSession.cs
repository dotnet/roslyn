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
        // communication channel related to service information
        private readonly ServiceJsonRpcClient _serviceClient;

        // communication channel related to snapshot information
        private readonly SnapshotJsonRpcClient _snapshotClient;

        // close connection when cancellation has raised
        private readonly CancellationTokenRegistration _cancellationRegistration;

        public JsonRpcSession(
            object callbackTarget,
            Stream serviceStream,
            Stream snapshotStream,
            CancellationToken cancellationToken) :
            base(cancellationToken)
        {
            _serviceClient = new ServiceJsonRpcClient(serviceStream, callbackTarget, cancellationToken);
            _snapshotClient = new SnapshotJsonRpcClient(this, snapshotStream, cancellationToken);

            // dispose session when cancellation has raised
            _cancellationRegistration = CancellationToken.Register(Dispose);
        }

        public override async Task RegisterPinnedRemotableDataScopeAsync(PinnedRemotableDataScope scope)
        {
            await base.RegisterPinnedRemotableDataScopeAsync(scope).ConfigureAwait(false);

            await _snapshotClient.InvokeAsync(WellKnownServiceHubServices.ServiceHubServiceBase_Initialize, scope.SolutionInfo).ConfigureAwait(false);
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
                _owner = owner;
                _source = new CancellationTokenSource();

                StartListening();
            }

            /// <summary>
            /// this is callback from remote host side to get asset associated with checksum from VS.
            /// </summary>
            public async Task RequestAssetAsync(int scopeId, Checksum[] checksums, string streamName)
            {
                try
                {
                    using (Logger.LogBlock(FunctionId.JsonRpcSession_RequestAssetAsync, streamName, _source.Token))
                    using (var stream = await DirectStream.GetAsync(streamName, _source.Token).ConfigureAwait(false))
                    {
                        var scope = _owner.PinnedRemotableDataScope;
                        using (var writer = new ObjectWriter(stream))
                        {
                            writer.WriteInt32(scopeId);

                            await WriteAssetAsync(writer, scope, checksums).ConfigureAwait(false);
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

            private async Task WriteAssetAsync(ObjectWriter writer, PinnedRemotableDataScope scope, Checksum[] checksums)
            {
                // special case
                if (checksums.Length == 0)
                {
                    await WriteNoAssetAsync(writer).ConfigureAwait(false);
                    return;
                }

                if (checksums.Length == 1)
                {
                    await WriteOneAssetAsync(writer, scope, checksums[0]).ConfigureAwait(false);
                    return;
                }

                await WriteMultipleAssetsAsync(writer, scope, checksums).ConfigureAwait(false);
            }

            private Task WriteNoAssetAsync(ObjectWriter writer)
            {
                writer.WriteInt32(0);
                return SpecializedTasks.EmptyTask;
            }

            private async Task WriteOneAssetAsync(ObjectWriter writer, PinnedRemotableDataScope scope, Checksum checksum)
            {
                var remotableData = scope.GetRemotableData(checksum, _source.Token) ?? RemotableData.Null;
                writer.WriteInt32(1);

                checksum.WriteTo(writer);
                writer.WriteInt32((int)remotableData.Kind);

                await remotableData.WriteObjectToAsync(writer, _source.Token).ConfigureAwait(false);
            }

            private async Task WriteMultipleAssetsAsync(ObjectWriter writer, PinnedRemotableDataScope scope, Checksum[] checksums)
            {
                var remotableDataMap = scope.GetRemotableData(checksums, _source.Token);
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