// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
    /// <summary>
    /// Communication channel between remote host client and remote host.
    /// 
    /// this is framework's back channel to talk to remote host
    /// 
    /// for example, this will be used to deliver missing remotable data to remote host.
    /// 
    /// all connection will share one remotable data channel
    /// </summary>
    internal sealed class RemotableDataJsonRpc : JsonRpcEx
    {
        private readonly IRemotableDataService _remotableDataService;
        private readonly CancellationTokenSource _source;

        public RemotableDataJsonRpc(Microsoft.CodeAnalysis.Workspace workspace, Stream stream)
            : base(stream, callbackTarget: null, useThisAsCallback: true)
        {
            _remotableDataService = workspace.Services.GetService<IRemotableDataService>();

            // cancellation will be removed once cancellation refactoring is done
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
                    using (var writer = new ObjectWriter(stream))
                    {
                        writer.WriteInt32(scopeId);

                        await WriteAssetAsync(writer, scopeId, checksums).ConfigureAwait(false);
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

        private async Task WriteAssetAsync(ObjectWriter writer, int scopeId, Checksum[] checksums)
        {
            // special case
            if (checksums.Length == 0)
            {
                await WriteNoAssetAsync(writer).ConfigureAwait(false);
                return;
            }

            if (checksums.Length == 1)
            {
                await WriteOneAssetAsync(writer, scopeId, checksums[0]).ConfigureAwait(false);
                return;
            }

            await WriteMultipleAssetsAsync(writer, scopeId, checksums).ConfigureAwait(false);
        }

        private Task WriteNoAssetAsync(ObjectWriter writer)
        {
            writer.WriteInt32(0);
            return SpecializedTasks.EmptyTask;
        }

        private async Task WriteOneAssetAsync(ObjectWriter writer, int scopeId, Checksum checksum)
        {
            var remotableData = _remotableDataService.GetRemotableData(scopeId, checksum, _source.Token) ?? RemotableData.Null;
            writer.WriteInt32(1);

            checksum.WriteTo(writer);
            writer.WriteInt32((int)remotableData.Kind);

            await remotableData.WriteObjectToAsync(writer, _source.Token).ConfigureAwait(false);
        }

        private async Task WriteMultipleAssetsAsync(ObjectWriter writer, int scopeId, Checksum[] checksums)
        {
            var remotableDataMap = _remotableDataService.GetRemotableData(scopeId, checksums, _source.Token);
            writer.WriteInt32(remotableDataMap.Count);

            foreach (var (checksum, remotableData) in remotableDataMap)
            {
                checksum.WriteTo(writer);
                writer.WriteInt32((int)remotableData.Kind);

                await remotableData.WriteObjectToAsync(writer, _source.Token).ConfigureAwait(false);
            }
        }

        protected override void Dispose(bool disposing)
        {
            Contract.ThrowIfFalse(disposing);
            Disconnect();
        }

        protected override void OnDisconnected(object sender, JsonRpcDisconnectedEventArgs e)
        {
            _source.Cancel();
        }
    }
}