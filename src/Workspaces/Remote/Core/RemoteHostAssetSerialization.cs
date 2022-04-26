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
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    internal static class RemoteHostAssetSerialization
    {
        internal static readonly PipeOptions PipeOptionsWithUnlimitedWriterBuffer = new(pauseWriterThreshold: long.MaxValue);

        public static void WriteData(
            ObjectWriter writer,
            SolutionAsset? singleAsset,
            IReadOnlyDictionary<Checksum, SolutionAsset>? assetMap,
            ISerializerService serializer,
            SolutionReplicationContext context,
            Checksum solutionChecksum,
            Checksum[] checksums,
            CancellationToken cancellationToken)
        {
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
            }

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
            // We can cancel at entry, but once the pipe operations are scheduled we rely on both operations running to
            // avoid deadlocks (the exception handler in 'copyTask' ensures progress is made in the blocking read).
            cancellationToken.ThrowIfCancellationRequested();
            var mustNotCancelToken = CancellationToken.None;

            // Workaround for https://github.com/AArnott/Nerdbank.Streams/issues/361
            var mustNotCancelUntilBugFix = CancellationToken.None;

            // Workaround for ObjectReader not supporting async reading.
            // Unless we read from the RPC stream asynchronously and with cancallation support we might deadlock when the server cancels.
            // https://github.com/dotnet/roslyn/issues/47861

            // Use local pipe to avoid blocking the current thread on networking IO.
            var localPipe = new Pipe(PipeOptionsWithUnlimitedWriterBuffer);

            Exception? copyException = null;

            // start a task on a thread pool thread copying from the RPC pipe to a local pipe:
            var copyTask = Task.Run(async () =>
            {
                try
                {
                    await pipeReader.CopyToAsync(localPipe.Writer, mustNotCancelUntilBugFix).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    copyException = e;
                }
                finally
                {
                    await localPipe.Writer.CompleteAsync(copyException).ConfigureAwait(false);
                }
            }, mustNotCancelToken);

            // blocking read from the local pipe on the current thread:
            try
            {
                using var stream = localPipe.Reader.AsStream(leaveOpen: false);
                return ReadData(stream, solutionChecksum, checksums, serializerService, mustNotCancelUntilBugFix);
            }
            catch (EndOfStreamException) when (IsEndOfStreamExceptionExpected(copyException, cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                throw copyException ?? ExceptionUtilities.Unreachable;
            }
            finally
            {
                // Make sure to complete the copy and pipes before returning, otherwise the caller could complete the
                // reader and/or writer while they are still in use.
                await copyTask.ConfigureAwait(false);
            }

            // Local functions
            static bool IsEndOfStreamExceptionExpected(Exception? copyException, CancellationToken cancellationToken)
            {
                // The local pipe is only closed in the 'finally' block of 'copyTask'. If the reader fails with an
                // EndOfStreamException, we known 'copyTask' has already completed its work.
                if (cancellationToken.IsCancellationRequested)
                {
                    // The writer closed early due to a cancellation request.
                    return true;
                }

                if (copyException is not null)
                {
                    // An exception occurred while attempting to copy data to the local pipe. Catch and throw the
                    // exception that occurred during that copy operation.
                    return true;
                }

                // The reader attempted to read more data than was copied to the local pipe. Avoid catching the
                // exception to reveal the faulty read stack in telemetry.
                return false;
            }
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
