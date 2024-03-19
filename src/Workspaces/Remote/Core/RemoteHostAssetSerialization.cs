// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Serialization;
using Nerdbank.Streams;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    internal static class RemoteHostAssetSerialization
    {
        public static async ValueTask WriteDataAsync(
            Stream stream,
            Dictionary<Checksum, object> assetMap,
            ISerializerService serializer,
            SolutionReplicationContext context,
            Checksum solutionChecksum,
            ReadOnlyMemory<Checksum> checksums,
            CancellationToken cancellationToken)
        {
            using var writer = new ObjectWriter(stream, leaveOpen: true, cancellationToken);

            // This information is not actually needed on the receiving end.  However, we still send it so that the
            // receiver can assert that both sides are talking about the same solution snapshot and no weird invariant
            // breaks have occurred.
            solutionChecksum.WriteTo(writer);

            // special case
            if (checksums.Length == 0)
                return;

            Debug.Assert(assetMap != null);

            for (var i = 0; i < checksums.Span.Length; i++)
            {
                var checksum = checksums.Span[i];
                var asset = assetMap[checksum];

                // We flush after each item as that forms a reasonably sized chunk of data to want to then send over the
                // pipe for the reader on the other side to read.  This allows the item-writing to remain entirely
                // synchronous without any blocking on async flushing, while also ensuring that we're not buffering the
                // entire stream of data into the pipe before it gets sent to the other side.
                WriteAsset(writer, serializer, context, asset, cancellationToken);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            return;

            static void WriteAsset(ObjectWriter writer, ISerializerService serializer, SolutionReplicationContext context, object asset, CancellationToken cancellationToken)
            {
                Contract.ThrowIfNull(asset);
                var kind = asset.GetWellKnownSynchronizationKind();
                writer.WriteInt32((int)kind);

                serializer.Serialize(asset, writer, context, cancellationToken);
            }
        }

        public static ValueTask<ImmutableArray<object>> ReadDataAsync(
            PipeReader pipeReader, Checksum solutionChecksum, int objectCount, ISerializerService serializerService, CancellationToken cancellationToken)
        {
            // Suppress ExecutionContext flow for asynchronous operations operate on the pipe. In addition to avoiding
            // ExecutionContext allocations, this clears the LogicalCallContext and avoids the need to clone data set by
            // CallContext.LogicalSetData at each yielding await in the task tree.
            //
            // ⚠ DO NOT AWAIT INSIDE THE USING. The Dispose method that restores ExecutionContext flow must run on the
            // same thread where SuppressFlow was originally run.
            using var _ = FlowControlHelper.TrySuppressFlow();
            return ReadDataSuppressedFlowAsync(pipeReader, solutionChecksum, objectCount, serializerService, cancellationToken);

            static async ValueTask<ImmutableArray<object>> ReadDataSuppressedFlowAsync(
                PipeReader pipeReader, Checksum solutionChecksum, int objectCount, ISerializerService serializerService, CancellationToken cancellationToken)
            {
                using var stream = await pipeReader.AsPrebufferedStreamAsync(cancellationToken).ConfigureAwait(false);
                return ReadData(stream, solutionChecksum, objectCount, serializerService, cancellationToken);
            }
        }

        public static ImmutableArray<object> ReadData(Stream stream, Checksum solutionChecksum, int objectCount, ISerializerService serializerService, CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<object>.GetInstance(objectCount, out var results);

            using var reader = ObjectReader.GetReader(stream, leaveOpen: true, cancellationToken);

            // Ensure that no invariants were broken and that both sides of the communication channel are talking about
            // the same pinned solution.
            var responseSolutionChecksum = Checksum.ReadFrom(reader);
            Contract.ThrowIfFalse(solutionChecksum == responseSolutionChecksum);

            for (int i = 0, n = objectCount; i < n; i++)
            {
                var kind = (WellKnownSynchronizationKind)reader.ReadInt32();

                // in service hub, cancellation means simply closed stream
                var result = serializerService.Deserialize(kind, reader, cancellationToken);
                Contract.ThrowIfNull(result);
                results.Add(result);
            }

            return results.ToImmutableAndClear();
        }
    }
}
