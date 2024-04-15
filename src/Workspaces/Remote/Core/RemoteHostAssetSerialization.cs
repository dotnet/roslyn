// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Serialization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote;

using ChecksumChannel = Channel<(Checksum checksum, object asset)>;
using ChecksumChannelReader = ChannelReader<(Checksum checksum, object asset)>;
using ChecksumChannelWriter = ChannelWriter<(Checksum checksum, object asset)>;

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
/// ----------------------------------------------------------------------------------------------------------
/// | data (variable length)                                                                                 |
/// ----------------------------------------------------------------------------------------------------------
/// | ObjectWriter validation (2 bytes) | checksum (16 bytes) | kind (1 byte) | asset-data (asset specified) |
/// ----------------------------------------------------------------------------------------------------------
/// </code>
/// The validation bytes are followed by the checksum.  The checksum is needed in the message as assets can be found
/// in any order (they are not reported in the order of the array of checksums passed into the writing method).
/// Following this is the kind of the asset.  This kind is used by the reading code to know which
/// asset-deserialization routine to invoke. Finally, the asset data itself is written out.
/// </summary>
internal static class RemoteHostAssetSerialization
{
    /// <summary>
    /// A sentinel byte we place between messages.  Ensures we can detect when something has gone wrong as soon as
    /// possible. Note: the value we pick is neither ascii nor extended ascii.  So it's very unlikely to appear
    /// accidentally.
    /// </summary>
    private const byte MessageSentinelByte = 0b10010000;

    private static readonly ObjectPool<SerializableBytes.ReadWriteStream> s_streamPool = new(SerializableBytes.CreateWritableStream);

    private static readonly UnboundedChannelOptions s_channelOptions = new()
    {
        // We have a single task reading the data from the channel and writing it to the pipe.  This option allows the
        // channel to operate in a more efficient manner knowing it won't have to synchronize data for multiple readers.
        SingleReader = true,

        // Currently we only have a single writer writing to the channel when we call FindAllAssetsAsync. However, we
        // could change this in the future to allow the search to happen in parallel.
        SingleWriter = true,
    };

