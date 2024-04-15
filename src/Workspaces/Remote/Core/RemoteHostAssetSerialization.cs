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

using static SerializableBytes;
using static SolutionAssetStorage;
using ChecksumAndAsset = (Checksum checksum, object asset);

/// <summary>
/// Contains the utilities for writing assets from the host to a pipe-writer and for reading those assets on the
/// server.  The format we use is as follows.  For each asset we're writing we write:
/// <code>
/// -------------------------------------------------------------------------
/// | sentinel (1 byte) | length of data (4 bytes) | data (variable length) |
/// -------------------------------------------------------------------------
/// </code>
/// The writing code will write out the sentinel-byte and data-length, ensuring it is flushed to the pipe-writer. This
/// allows the pipe-reader to immediately read that information so it can then pre-allocate the space for the data to go
/// into. After writing the data the writer will also flush, so the reader can then read the data out of the pipe into
/// its buffer.  Once present in the reader's buffer, synchronous deserialization can happen without any sync-over-async
/// blocking on async-io.
/// <para/> The sentinel byte serves to let us detect immediately on the reading side if something has gone wrong with
/// this system.
/// <para/> In order to be able to write out the data-length, the writer will first synchronously write the asset to an
/// in-memory buffer, then write that buffer's length to the pipe-writer, then copy the in-memory buffer to the writer.
/// <para/> When writing/reading the data-segment, we use an the <see cref="ObjectWriter"/>/<see cref="ObjectReader"/>
/// subsystem.  This will write its own validation bits, and then the data describing the asset.  This data is:
/// <code>
/// ----------------------------------------------------------------------------------------------------------
/// | data (variable length)                                                                                 |
/// ----------------------------------------------------------------------------------------------------------
/// | ObjectWriter validation (2 bytes) | checksum (16 bytes) | kind (1 byte) | asset-data (asset specified) |
/// ----------------------------------------------------------------------------------------------------------
/// </code>
/// The validation bytes are followed by the checksum.  The checksum is needed in the message as assets can be found in
/// any order (they are not reported in the order of the array of checksums passed into the writing method). Following
/// this is the kind of the asset.  This kind is used by the reading code to know which asset-deserialization routine to
/// invoke. Finally, the asset data itself is written out.
/// </summary>
internal readonly struct RemoteHostAssetWriter(
    PipeWriter pipeWriter, Scope scope, AssetPath assetPath, ReadOnlyMemory<Checksum> checksums, ISerializerService serializer)
{
    /// <summary>
    /// A sentinel byte we place between messages.  Ensures we can detect when something has gone wrong as soon as
    /// possible. Note: the value we pick is neither ascii nor extended ascii.  So it's very unlikely to appear
    /// accidentally.
    /// </summary>
    public const byte MessageSentinelByte = 0b10010000;

    private static readonly ObjectPool<ReadWriteStream> s_streamPool = new(CreateWritableStream);

    private static readonly UnboundedChannelOptions s_channelOptions = new()
    {
        // We have a single task reading the data from the channel and writing it to the pipe.  This option allows the
        // channel to operate in a more efficient manner knowing it won't have to synchronize data for multiple readers.
        SingleReader = true,

        // Currently we only have a single writer writing to the channel when we call FindAllAssetsAsync. However, we
        // could change this in the future to allow the search to happen in parallel.
        SingleWriter = true,
    };

    private readonly PipeWriter _pipeWriter = pipeWriter;
    private readonly Scope _scope = scope;
    private readonly AssetPath _assetPath = assetPath;
    private readonly ReadOnlyMemory<Checksum> _checksums = checksums;
    private readonly ISerializerService _serializer = serializer;

    public async ValueTask WriteDataAsync(CancellationToken cancellationToken)
    {
        // Create a channel to communicate between the searching and writing tasks.  This allows the searching task to
        // find items, add them to the channel synchronously, and immediately continue searching for more items.
        // Concurrently, the writing task can read from the channel and write the items to the pipe-writer.
        var channel = Channel.CreateUnbounded<ChecksumAndAsset>(s_channelOptions);

        // When cancellation happens, attempt to close the channel.  That will unblock the task writing the assets
        // to the pipe. Capture-free version is only available on netcore unfortunately.
        using var _ = cancellationToken.Register(
#if NET
            static (obj, cancellationToken) => ((Channel<ChecksumAndAsset>)obj!).Writer.TryComplete(new OperationCanceledException(cancellationToken)),
            state: channel);
#else
            () => channel.Writer.TryComplete(new OperationCanceledException(cancellationToken)));
#endif

        // Spin up a task to go search for all the requested checksums, adding results to the channel.
        var findAssetsTask = FindAssetsFromScopeAndWriteToChannelAsync(channel.Writer, cancellationToken);

        // Spin up a task to read from the channel and write out the assets to the pipe-writer.
        var writeAssetsTask = ReadAssetsFromChannelAndWriteToPipeAsync(channel.Reader, cancellationToken);

        // Wait for both the searching and writing tasks to finish.
        await Task.WhenAll(findAssetsTask, writeAssetsTask).ConfigureAwait(false);
    }

    private async Task FindAssetsFromScopeAndWriteToChannelAsync(ChannelWriter<ChecksumAndAsset> channelWriter, CancellationToken cancellationToken)
    {
        Exception? exception = null;
        try
        {
            await Task.Yield();

            await _scope.FindAssetsAsync(
                _assetPath, _checksums,
                // It's ok to use TryWrite here.  TryWrite always succeeds unless the channel is completed. And the
                // channel is only ever completed by us (after FindAssetsAsync completed) or if cancellation
                // happens.  In that latter case, it's ok for writing to the channel to do nothing as we no longer
                // need to write out those assets to the pipe.
                static (checksum, asset, channelWriter) => channelWriter.TryWrite((checksum, asset)),
                channelWriter, cancellationToken).ConfigureAwait(false);
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

    private async Task ReadAssetsFromChannelAndWriteToPipeAsync(ChannelReader<ChecksumAndAsset> channelReader, CancellationToken cancellationToken)
    {
        await Task.Yield();

        // Get the in-memory buffer and object-writer we'll use to serialize the assets into.  Don't write any
        // validation bytes at this point in time.  We'll write them between each asset we write out.  Using a
        // single object writer across all assets means we get the benefit of string deduplication across all assets
        // we write out.
        using var pooledStream = s_streamPool.GetPooledObject();
        using var objectWriter = new ObjectWriter(pooledStream.Object, leaveOpen: true, writeValidationBytes: false);

        // This information is not actually needed on the receiving end.  However, we still send it so that the
        // receiver can assert that both sides are talking about the same solution snapshot and no weird invariant
        // breaks have occurred.
        _scope.SolutionChecksum.WriteTo(_pipeWriter);
        await _pipeWriter.FlushAsync(cancellationToken).ConfigureAwait(false);

        // Keep track of how many checksums we found.  We must find all the checksums we were asked to find.
        var foundChecksumCount = 0;

        while (await channelReader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
        {
            while (channelReader.TryRead(out var item))
            {
                await WriteSingleAssetToPipeAsync(
                    pooledStream.Object, objectWriter, item.checksum, item.asset, cancellationToken).ConfigureAwait(false);
                foundChecksumCount++;
            }
        }

        cancellationToken.ThrowIfCancellationRequested();

        // If we weren't canceled, we better have found and written out all the expected assets.
        Contract.ThrowIfTrue(foundChecksumCount != _checksums.Length);
    }

    private async ValueTask WriteSingleAssetToPipeAsync(
        ReadWriteStream tempStream, ObjectWriter objectWriter, Checksum checksum, object asset, CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(asset);

        // We're about to send a message.  Write out our sentinel byte to ensure the reading side can detect
        // problems with our writing.
        WriteSentinelByteToPipeWriter();

        // Write the asset to a temporary buffer so we can calculate its length.  Note: as this is an in-memory
        // temporary buffer, we don't have to worry about synchronous writes on it blocking on the pipe-writer.
        // Instead, we'll handle the pipe-writing ourselves afterwards in a completely async fashion.
        WriteAssetToTempStream(tempStream, objectWriter, checksum, asset, cancellationToken);

        // Write the length of the asset to the pipe writer so the reader knows how much data to read.
        WriteTempStreamLengthToPipeWriter(tempStream);

        // Ensure we flush out the length so the reading side can immediately read the header to determine how much
        // data to it will need to prebuffer.
        await _pipeWriter.FlushAsync(cancellationToken).ConfigureAwait(false);

        // Now, asynchronously copy the temp buffer over to the writer stream.
        tempStream.Position = 0;
        await tempStream.CopyToAsync(_pipeWriter, cancellationToken).ConfigureAwait(false);

        // We flush after each item as that forms a reasonably sized chunk of data to want to then send over the pipe
        // for the reader on the other side to read.  This allows the item-writing to remain entirely synchronous
        // without any blocking on async flushing, while also ensuring that we're not buffering the entire stream of
        // data into the pipe before it gets sent to the other side.
        await _pipeWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private void WriteAssetToTempStream(
        ReadWriteStream tempStream, ObjectWriter objectWriter, Checksum checksum, object asset, CancellationToken cancellationToken)
    {
        // Reset the temp stream to the beginning and clear it out. Don't truncate the stream as we're going to be
        // writing to it multiple times.  This will allow us to reuse the internal chunks of the buffer, without
        // having to reallocate them over and over again. Note: this stream internally keeps a list of byte[]s that
        // it writes to.  Each byte[] is less than the LOH size, so there's no concern about LOH fragmentation here.
        tempStream.Position = 0;
        tempStream.SetLength(0, truncate: false);

        // Write out the object writer validation bytes.  This will help us detect issues when reading if we've
        // screwed something up.
        objectWriter.WriteValidationBytes();

        // Write the checksum for the asset we're writing out, so the other side knows what asset this is.
        checksum.WriteTo(objectWriter);

        // Write out the kind so the receiving end knows how to deserialize this asset.
        objectWriter.WriteByte((byte)asset.GetWellKnownSynchronizationKind());

        // Now serialize out the asset itself.
        _serializer.Serialize(asset, objectWriter, _scope.ReplicationContext, cancellationToken);
    }

    private void WriteSentinelByteToPipeWriter()
    {
        var span = _pipeWriter.GetSpan(1);
        span[0] = MessageSentinelByte;
        _pipeWriter.Advance(1);
    }

    private void WriteTempStreamLengthToPipeWriter(ReadWriteStream tempStream)
    {
        var length = tempStream.Length;
        Contract.ThrowIfTrue(length > int.MaxValue);

        var span = _pipeWriter.GetSpan(sizeof(int));
        BinaryPrimitives.WriteInt32LittleEndian(span, (int)length);
        _pipeWriter.Advance(sizeof(int));
    }
}

internal readonly struct RemoteHostAssetReader<T, TArg>(
    PipeReader pipeReader,
    Checksum solutionChecksum,
    int objectCount,
    ISerializerService serializer,
    Action<Checksum, T, TArg> callback,
    TArg arg)
{
    private readonly PipeReader _pipeReader = pipeReader;
    private readonly Checksum _solutionChecksum = solutionChecksum;
    private readonly int _objectCount = objectCount;
    private readonly ISerializerService _serializer = serializer;
    private readonly Action<Checksum, T, TArg> _callback = callback;
    private readonly TArg _arg = arg;

    public ValueTask ReadDataAsync(CancellationToken cancellationToken)
    {
        // Suppress ExecutionContext flow for asynchronous operations operate on the pipe. In addition to avoiding
        // ExecutionContext allocations, this clears the LogicalCallContext and avoids the need to clone data set by
        // CallContext.LogicalSetData at each yielding await in the task tree.
        //
        // ⚠ DO NOT AWAIT INSIDE THE USING. The Dispose method that restores ExecutionContext flow must run on the
        // same thread where SuppressFlow was originally run.
        using var _ = FlowControlHelper.TrySuppressFlow();
        return ReadDataSuppressedFlowAsync(cancellationToken);
    }

    private async ValueTask ReadDataSuppressedFlowAsync(CancellationToken cancellationToken)
    {
        using var pipeReaderStream = _pipeReader.AsStream(leaveOpen: true);

        // Get an object reader over the stream.  Note: we do not check the validation bytes here as the stream is
        // currently pointing at header data prior to the object data.  Instead, we will check the validation bytes
        // prior to reading each asset out.
        using var objectReader = ObjectReader.GetReader(pipeReaderStream, leaveOpen: true, checkValidationBytes: false);

        // Ensure that no invariants were broken and that both sides of the communication channel are talking about
        // the same pinned solution.
        var responseSolutionChecksum = await ReadChecksumFromPipeReaderAsync(cancellationToken).ConfigureAwait(false);
        Contract.ThrowIfFalse(_solutionChecksum == responseSolutionChecksum);

        // Now actually read all the messages we expect to get.
        for (int i = 0, n = _objectCount; i < n; i++)
            await ReadSingleMessageAsync(objectReader, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask ReadSingleMessageAsync(ObjectReader objectReader, CancellationToken cancellationToken)
    {
        // For each message, read the sentinel byte and the length of the data chunk we'll be reading.
        var length = await CheckSentinelByteAndReadLengthAsync(cancellationToken).ConfigureAwait(false);

        // Now buffer in the rest of the data we need to read.  Because we're reading as much data in as
        // we'll need to consume, all further reading (for this single item) can handle synchronously
        // without worrying about this blocking the reading thread on cross-process pipe io.
        var fillReadResult = await _pipeReader.ReadAtLeastAsync(length, cancellationToken).ConfigureAwait(false);

        // Note: we have let the pipe reader know that we're done with 'read at least' call, but that we
        // haven't consumed anything from it yet.  Otherwise it will throw that another read can't start
        // from within ObjectReader.GetReader below.
        _pipeReader.AdvanceTo(fillReadResult.Buffer.Start);

        // Let the object reader do it's own individual object checking.
        objectReader.CheckValidationBytes();

        // Now do the actual read of the data, synchronously, from the buffers that are now in memory within our
        // process.  These reads will move the pipe-reader forward, without causing any blocking on async-io.
        var checksum = Checksum.ReadFrom(objectReader);
        var kind = (WellKnownSynchronizationKind)objectReader.ReadByte();

        var asset = _serializer.Deserialize(kind, objectReader, cancellationToken);
        Contract.ThrowIfNull(asset);
        _callback(checksum, (T)asset, _arg);
    }

    private async ValueTask<int> CheckSentinelByteAndReadLengthAsync(CancellationToken cancellationToken)
    {
        const int HeaderSize = sizeof(byte) + sizeof(int);

        var lengthReadResult = await _pipeReader.ReadAtLeastAsync(HeaderSize, cancellationToken).ConfigureAwait(false);
        var (sentinelByte, length) = ReadSentinelAndLength(lengthReadResult);

        // Check that the sentinel is correct, and move the pipe reader forward to the end of the header.
        Contract.ThrowIfTrue(sentinelByte != RemoteHostAssetWriter.MessageSentinelByte);
        _pipeReader.AdvanceTo(lengthReadResult.Buffer.GetPosition(HeaderSize));

        return length;
    }

    // Note on Checksum itself as it depends on SequenceReader, which is provided by nerdbank.streams on
    // netstandard2.0 (which the Workspace layer does not depend on).
    private async ValueTask<Checksum> ReadChecksumFromPipeReaderAsync(CancellationToken cancellationToken)
    {
        var readChecksumResult = await _pipeReader.ReadAtLeastAsync(Checksum.HashSize, cancellationToken).ConfigureAwait(false);

        var checksum = ReadChecksum(readChecksumResult);
        _pipeReader.AdvanceTo(readChecksumResult.Buffer.GetPosition(Checksum.HashSize));
        return checksum;
    }

    private static (byte, int) ReadSentinelAndLength(ReadResult readResult)
    {
        var sequenceReader = new SequenceReader<byte>(readResult.Buffer);
        Contract.ThrowIfFalse(sequenceReader.TryRead(out var sentinel));
        Contract.ThrowIfFalse(sequenceReader.TryReadLittleEndian(out int length));
        return (sentinel, length);
    }

    private static Checksum ReadChecksum(ReadResult readResult)
    {
        var sequenceReader = new SequenceReader<byte>(readResult.Buffer);
        Span<byte> checksumBytes = stackalloc byte[Checksum.HashSize];
        Contract.ThrowIfFalse(sequenceReader.TryCopyTo(checksumBytes));
        return Checksum.From(checksumBytes);
    }
}
