// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;
using StreamJsonRpc;

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
        private readonly CancellationTokenSource _shutdownCancellationSource;

        public RemotableDataJsonRpc(Workspace workspace, TraceSource logger, Stream stream)
            : base(workspace, logger, stream, callbackTarget: null, useThisAsCallback: true)
        {
            _remotableDataService = workspace.Services.GetService<IRemotableDataService>();

            _shutdownCancellationSource = new CancellationTokenSource();

            StartListening();
        }

        /// <summary>
        /// this is callback from remote host side to get asset associated with checksum from VS.
        /// </summary>
        public async Task RequestAssetAsync(int scopeId, Checksum[] checksums, string streamName, CancellationToken cancellationToken)
        {
            try
            {
                using (var combinedCancellationToken = _shutdownCancellationSource.Token.CombineWith(cancellationToken))
                using (Logger.LogBlock(FunctionId.JsonRpcSession_RequestAssetAsync, streamName, combinedCancellationToken.Token))
                using (var stream = await DirectStream.GetAsync(streamName, combinedCancellationToken.Token).ConfigureAwait(false))
                {
                    using (var writer = new ObjectWriter(stream, combinedCancellationToken.Token))
                    {
                        writer.WriteInt32(scopeId);

                        await WriteAssetAsync(writer, scopeId, checksums, combinedCancellationToken.Token).ConfigureAwait(false);
                    }

                    await stream.FlushAsync(combinedCancellationToken.Token).ConfigureAwait(false);
                }
            }
            catch (Exception ex) when (ReportUnlessCanceled(ex, cancellationToken))
            {
                // only expected exception will be catched. otherwise, NFW and let it propagate
                Debug.Assert(cancellationToken.IsCancellationRequested || ex is IOException);
            }
        }

        public Task<bool> IsExperimentEnabledAsync(string experimentName, CancellationToken _)
        {
            return Task.FromResult(Workspace.Services.GetRequiredService<IExperimentationService>().IsExperimentEnabled(experimentName));
        }

        private bool ReportUnlessCanceled(Exception ex, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                // any exception can happen if things are cancelled.
                return true;
            }

            if (ex is IOException)
            {
                // direct connection can be disconnected before cancellation token from remote host have
                // passed to us
                return true;
            }

            // log the exception
            LogError("unexpected exception from RequestAsset: " + ex.ToString());

            // report NFW
            ex.ReportServiceHubNFW("RequestAssetFailed");
            return false;
        }

        private async Task WriteAssetAsync(ObjectWriter writer, int scopeId, Checksum[] checksums, CancellationToken cancellationToken)
        {
            // special case
            if (checksums.Length == 0)
            {
                await WriteNoAssetAsync(writer).ConfigureAwait(false);
                return;
            }

            if (checksums.Length == 1)
            {
                await WriteOneAssetAsync(writer, scopeId, checksums[0], cancellationToken).ConfigureAwait(false);
                return;
            }

            await WriteMultipleAssetsAsync(writer, scopeId, checksums, cancellationToken).ConfigureAwait(false);
        }

        private Task WriteNoAssetAsync(ObjectWriter writer)
        {
            writer.WriteInt32(0);
            return Task.CompletedTask;
        }

        private async Task WriteOneAssetAsync(ObjectWriter writer, int scopeId, Checksum checksum, CancellationToken cancellationToken)
        {
            var remotableData = _remotableDataService.GetRemotableData(scopeId, checksum, cancellationToken) ?? RemotableData.Null;
            writer.WriteInt32(1);

            checksum.WriteTo(writer);
            writer.WriteInt32((int)remotableData.Kind);

            await remotableData.WriteObjectToAsync(writer, cancellationToken).ConfigureAwait(false);
        }

        private async Task WriteMultipleAssetsAsync(ObjectWriter writer, int scopeId, Checksum[] checksums, CancellationToken cancellationToken)
        {
            var remotableDataMap = _remotableDataService.GetRemotableData(scopeId, checksums, cancellationToken);
            writer.WriteInt32(remotableDataMap.Count);

            foreach (var (checksum, remotableData) in remotableDataMap)
            {
                checksum.WriteTo(writer);
                writer.WriteInt32((int)remotableData.Kind);

                await remotableData.WriteObjectToAsync(writer, cancellationToken).ConfigureAwait(false);
            }
        }

        protected override void Dispose(bool disposing)
        {
            Contract.ThrowIfFalse(disposing);
            Disconnect();
        }

        protected override void Disconnected(JsonRpcDisconnectedEventArgs e)
        {
            // we don't expect OOP side to disconnect the connection. 
            // Host (VS) always initiate or disconnect the connection.
            if (e.Reason != DisconnectedReason.LocallyDisposed)
            {
                // log when this happens
                LogDisconnectInfo(e, new StackTrace().ToString());
            }

            _shutdownCancellationSource.Cancel();
        }
    }
}
