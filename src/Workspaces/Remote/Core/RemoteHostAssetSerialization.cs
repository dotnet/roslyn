// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Serialization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote;

/// <summary>
/// Contains the utilities for writing assets from the host to a pipe-writer and for reading those assets on the
/// server.  The format we use is as follows.  For each asset we're writing we write:
/// <code>
/// -------------------------------------------------------------------------
/// | sentinel (1 byte) | length of data (4 bytes) | data (variable length) |
/// -------------------------------------------------------------------------
/// </code>
/// The writing code will write out the sentinel-byte and data-length, ensuring it is flushed to the pipe-writer.
/// This allows the pipe-reader to immediately read that information so it can then pre-allocate the space for the
/// data to go into. After writing the data the writer will also flush, so the reader can then read the data out of
/// the pipe into its buffer.  Once present in the reader's buffer, synchronous deserialization can happen without
/// any sync-over-async blocking on async-io.
/// <para>
/// The sentinel byte serves to let us detect immediately on the reading side if something has gone wrong with this
/// system.
/// </para>
/// <para>
/// In order to be able to write out the data-length, the writer will first synchronously write the asset to an
/// in-memory buffer, then write that buffer's length to the pipe-writer, then copy the in-memory buffer to the
/// writer.
/// </para>
/// When writing/reading the data-segment, we use an the <see cref="ObjectWriter"/>/<see cref="ObjectReader"/>
/// subsystem.  This will write its own validation bits, and then the data describing the asset.  This data is:
/// <code>
/// -----------------------------------------------------------------------------------------------------------
/// | data (variable length)                                                                                  |
/// -----------------------------------------------------------------------------------------------------------
/// | ObjectWriter validation (2 bytes) | checksum (16 bytes) | kind (4 bytes) | asset-data (asset specified) |
/// -----------------------------------------------------------------------------------------------------------
/// </code>
/// The validation bytes are followed by the checksum.  The checksum is needed in the message as assets can be found
/// in any order (they are not reported in the order of the array of checksums passed into the writing method).
/// Following this is the kind of the asset.  This kind is used by the reading code to know which
/// asset-deserialization routine to invoke. Finally, the asset data itself is written out.
/// </summary>
internal static class RemoteHostAssetSerialization
{
    /// <summary>
    /// A sentinel byte we place between messages.  Ensures we can detect when something has gone wrong as soon as possible.
    /// </summary>
    private const byte MessageSentinelByte = 0b01010101;

    private static readonly ObjectPool<SerializableBytes.ReadWriteStream> s_streamPool = new(SerializableBytes.CreateWritableStream);

