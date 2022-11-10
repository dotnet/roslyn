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
            SolutionAsset? singleAsset,
            IReadOnlyDictionary<Checksum, SolutionAsset>? assetMap,
            ISerializerService serializer,
            SolutionReplicationContext context,
            Checksum solutionChecksum,
            Checksum[] checksums,
            CancellationToken cancellationToken)
        {
            using var writer = new ObjectWriter(stream, leaveOpen: true, cancellationToken);

            // This information is not actually needed on the receiving end.  However, we still send it so that the
            // receiver can assert that both sides are talking about the same solution snapshot and no weird invariant
            // breaks have occurred.
            solutionChecksum.WriteTo(writer);

            // special case
            if (checksums.Length == 0)
            {
                writer.WriteInt32(0);
                return;
            }

            if (singleAsset != null)
            {
                writer.WriteInt32(1);
                WriteAsset(writer, serializer, context, checksums[0], singleAsset, cancellationToken);
                return;
            }

            Debug.Assert(assetMap != null);
            writer.WriteInt32(assetMap.Count);

            foreach (var (checksum, asset) in assetMap)
            {
                WriteAsset(writer, serializer, context, checksum, asset, cancellationToken);

                // We flush after each item as that forms a reasonably sized chunk of data to want to then send over the
                // pipe for the reader on the other side to read.  This allows the item-writing to remain entirely
                // synchronous without any blocking on async flushing, while also ensuring that we're not buffering the
                // entire stream of data into the pipe before it gets sent to the other side.
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            return;

            static void WriteAsset(ObjectWriter writer, ISerializerService serializer, SolutionReplicationContext context, Checksum checksum, SolutionAsset asset, CancellationToken cancellationToken)
            {
                Debug.Assert(asset.Kind != WellKnownSynchronizationKind.Null, "We should not be sending null assets");
                checksum.WriteTo(writer);
                writer.WriteInt32((int)asset.Kind);

                // null is already indicated by checksum and kind above:
                if (asset.Value is not null)
                {
                    serializer.Serialize(asset.Value, writer, context, cancellationToken);
                }
            }
        }

        public static async ValueTask<ImmutableArray<(Checksum, object)>> ReadDataAsync(
            PipeReader pipeReader, Checksum solutionChecksum, ISet<Checksum> checksums, ISerializerService serializerService, CancellationToken cancellationToken)
        {
            using var stream = await pipeReader.AsPrebufferedStreamAsync(cancellationToken).ConfigureAwait(false);
            return ReadData(stream, solutionChecksum, checksums, serializerService, cancellationToken);
        }

        public static ImmutableArray<(Checksum, object)> ReadData(Stream stream, Checksum solutionChecksum, ISet<Checksum> checksums, ISerializerService serializerService, CancellationToken cancellationToken)
        {
            Debug.Assert(!checksums.Contains(Checksum.Null));

            using var _ = ArrayBuilder<(Checksum, object)>.GetInstance(out var results);

            using var reader = ObjectReader.GetReader(stream, leaveOpen: true, cancellationToken);

            // Ensure that no invariants were broken and that both sides of the communication channel are talking about
            // the same pinned solution.
            var responseSolutionChecksum = Checksum.ReadFrom(reader);
            Contract.ThrowIfFalse(solutionChecksum == responseSolutionChecksum);

            var count = reader.ReadInt32();
            Contract.ThrowIfFalse(count == checksums.Count);

            for (var i = 0; i < count; i++)
            {
                var responseChecksum = Checksum.ReadFrom(reader);
                Contract.ThrowIfFalse(checksums.Contains(responseChecksum));

                var kind = (WellKnownSynchronizationKind)reader.ReadInt32();

                // in service hub, cancellation means simply closed stream
                var result = serializerService.Deserialize<object>(kind, reader, cancellationToken);

                Debug.Assert(result != null, "We should not be requesting null assets");

                results.Add((responseChecksum, result));
            }

            return results.ToImmutable();
        }
    }
}
