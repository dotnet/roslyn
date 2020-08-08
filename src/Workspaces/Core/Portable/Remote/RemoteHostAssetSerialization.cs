// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Serialization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    internal static class RemoteHostAssetSerialization
    {
        public static async Task WriteDataAsync(ObjectWriter writer, IRemotableDataService remotableDataService, int scopeId, Checksum[] checksums, CancellationToken cancellationToken)
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
                var remotableData = (await remotableDataService.GetRemotableDataAsync(scopeId, checksum, cancellationToken).ConfigureAwait(false)) ?? RemotableData.Null;
                writer.WriteInt32(1);

                await WriteRemotableData(writer, checksum, remotableData, cancellationToken).ConfigureAwait(false);
                return;
            }

            var remotableDataMap = await remotableDataService.GetRemotableDataAsync(scopeId, checksums, cancellationToken).ConfigureAwait(false);
            writer.WriteInt32(remotableDataMap.Count);

            foreach (var (checksum, remotableData) in remotableDataMap)
            {
                await WriteRemotableData(writer, checksum, remotableData, cancellationToken).ConfigureAwait(false);
            }

            static async Task WriteRemotableData(ObjectWriter writer, Checksum checksum, RemotableData remotableData, CancellationToken cancellationToken)
            {
                checksum.WriteTo(writer);
                writer.WriteInt32((int)remotableData.Kind);

                await remotableData.WriteObjectToAsync(writer, cancellationToken).ConfigureAwait(false);
            }
        }

        public static ImmutableArray<(Checksum, object)> ReadData(Stream stream, int scopeId, ISet<Checksum> checksums, ISerializerService serializerService, CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<(Checksum, object)>.GetInstance(out var results);

            using var reader = ObjectReader.TryGetReader(stream, leaveOpen: true, cancellationToken);

            // We only get a reader for data transmitted between live processes.
            // This data should always be correct as we're never persisting the data between sessions.
            Contract.ThrowIfNull(reader);

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
