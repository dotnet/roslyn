// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ErrorReporting;
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
    /// Called by SnapshotService from the ServiceHub process to morror remotable data (e.g. solution snapshots)
    /// from Visual Studio process to the ServiceHub process.
    /// </summary>
    internal sealed class RemotableDataProvider : IDisposable
    {
        private readonly Workspace _workspace;
        private readonly IRemotableDataService _remotableDataService;
        private readonly CancellationTokenSource _shutdownCancellationSource;
        private readonly RemoteEndPoint _endPoint;

        public RemotableDataProvider(Workspace workspace, TraceSource logger, Stream snapshotServiceStream)
        {
            _workspace = workspace;
            _remotableDataService = workspace.Services.GetRequiredService<IRemotableDataService>();

            _shutdownCancellationSource = new CancellationTokenSource();

            _endPoint = new RemoteEndPoint(snapshotServiceStream, logger, incomingCallTarget: this);
            _endPoint.UnexpectedExceptionThrown += UnexpectedExceptionThrown;
            _endPoint.Disconnected += OnDisconnected;
            _endPoint.StartListening();
        }

        private void UnexpectedExceptionThrown(Exception exception)
            => RemoteHostCrashInfoBar.ShowInfoBar(_workspace, exception);

        private void OnDisconnected(JsonRpcDisconnectedEventArgs e)
            => _shutdownCancellationSource.Cancel();

        public void Dispose()
        {
            _endPoint.Disconnected -= OnDisconnected;
            _endPoint.UnexpectedExceptionThrown -= UnexpectedExceptionThrown;
            _endPoint.Dispose();
        }

        /// <summary>
        /// Called remotely: <see cref="WellKnownServiceHubServices.AssetService_RequestAssetAsync"/>.
        /// </summary>
        public async Task RequestAssetAsync(int scopeId, Checksum[] checksums, string pipeName, CancellationToken cancellationToken)
        {
            try
            {
                using var combinedCancellationToken = _shutdownCancellationSource.Token.CombineWith(cancellationToken);

                using (Logger.LogBlock(FunctionId.JsonRpcSession_RequestAssetAsync, pipeName, combinedCancellationToken.Token))
                {
                    await RemoteEndPoint.WriteDataToNamedPipeAsync(
                        pipeName,
                        (scopeId, checksums),
                        (writer, data, ct) => WriteAssetAsync(writer, data.scopeId, data.checksums, ct),
                        combinedCancellationToken.Token).ConfigureAwait(false);
                }
            }
            catch (Exception ex) when (FatalError.ReportWithoutCrashUnlessCanceledAndPropagate(ex, cancellationToken))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        /// <summary>
        /// Called remotely: <see cref="WellKnownServiceHubServices.AssetService_IsExperimentEnabledAsync"/>.
        /// </summary>
        public Task<bool> IsExperimentEnabledAsync(string experimentName, CancellationToken cancellationToken)
        {
            try
            {
                return Task.FromResult(_workspace.Services.GetRequiredService<IExperimentationService>().IsExperimentEnabled(experimentName));
            }
            catch (Exception ex) when (FatalError.ReportWithoutCrashUnlessCanceledAndPropagate(ex, cancellationToken))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private async Task WriteAssetAsync(ObjectWriter writer, int scopeId, Checksum[] checksums, CancellationToken cancellationToken)
        {
            writer.WriteInt32(scopeId);

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
            var remotableData = (await _remotableDataService.GetRemotableDataAsync(scopeId, checksum, cancellationToken).ConfigureAwait(false)) ?? RemotableData.Null;
            writer.WriteInt32(1);

            checksum.WriteTo(writer);
            writer.WriteInt32((int)remotableData.Kind);

            await remotableData.WriteObjectToAsync(writer, cancellationToken).ConfigureAwait(false);
        }

        private async Task WriteMultipleAssetsAsync(ObjectWriter writer, int scopeId, Checksum[] checksums, CancellationToken cancellationToken)
        {
            var remotableDataMap = await _remotableDataService.GetRemotableDataAsync(scopeId, checksums, cancellationToken).ConfigureAwait(false);
            writer.WriteInt32(remotableDataMap.Count);

            foreach (var (checksum, remotableData) in remotableDataMap)
            {
                checksum.WriteTo(writer);
                writer.WriteInt32((int)remotableData.Kind);

                await remotableData.WriteObjectToAsync(writer, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