    public static async ValueTask WriteDataAsync(
        PipeWriter pipeWriter,
        SolutionAssetStorage.Scope scope,
        AssetPath assetPath,
        ReadOnlyMemory<Checksum> checksums,
        ISerializerService serializer,
        CancellationToken cancellationToken)
    {
        // Create a channel to communicate between the searching and writing tasks.  This allows the searching task to
        // find items, add them to the channel synchronously, and immediately continue searching for more items.
        // Concurrently, the writing task can read from the channel and write the items to the pipe-writer.
        var channel = Channel.CreateUnbounded<(Checksum checksum, object asset)>(s_channelOptions);

        // When cancellation happens, attempt to close the channel.  That will unblock the task writing the assets
        // to the pipe. Capture-free version is only available on netcore unfortunately.
#if NET
        using var _ = cancellationToken.Register(
            static (obj, cancellationToken) => ((ChecksumChannel)obj!).Writer.TryComplete(new OperationCanceledException(cancellationToken)),
            state: channel);
#else
        using var _ = cancellationToken.Register(
            () => channel.Writer.TryComplete(new OperationCanceledException(cancellationToken)));
#endif

        // Spin up a task to go search for all the requested checksums, adding results to the channel.
        var findAssetsTask = FindAssetsFromScopeAndWriteToChannelAsync(
            assetPath, scope, checksums, channel.Writer, cancellationToken);

        // Spin up a task to read from the channel and write out the assets to the pipe-writer.
        var writeAssetsTask = ReadAssetsFromChannelAndWriteToPipeAsync(
            pipeWriter, scope, checksums, serializer, channel.Reader, cancellationToken);

        // Wait for both the searching and writing tasks to finish.
        await Task.WhenAll(findAssetsTask, writeAssetsTask).ConfigureAwait(false);

        return;

        static async Task FindAssetsFromScopeAndWriteToChannelAsync(
            AssetPath assetPath,
            SolutionAssetStorage.Scope scope,
            ReadOnlyMemory<Checksum> checksums,
            ChecksumChannelWriter channelWriter,
            CancellationToken cancellationToken)
        {
            Exception? exception = null;
            try
            {
                await Task.Yield();

                await scope.FindAssetsAsync(
                    assetPath,
                    checksums,
                    // It's ok to use TryWrite here.  TryWrite always succeeds unless the channel is completed. And the
                    // channel is only ever completed by us (after FindAssetsAsync completed) or if cancellation
                    // happens.  In that latter case, it's ok for writing to the channel to do nothing as we no longer
                    // need to write out those assets to the pipe.
                    static (checksum, asset, channelWriter) => channelWriter.TryWrite((checksum, asset)),
                    channelWriter,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when ((exception = ex) == null)
            {
                throw ExceptionUtilities.Unreachable();
            }
            finally
            {
                // No matter what path we take (exceptional or non-exceptional), always complete the channel so the
                // writing task knows it's done.
                channelWriter.TryComplete(exception);
            }
        }

        static async Task ReadAssetsFromChannelAndWriteToPipeAsync(
            PipeWriter pipeWriter,
            SolutionAssetStorage.Scope scope,
            ReadOnlyMemory<Checksum> checksums,
            ISerializerService serializer,
            ChecksumChannelReader channelReader,
            CancellationToken cancellationToken)
        {
            await Task.Yield();

            // Get the in-memory buffer and object-writer we'll use to serialize the assets into.  Don't write any
            // validation bytes at this point in time.  We'll write them between each asset we write out.  Using a
            // single object writer across all assets means we get the benefit of string deduplication across all assets
            // we write out.
            using var pooledStream = s_streamPool.GetPooledObject();
            using var writer = new ObjectWriter(pooledStream.Object, leaveOpen: true, writeValidationBytes: false);

            // This information is not actually needed on the receiving end.  However, we still send it so that the
            // receiver can assert that both sides are talking about the same solution snapshot and no weird invariant
            // breaks have occurred.
            scope.SolutionChecksum.WriteTo(pipeWriter);
            await pipeWriter.FlushAsync(cancellationToken).ConfigureAwait(false);

            // Keep track of how many checksums we found.  We must find all the checksums we were asked to find.
            var foundChecksumCount = 0;

            while (await channelReader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                while (channelReader.TryRead(out var item))
                {
                    await WriteSingleAssetToPipeAsync(
                        pipeWriter, pooledStream.Object, writer, item.checksum, item.asset, scope, serializer, cancellationToken).ConfigureAwait(false);
                    foundChecksumCount++;
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            // If we weren't canceled, we better have found and written out all the expected assets.
            Contract.ThrowIfTrue(foundChecksumCount != checksums.Length);
        }
    }

    private static async ValueTask WriteSingleAssetToPipeAsync(
        PipeWriter pipeWriter,
        SerializableBytes.ReadWriteStream tempStream,
        ObjectWriter writer,
        Checksum checksum,
        object asset,
        SolutionAssetStorage.Scope scope,
        ISerializerService serializer,
        CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(asset);

        // We're about to send a message.  Write out our sentinel byte to ensure the reading side can detect
        // problems with our writing.
        WriteSentinelByteToPipeWriter();

        // Write the asset to a temporary buffer so we can calculate its length.  Note: as this is an in-memory
        // temporary buffer, we don't have to worry about synchronous writes on it blocking on the pipe-writer.
        // Instead, we'll handle the pipe-writing ourselves afterwards in a completely async fashion.
        WriteAssetToTempStream();

        // Write the length of the asset to the pipe writer so the reader knows how much data to read.
        WriteTempStreamLengthToPipeWriter();

        // Ensure we flush out the length so the reading side can immediately read the header to determine how much
        // data to it will need to prebuffer.
        await pipeWriter.FlushAsync(cancellationToken).ConfigureAwait(false);

        // Now, asynchronously copy the temp buffer over to the writer stream.
        tempStream.Position = 0;
        await tempStream.CopyToAsync(pipeWriter, cancellationToken).ConfigureAwait(false);

        // We flush after each item as that forms a reasonably sized chunk of data to want to then send over the pipe
        // for the reader on the other side to read.  This allows the item-writing to remain entirely synchronous
        // without any blocking on async flushing, while also ensuring that we're not buffering the entire stream of
        // data into the pipe before it gets sent to the other side.
        await pipeWriter.FlushAsync(cancellationToken).ConfigureAwait(false);

        return;

        void WriteAssetToTempStream()
        {
            // Reset the temp stream to the beginning and clear it out. Don't truncate the stream as we're going to be
            // writing to it multiple times.  This will allow us to reuse the internal chunks of the buffer, without
            // having to reallocate them over and over again. Note: this stream internally keeps a list of byte[]s that
            // it writes to.  Each byte[] is less than the LOH size, so there's no concern about LOH fragmentation here.
            tempStream.Position = 0;
            tempStream.SetLength(0, truncate: false);

            // Write out the object writer validation bytes.  This will help us detect issues when reading if we've
            // screwed something up.
            writer.WriteValidationBytes();

            // Write the checksum for the asset we're writing out, so the other side knows what asset this is.
            checksum.WriteTo(writer);

            // Write out the kind so the receiving end knows how to deserialize this asset.
            writer.WriteByte((byte)asset.GetWellKnownSynchronizationKind());

            // Now serialize out the asset itself.
            serializer.Serialize(asset, writer, scope.ReplicationContext, cancellationToken);
        }

        void WriteSentinelByteToPipeWriter()
        {
            var span = pipeWriter.GetSpan(1);
            span[0] = MessageSentinelByte;
            pipeWriter.Advance(1);
        }

        void WriteTempStreamLengthToPipeWriter()
        {
            var length = tempStream.Length;
            Contract.ThrowIfTrue(length > int.MaxValue);

            var span = pipeWriter.GetSpan(sizeof(int));
            BinaryPrimitives.WriteInt32LittleEndian(span, (int)length);
            pipeWriter.Advance(sizeof(int));
        }
    }

    public static ValueTask ReadDataAsync<T, TArg>(
        PipeReader pipeReader, Checksum solutionChecksum, int objectCount, ISerializerService serializerService, Action<Checksum, T, TArg> callback, TArg arg, CancellationToken cancellationToken)
    {
        // Suppress ExecutionContext flow for asynchronous operations operate on the pipe. In addition to avoiding
        // ExecutionContext allocations, this clears the LogicalCallContext and avoids the need to clone data set by
        // CallContext.LogicalSetData at each yielding await in the task tree.
        //
        // ⚠ DO NOT AWAIT INSIDE THE USING. The Dispose method that restores ExecutionContext flow must run on the
        // same thread where SuppressFlow was originally run.
        using var _ = FlowControlHelper.TrySuppressFlow();
        return ReadDataSuppressedFlowAsync(pipeReader, solutionChecksum, objectCount, serializerService, callback, arg, cancellationToken);

        static async ValueTask ReadDataSuppressedFlowAsync(
            PipeReader pipeReader, Checksum solutionChecksum, int objectCount, ISerializerService serializerService, Action<Checksum, T, TArg> callback, TArg arg, CancellationToken cancellationToken)
        {
            using var pipeReaderStream = pipeReader.AsStream(leaveOpen: true);

            // Get an object reader over the stream.  Note: we do not check the validation bytes here as the stream is
            // currently pointing at header data prior to the object data.  Instead, we will check the validation bytes
            // prior to reading each asset out.
            using var reader = ObjectReader.GetReader(pipeReaderStream, leaveOpen: true, checkValidationBytes: false);

            // Ensure that no invariants were broken and that both sides of the communication channel are talking about
            // the same pinned solution.
            var responseSolutionChecksum = await ReadChecksumFromPipeReaderAsync(pipeReader, cancellationToken).ConfigureAwait(false);
            Contract.ThrowIfFalse(solutionChecksum == responseSolutionChecksum);

            for (int i = 0, n = objectCount; i < n; i++)
            {
                // For each message, read the sentinel byte and the length of the data chunk we'll be reading.
                const int HeaderSize = sizeof(byte) + sizeof(int);
                var lengthReadResult = await pipeReader.ReadAtLeastAsync(HeaderSize, cancellationToken).ConfigureAwait(false);
                var (sentinelByte, length) = ReadSentinelAndLength(lengthReadResult);

                // Check that the sentinel is correct, and move the pipe reader forward to the end of the header.
                Contract.ThrowIfTrue(sentinelByte != MessageSentinelByte);
                pipeReader.AdvanceTo(lengthReadResult.Buffer.GetPosition(HeaderSize));

                // Now buffer in the rest of the data we need to read.  Because we're reading as much data in as
                // we'll need to consume, all further reading (for this single item) can handle synchronously
                // without worrying about this blocking the reading thread on cross-process pipe io.
                var fillReadResult = await pipeReader.ReadAtLeastAsync(length, cancellationToken).ConfigureAwait(false);

                // Note: we have let the pipe reader know that we're done with 'read at least' call, but that we
                // haven't consumed anything from it yet.  Otherwise it will throw that another read can't start
                // from within ObjectReader.GetReader below.
                pipeReader.AdvanceTo(fillReadResult.Buffer.Start);

                var (checksum, asset) = ReadSingleAssetFromObjectReader(reader, serializerService, cancellationToken);
                callback(checksum, asset, arg);
            }
        }

        static (byte, int) ReadSentinelAndLength(ReadResult readResult)
        {
            var sequenceReader = new SequenceReader<byte>(readResult.Buffer);
            Contract.ThrowIfFalse(sequenceReader.TryRead(out var sentinel));
            Contract.ThrowIfFalse(sequenceReader.TryReadLittleEndian(out int length));
            return (sentinel, length);
        }

        static (Checksum checksum, T asset) ReadSingleAssetFromObjectReader(
            ObjectReader reader, ISerializerService serializerService, CancellationToken cancellationToken)
        {
            // Let the object reader do it's own individual object checking.
            reader.CheckValidationBytes();

            // Now do the actual read of the data, synchronously, from the buffers that are now in memory within our
            // process.  These reads will move the pipe-reader forward, without causing any blocking on async-io.
            var checksum = Checksum.ReadFrom(reader);
            var kind = (WellKnownSynchronizationKind)reader.ReadByte();

            var result = serializerService.Deserialize(kind, reader, cancellationToken);
            Contract.ThrowIfNull(result);
            return (checksum, (T)result);
        }

        // Note on Checksum itself as it depends on SequenceReader, which is provided by nerdbank.streams on
        // netstandard2.0 (which the Workspace layer does not depend on).
        static async ValueTask<Checksum> ReadChecksumFromPipeReaderAsync(PipeReader pipeReader, CancellationToken cancellationToken)
        {
            var readChecksumResult = await pipeReader.ReadAtLeastAsync(Checksum.HashSize, cancellationToken).ConfigureAwait(false);

            var checksum = ReadChecksum(readChecksumResult);
            pipeReader.AdvanceTo(readChecksumResult.Buffer.GetPosition(Checksum.HashSize));
            return checksum;

            static Checksum ReadChecksum(ReadResult readResult)
            {
                var sequenceReader = new SequenceReader<byte>(readResult.Buffer);
                Span<byte> checksumBytes = stackalloc byte[Checksum.HashSize];
                Contract.ThrowIfFalse(sequenceReader.TryCopyTo(checksumBytes));
                return Checksum.From(checksumBytes);
            }
        }
    }
}
