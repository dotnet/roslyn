// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Serialization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote;

/// <summary>
/// See <see cref="RemoteHostAssetWriter"/> for an explanation of the wire format we use when communicating assets
/// between the host and our OOP server.  This implements the code for reading assets transmitted over the wire.  <see
/// cref="RemoteHostAssetWriter"/> has the code for writing assets.
/// </summary>
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
        // ⚠ DO NOT AWAIT INSIDE THE USING BLOCK LEXICALLY (it's fine to await within the call to
        // ReadDataSuppressedFlowAsync). The Dispose method that restores ExecutionContext flow must run on the same
        // thread where SuppressFlow was originally run.
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

        // Ensure that no invariants were broken and that both sides of the communication channel are talking about the
        // same pinned solution.
        var responseSolutionChecksum = await ReadChecksumFromPipeReaderAsync(cancellationToken).ConfigureAwait(false);
        Contract.ThrowIfFalse(_solutionChecksum == responseSolutionChecksum);

        // Now actually read all the messages we expect to get.
        for (var i = 0; i < _objectCount; i++)
            await ReadSingleMessageAsync(objectReader, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask ReadSingleMessageAsync(ObjectReader objectReader, CancellationToken cancellationToken)
    {
        // For each message, read the sentinel byte and the length of the data chunk we'll be reading.
        var length = await CheckSentinelByteAndReadLengthAsync(cancellationToken).ConfigureAwait(false);

        // Now buffer in the rest of the data we need to read.  Because we're reading as much data in as we'll need to
        // consume, all further reading (for this single item) can handle synchronously without worrying about this
        // blocking the reading thread on cross-process pipe io.
        var fillReadResult = await _pipeReader.ReadAtLeastAsync(length, cancellationToken).ConfigureAwait(false);

        // Note: we have let the pipe reader know that we're done with 'read at least' call, but that we haven't
        // consumed anything from it yet.  Otherwise it will throw that another read can't start the objectReader
        // reading calls below.
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

    // Note on Checksum itself as it depends on SequenceReader, which is provided by nerdbank.streams on netstandard2.0
    // (which the Workspace layer does not depend on).
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
