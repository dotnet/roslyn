// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Serialization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    internal static class RemoteHostAssetSerialization
    {
        public static async Task WriteDataAsync(ObjectWriter writer, SolutionAssetStorage assetStorage, ISerializerService serializer, int scopeId, Checksum[] checksums, CancellationToken cancellationToken)
        {
            writer.WriteInt32(scopeId);

            // special case
            if (checksums.Length == 0)
            {
                writer.WriteInt32(0);
                return;
            }

            if (checksums.Length == 1)
            {
                var checksum = checksums[0];

                var asset = (await assetStorage.GetAssetAsync(scopeId, checksum, cancellationToken).ConfigureAwait(false)) ?? SolutionAsset.Null;
                writer.WriteInt32(1);

                WriteAsset(writer, serializer, checksum, asset, cancellationToken);
                return;
            }

            var assets = await assetStorage.GetAssetsAsync(scopeId, checksums, cancellationToken).ConfigureAwait(false);
            writer.WriteInt32(assets.Count);

            foreach (var (checksum, asset) in assets)
            {
                WriteAsset(writer, serializer, checksum, asset, cancellationToken);
            }

            static void WriteAsset(ObjectWriter writer, ISerializerService serializer, Checksum checksum, SolutionAsset asset, CancellationToken cancellationToken)
            {
                checksum.WriteTo(writer);
                writer.WriteInt32((int)asset.Kind);

                // null is already indicated by checksum and kind above:
                if (asset.Value is not null)
                {
                    serializer.Serialize(asset.Value, writer, cancellationToken);
                }
            }
        }

        public static ImmutableArray<(Checksum, object)> ReadData(Stream stream, int scopeId, ISet<Checksum> checksums, ISerializerService serializerService, CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<(Checksum, object)>.GetInstance(out var results);

            using var reader = ObjectReader.GetReader(stream, leaveOpen: true, cancellationToken);

            var responseScopeId = reader.ReadInt32();
            Contract.ThrowIfFalse(scopeId == responseScopeId);

            var count = reader.ReadInt32();
            Contract.ThrowIfFalse(count == checksums.Count);

            for (var i = 0; i < count; i++)
            {
                var responseChecksum = Checksum.ReadFrom(reader);
                Contract.ThrowIfFalse(checksums.Contains(responseChecksum));

                var kind = (WellKnownSynchronizationKind)reader.ReadInt32();

                // in service hub, cancellation means simply closed stream
                var result = serializerService.Deserialize<object>(kind, reader, cancellationToken);

                results.Add((responseChecksum, result));
            }

            return results.ToImmutable();
        }
    }
}