    public static async ValueTask WriteDataAsync(
        PipeWriter pipeWriter,
        AssetPath assetPath,
        ReadOnlyMemory<Checksum> checksums,
        SolutionAssetStorage.Scope scope,
        ISerializerService serializer,
        CancellationToken cancellationToken)
    {
        // Create a channel to communicate between the searching and writing tasks.  This allows the searching task to
        // find items, add them to the channel synchronously, and immediately continue searching for more items.
        // Concurrently, the writing task can read from the channel and write the items to the pipe-writer.
        var channel = Channel.CreateUnbounded<(Checksum checksum, object asset)>(new UnboundedChannelOptions()
        {
            // We have a single task reading the data from the channel and writing it to the pipe.  This option
            // allows the channel to operate in a more efficient manner knowing it won't have to sychronize data
            // for multiple readers.
            SingleReader = true,

            // Currently we only have a single writer writing to the channel when we call FindAllAssetsAsync.
            // However, we could change this in the future to allow the search to happen in parallel.
            SingleWriter = true,
        });

        // When cancellation happens, attempt to close the channel.  That will unblock the task writing the assets
        // to the pipe.
        using var _ = cancellationToken.Register(() => channel.Writer.TryComplete(new OperationCanceledException(cancellationToken)));

        // Keep track of how many checksums we found.  We must find all the checksums we were asked to find.
        var foundChecksumCount = 0;

        // Spin up a task to go search for all the requested checksums, adding results to the channel.
        var findAssetsTask = FindAllAssetsAsync();

        // Spin up a task to read from the channel and write out the assets to the pipe-writer.
        var writeAssetsTask = WriteAllAssetsToPipeAsync();

        // Wait for both the searching and writing tasks to finish.
        await Task.WhenAll(findAssetsTask, writeAssetsTask).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        // If we weren't canceled, we better have found and written out all the expected assets.
        Contract.ThrowIfTrue(foundChecksumCount != checksums.Length);

        return;

        async Task FindAllAssetsAsync()
        {
            await Task.Yield();

            try
            {
                await scope.FindAssetsAsync(
                    assetPath,
                    checksums,
                    (checksum, asset) => channel.Writer.TryWrite((checksum, asset)),
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // If Something went wrong ensure that we complete the channel so that the writing task will stop.
                // Also bubble the exception out so that the outer Task.WhenAll will bubble it up.
                channel.Writer.TryComplete(ex);
                throw;
            }
            finally
            {
                // We finished searching for all the checksums, let the writer know.
                channel.Writer.TryComplete();
            }
        }

        async Task WriteAllAssetsToPipeAsync()
        {
            await Task.Yield();

            while (await channel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                while (channel.Reader.TryRead(out var item))
                    await WriteSingleAssetToPipeAsync(item.checksum, item.asset, cancellationToken).ConfigureAwait(false);
            }
        }

        async ValueTask WriteSingleAssetToPipeAsync(Checksum checksum, object asset, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(asset);

            foundChecksumCount++;

            // We're about to send a message.  Write out our sentinel byte to ensure the reading side can detect
            // problems with our writing.
            WriteSentinelByte();

            // Write the asset to a temporary buffer so we can calculate its length.  Note: as this is an in-memory
            // temporary buffer, we don't have to worry about synchronous writes on it blocking on the pipe-writer.
            // Instead, we'll handle the pipe-writing ourselves afterwards in a completely async fashion.

            using (var _ = GetTempStream(out var tempStream))
            {
                WriteAssetToTempStream(tempStream, checksum, asset);

                // Write the length of the asset to the pipe writer so the reader knows how much data to read.
                WriteLength(tempStream.Length);

                // Ensure we flush out the length so the reading side can immediately read the header to determine qhow
                // much data to it will need to prebuffer.
                await pipeWriter.FlushAsync(cancellationToken).ConfigureAwait(false);

                // Now, asynchronously copy the temp buffer over to the writer stream.
                tempStream.Position = 0;
                await tempStream.CopyToAsync(pipeWriter, cancellationToken).ConfigureAwait(false);
            }

            // We flush after each item as that forms a reasonably sized chunk of data to want to then send over
            // the pipe for the reader on the other side to read.  This allows the item-writing to remain
            // entirely synchronous without any blocking on async flushing, while also ensuring that we're not
            // buffering the entire stream of data into the pipe before it gets sent to the other side.
            await pipeWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        void WriteAssetToTempStream(Stream tempStream, Checksum checksum, object asset)
        {
            using var writer = new ObjectWriter(tempStream, leaveOpen: true, cancellationToken);
            {
                // Write the checksum for the asset we're writing out, so the other side knows what asset this is.
                checksum.WriteTo(writer);

                // Write out the kind so the receiving end knows how to deserialize this asset.
                var kind = asset.GetWellKnownSynchronizationKind();
                writer.WriteInt32((int)kind);

                // Now serialize out the asset itself.
                serializer.Serialize(asset, writer, scope.ReplicationContext, cancellationToken);
            }
        }

        void WriteSentinelByte()
        {
            var span = pipeWriter.GetSpan(1);
            span[0] = MessageSentinelByte;
            pipeWriter.Advance(1);
        }

        void WriteLength(long length)
        {
            Contract.ThrowIfTrue(length > int.MaxValue);

            var span = pipeWriter.GetSpan(sizeof(int));
            BinaryPrimitives.WriteInt32LittleEndian(span, (int)length);
            pipeWriter.Advance(sizeof(int));
        }

        PooledObject<SerializableBytes.ReadWriteStream> GetTempStream(out Stream stream)
        {
            var pooledObject = s_streamPool.GetPooledObject();
            var tempStream = pooledObject.Object;
            tempStream.Position = 0;

            // Don't truncate the stream as we're going to be writing to it multiple times.  This will allow us to
            // reuse the internal chunks of the buffer, without having to reallocate them over and over again.
            // Note: this stream internally keeps a list of byte[]s that it writes to.  Each byte[] is less than the
            // LOH size, so there's no concern about LOH fragmentation here.
            tempStream.SetLength(0, truncate: false);
            stream = tempStream;
            return pooledObject;
        }
    }

    public static ValueTask ReadDataAsync<T, TArg>(
        PipeReader pipeReader, int objectCount, ISerializerService serializerService, Action<Checksum, T, TArg> callback, TArg arg, CancellationToken cancellationToken)
    {
        // Suppress ExecutionContext flow for asynchronous operations operate on the pipe. In addition to avoiding
        // ExecutionContext allocations, this clears the LogicalCallContext and avoids the need to clone data set by
        // CallContext.LogicalSetData at each yielding await in the task tree.
        //
        // ⚠ DO NOT AWAIT INSIDE THE USING. The Dispose method that restores ExecutionContext flow must run on the
        // same thread where SuppressFlow was originally run.
        using var _ = FlowControlHelper.TrySuppressFlow();
        return ReadDataSuppressedFlowAsync(pipeReader, objectCount, serializerService, callback, arg, cancellationToken);

        static async ValueTask ReadDataSuppressedFlowAsync(
            PipeReader pipeReader, int objectCount, ISerializerService serializerService, Action<Checksum, T, TArg> callback, TArg arg, CancellationToken cancellationToken)
        {
            using var pipeReaderStream = pipeReader.AsStream(leaveOpen: true);

            for (var i = 0; i < objectCount; i++)
            {
                // First, read the sentinel byte and the length of the data chunk we'll be reading.
                const int HeaderSize = sizeof(byte) + sizeof(int);
                var lengthReadResult = await pipeReader.ReadAtLeastAsync(HeaderSize, cancellationToken).ConfigureAwait(false);
                var (sentinelByte, length) = ReadSentinelAndLength(lengthReadResult);

                // Check that the sentinel is correct.
                Contract.ThrowIfTrue(sentinelByte != MessageSentinelByte);

                // If so, move the pipe reader forward to the end of the header.
                pipeReader.AdvanceTo(lengthReadResult.Buffer.GetPosition(HeaderSize));

                // Now buffer in the rest of the data we need to read.  Because we're reading as much data in as
                // we'll need to consume, all further reading (for this single item) can handle synchronously
                // without worrying about this blocking the reading thread on cross-process pipe io.
                var fillReadResult = await pipeReader.ReadAtLeastAsync(length, cancellationToken).ConfigureAwait(false);

                // Note: we have let the pipe reader know that we're done with 'read at least' call, but that we
                // haven't consumed anything from it yet.  Otherwise it will throw that another read can't start
                // from within ObjectReader.GetReader below.
                pipeReader.AdvanceTo(fillReadResult.Buffer.Start);

                // Now do the actual read of the data, synchronously, from the buffers that are now in memory within
                // our process.  These reads will move the pipe-reader forward, without causing any blocking on
                // async-io.
                using var reader = ObjectReader.GetReader(pipeReaderStream, leaveOpen: true, cancellationToken);

                var checksum = Checksum.ReadFrom(reader);
                var kind = (WellKnownSynchronizationKind)reader.ReadInt32();

                // in service hub, cancellation means simply closed stream
                var result = serializerService.Deserialize(kind, reader, cancellationToken);
                Contract.ThrowIfNull(result);
                callback(checksum, (T)result, arg);
            }
        }

        static (byte, int) ReadSentinelAndLength(ReadResult readResult)
        {
            var sequenceReader = new SequenceReader<byte>(readResult.Buffer);
            Contract.ThrowIfFalse(sequenceReader.TryRead(out var sentinel));
            Contract.ThrowIfFalse(sequenceReader.TryReadLittleEndian(out int length));
            return (sentinel, length);
        }
    }
}
