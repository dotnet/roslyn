// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// Provides solution assets present locally (in the current process) to a remote process where the solution is being replicated to.
    /// </summary>
    internal sealed class SolutionAssetProvider : ISolutionAssetProvider
    {
        public const string ServiceName = "SolutionAssetProvider";

        internal static ServiceDescriptor ServiceDescriptor { get; } = ServiceDescriptor.CreateInProcServiceDescriptor(ServiceDescriptors.ComponentName, ServiceName, suffix: "", ServiceDescriptors.GetFeatureDisplayName);

        private readonly HostWorkspaceServices _services;

        public SolutionAssetProvider(HostWorkspaceServices services)
        {
            _services = services;
        }

        public async ValueTask GetAssetsAsync(PipeWriter pipeWriter, Checksum solutionChecksum, Checksum[] checksums, CancellationToken cancellationToken)
        {
            var assetStorage = _services.GetRequiredService<ISolutionAssetStorageProvider>().AssetStorage;
            var serializer = _services.GetRequiredService<ISerializerService>();
            var scope = assetStorage.GetScope(solutionChecksum);

            SolutionAsset? singleAsset = null;
            IReadOnlyDictionary<Checksum, SolutionAsset>? assetMap = null;

            if (checksums.Length == 1)
            {
                singleAsset = await scope.GetAssetAsync(checksums[0], cancellationToken).ConfigureAwait(false);
                singleAsset ??= SolutionAsset.Null;
            }
            else
            {
                assetMap = await scope.GetAssetsAsync(checksums, cancellationToken).ConfigureAwait(false);
            }

            // We can cancel early, but once the pipe operations are scheduled we rely on both operations running to
            // avoid deadlocks (the exception handler in 'task1' ensures progress is made in 'task2').
            cancellationToken.ThrowIfCancellationRequested();
            var mustNotCancelToken = CancellationToken.None;

            // Work around the lack of async stream writing in ObjectWriter, which is required when writing to the RPC
            // pipe. Run two tasks - the first synchronously writes to a local pipe and the second asynchronously
            // transfers the data to the RPC pipe.
            //
            // Configure the pipe to never block on write (waiting for the reader to read). This prevents deadlocks but
            // might result in more (non-contiguous) memory allocated for the underlying buffers. The amount of memory
            // is bounded by the total size of the serialized assets.
            var localPipe = new Pipe(RemoteHostAssetSerialization.PipeOptionsWithUnlimitedWriterBuffer);

            var task1 = Task.Run(() =>
            {
                try
                {
                    var stream = localPipe.Writer.AsStream(leaveOpen: false);
                    using var writer = new ObjectWriter(stream, leaveOpen: false, cancellationToken);
                    RemoteHostAssetSerialization.WriteData(writer, singleAsset, assetMap, serializer, scope.ReplicationContext, solutionChecksum, checksums, cancellationToken);
                }
                catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
                {
                    // no-op
                }
            }, mustNotCancelToken);

            // Complete RPC once we send the initial piece of data and start waiting for the writer to send more,
            // so the client can start reading from the stream. Once CopyPipeDataAsync completes the pipeWriter
            // the corresponding client-side pipeReader will complete and the data transfer will be finished.
            var task2 = CopyPipeDataAsync();

            await Task.WhenAll(task1, task2).ConfigureAwait(false);

            async Task CopyPipeDataAsync()
            {
                Exception? exception = null;
                try
                {
                    await localPipe.Reader.CopyToAsync(pipeWriter, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken);
                    exception = e;
                }
                finally
                {
                    await localPipe.Reader.CompleteAsync(exception).ConfigureAwait(false);
                    await pipeWriter.CompleteAsync(exception).ConfigureAwait(false);
                }
            }
        }
    }
}
