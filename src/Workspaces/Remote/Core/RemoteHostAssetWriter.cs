// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Shared.Utilities;
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

    private static readonly ObjectPool<ReadWriteStream> s_streamPool = new(() => new());

    private readonly PipeWriter _pipeWriter = pipeWriter;
    private readonly Scope _scope = scope;
    private readonly AssetPath _assetPath = assetPath;
    private readonly ReadOnlyMemory<Checksum> _checksums = checksums;
    private readonly ISerializerService _serializer = serializer;

    public Task WriteDataAsync(CancellationToken cancellationToken)
        => ProducerConsumer<ChecksumAndAsset>.RunAsync(
            ProducerConsumerOptions.SingleReaderWriterOptions,
            produceItems: static (onItemFound, @this, cancellationToken) => @this.FindAssetsAsync(onItemFound, cancellationToken),
            consumeItems: static (items, @this, cancellationToken) => @this.WriteBatchToPipeAsync(items, cancellationToken),
            args: this,
            cancellationToken);

    private Task FindAssetsAsync(Action<ChecksumAndAsset> onItemFound, CancellationToken cancellationToken)
        => _scope.FindAssetsAsync(
            _assetPath, _checksums,
            static (checksum, asset, onItemFound) => onItemFound((checksum, asset)),
            onItemFound, cancellationToken);

    private async Task WriteBatchToPipeAsync(
        IAsyncEnumerable<ChecksumAndAsset> checksumsAndAssets, CancellationToken cancellationToken)
    {
        // Get the in-memory buffer and object-writer we'll use to serialize the assets into.  Don't write any
        // validation bytes at this point in time.  We'll write them between each asset we write out.  Using a single
        // object writer across all assets means we get the benefit of string deduplication across all assets we write
        // out.
        using var pooledStream = s_streamPool.GetPooledObject();
        using var objectWriter = new ObjectWriter(pooledStream.Object, leaveOpen: true, writeValidationBytes: false);

        // This information is not actually needed on the receiving end.  However, we still send it so that the receiver
        // can assert that both sides are talking about the same solution snapshot and no weird invariant breaks have
        // occurred.
        _scope.SolutionChecksum.WriteTo(_pipeWriter);
        await _pipeWriter.FlushAsync(cancellationToken).ConfigureAwait(false);

        // Keep track of how many checksums we found.  We must find all the checksums we were asked to find.
        var foundChecksumCount = 0;

        await foreach (var (checksum, asset) in checksumsAndAssets)
        {
            await WriteSingleAssetToPipeAsync(
                pooledStream.Object, objectWriter, checksum, asset, cancellationToken).ConfigureAwait(false);
            foundChecksumCount++;
        }

        cancellationToken.ThrowIfCancellationRequested();

        // If we weren't canceled, we better have found and written out all the expected assets.
        Contract.ThrowIfTrue(foundChecksumCount != _checksums.Length);
    }

    private async ValueTask WriteSingleAssetToPipeAsync(
        ReadWriteStream tempStream, ObjectWriter objectWriter, Checksum checksum, object asset, CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(asset);

        // We're about to send a message.  Write out our sentinel byte to ensure the reading side can detect problems
        // with our writing.
        WriteSentinelByteToPipeWriter();

        // Write the asset to a temporary buffer so we can calculate its length.  Note: as this is an in-memory
        // temporary buffer, we don't have to worry about synchronous writes on it blocking on the pipe-writer. Instead,
        // we'll handle the pipe-writing ourselves afterwards in a completely async fashion.
        WriteAssetToTempStream(tempStream, objectWriter, checksum, asset, cancellationToken);

        // Write the length of the asset to the pipe writer so the reader knows how much data to read.
        WriteTempStreamLengthToPipeWriter(tempStream);

        // Ensure we flush out the length so the reading side can immediately read the header to determine how much data
        // to it will need to prebuffer.
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
        // writing to it multiple times.  This will allow us to reuse the internal chunks of the buffer, without having
        // to reallocate them over and over again. Note: this stream internally keeps a list of byte[]s that it writes
        // to.  Each byte[] is less than the LOH size, so there's no concern about LOH fragmentation here.
        tempStream.Position = 0;
        tempStream.SetLength(0, truncate: false);

        // Write out the object writer validation bytes.  This will help us detect issues when reading if we've screwed
        // something up.
        objectWriter.WriteValidationBytes();

        // Write the checksum for the asset we're writing out, so the other side knows what asset this is.
        checksum.WriteTo(objectWriter);

        // Write out the kind so the receiving end knows how to deserialize this asset.
        objectWriter.WriteByte((byte)asset.GetWellKnownSynchronizationKind());

        // Now serialize out the asset itself.
        _serializer.Serialize(asset, objectWriter, cancellationToken);
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
