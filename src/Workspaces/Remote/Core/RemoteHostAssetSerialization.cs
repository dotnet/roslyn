﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Serialization;
using Roslyn.Utilities;
using System;
using System.IO.Pipelines;
using Microsoft.VisualStudio.Threading;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Remote
{
    internal static class RemoteHostAssetSerialization
    {
        internal static readonly PipeOptions PipeOptionsWithUnlimitedWriterBuffer = new(pauseWriterThreshold: long.MaxValue);

        public static async Task WriteDataAsync(ObjectWriter writer, SolutionAssetStorage assetStorage, ISerializerService serializer, int scopeId, Checksum[] checksums, CancellationToken cancellationToken)
        {
            SolutionAsset? singleAsset = null;
            IReadOnlyDictionary<Checksum, SolutionAsset>? assetMap = null;

            if (checksums.Length == 1)
            {
                singleAsset = (await assetStorage.GetAssetAsync(scopeId, checksums[0], cancellationToken).ConfigureAwait(false)) ?? SolutionAsset.Null;
            }
            else
            {
                assetMap = await assetStorage.GetAssetsAsync(scopeId, checksums, cancellationToken).ConfigureAwait(false);
            }

            WriteData(writer, singleAsset, assetMap, serializer, scopeId, checksums, cancellationToken);
        }

        public static void WriteData(
            ObjectWriter writer,
            SolutionAsset? singleAsset,
            IReadOnlyDictionary<Checksum, SolutionAsset>? assetMap,
            ISerializerService serializer,
            int scopeId,
            Checksum[] checksums,
            CancellationToken cancellationToken)
        {
            writer.WriteInt32(scopeId);

            // special case
            if (checksums.Length == 0)
            {
                writer.WriteInt32(0);
                return;
            }

            if (singleAsset != null)
            {
                writer.WriteInt32(1);
                WriteAsset(writer, serializer, checksums[0], singleAsset, cancellationToken);
                return;
            }

            Debug.Assert(assetMap != null);
            writer.WriteInt32(assetMap.Count);

            foreach (var (checksum, asset) in assetMap)
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

        public static async ValueTask<ImmutableArray<(Checksum, object)>> ReadDataAsync(PipeReader pipeReader, int scopeId, ISet<Checksum> checksums, ISerializerService serializerService, CancellationToken cancellationToken)
        {
            // We can cancel at entry, but once the pipe operations are scheduled we rely on both operations running to
            // avoid deadlocks (the exception handler in 'copyTask' ensures progress is made in the blocking read).
            cancellationToken.ThrowIfCancellationRequested();
            var mustNotCancelToken = CancellationToken.None;

            // Workaround for ObjectReader not supporting async reading.
            // Unless we read from the RPC stream asynchronously and with cancallation support we might hang when the server cancels.
            // https://github.com/dotnet/roslyn/issues/47861

            // Use local pipe to avoid blocking the current thread on networking IO.
            var localPipe = new Pipe(PipeOptionsWithUnlimitedWriterBuffer);

            Exception? exception = null;

            // start a task on a thread pool thread copying from the RPC pipe to a local pipe:
            var copyTask = Task.Run(async () =>
            {
                try
                {
                    await pipeReader.CopyToAsync(localPipe.Writer, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    exception = e;
                }
                finally
                {
                    await localPipe.Writer.CompleteAsync(exception).ConfigureAwait(false);
                    await pipeReader.CompleteAsync(exception).ConfigureAwait(false);
                }
            }, mustNotCancelToken);

            // blocking read from the local pipe on the current thread:
            try
            {
                using var stream = localPipe.Reader.AsStream(leaveOpen: false);
                return ReadData(stream, scopeId, checksums, serializerService, cancellationToken);
            }
            catch (EndOfStreamException)
            {
                cancellationToken.ThrowIfCancellationRequested();

                throw exception ?? ExceptionUtilities.Unreachable;
            }
            finally
            {
                // Make sure to complete the copy and pipes before returning, otherwise the caller could complete the
                // reader and/or writer while they are still in use.
                await copyTask.ConfigureAwait(false);
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
