// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Hashing;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Serialization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

// various factory methods. all these are just helper methods
internal readonly partial record struct Checksum
{
    private const int XXHash128SizeBytes = 128 / 8;

    private static readonly ObjectPool<XxHash128> s_incrementalHashPool =
        new(() => new(), size: 20);

    // Pool of ObjectWriters to reduce allocations. The pool size is intentionally small as the writers are used for such
    // a short period that concurrent usage of different items from the pool is infrequent.
    private static readonly ObjectPool<ObjectWriter> s_objectWriterPool =
        new(() => new(SerializableBytes.CreateWritableStream(), leaveOpen: true, writeValidationBytes: true), size: 4);

    public static Checksum Create(IEnumerable<string?> values)
    {
        using var pooledHash = s_incrementalHashPool.GetPooledObject();

        foreach (var value in values)
        {
            pooledHash.Object.Append(MemoryMarshal.AsBytes(value.AsSpan()));
            pooledHash.Object.Append(MemoryMarshal.AsBytes("\0".AsSpan()));
        }

        Span<byte> hash = stackalloc byte[XXHash128SizeBytes];
        pooledHash.Object.GetHashAndReset(hash);
        return From(hash);
    }

    public static Checksum Create(ImmutableArray<string> values)
        => Create(ImmutableCollectionsMarshal.AsArray(values).AsSpan());

    public static Checksum Create(ReadOnlySpan<string> values)
    {
        using var pooledHash = s_incrementalHashPool.GetPooledObject();

        foreach (var value in values)
        {
            pooledHash.Object.Append(MemoryMarshal.AsBytes(value.AsSpan()));
            pooledHash.Object.Append(MemoryMarshal.AsBytes("\0".AsSpan()));
        }

        Span<byte> hash = stackalloc byte[XXHash128SizeBytes];
        pooledHash.Object.GetHashAndReset(hash);
        return From(hash);
    }

    public static Checksum Create(string? value)
    {
        Span<byte> destination = stackalloc byte[XXHash128SizeBytes];
        XxHash128.Hash(MemoryMarshal.AsBytes(value.AsSpan()), destination);
        return From(destination);
    }

    public static Checksum Create(Stream stream)
    {
        using var pooledHash = s_incrementalHashPool.GetPooledObject();
        pooledHash.Object.Append(stream);

        Span<byte> hash = stackalloc byte[XXHash128SizeBytes];
        pooledHash.Object.GetHashAndReset(hash);
        return From(hash);
    }

    public static Checksum Create<T>(T @object, Action<T, ObjectWriter> writeObject)
    {
        // Obtain a writer from the pool
        var objectWriter = s_objectWriterPool.Allocate();

        // Invoke the callback to Write object into objectWriter
        writeObject(@object, objectWriter);

        // Include validation bytes in the new checksum from the stream
        var stream = objectWriter.BaseStream;
        stream.Position = 0;
        var newChecksum = Create(stream);

        // Reset object writer back to it's initial state, including the validation bytes
        objectWriter.Reset();
        objectWriter.WriteValidationBytes();

        // Release the writer back to the pool
        s_objectWriterPool.Free(objectWriter);

        return newChecksum;
    }

    public static Checksum Create(Checksum checksum1, Checksum checksum2)
        => Create(stackalloc[] { checksum1, checksum2 });

    public static Checksum Create(Checksum checksum1, Checksum checksum2, Checksum checksum3)
        => Create(stackalloc[] { checksum1, checksum2, checksum3 });

    public static Checksum Create(Checksum checksum1, Checksum checksum2, Checksum checksum3, Checksum checksum4)
        => Create(stackalloc[] { checksum1, checksum2, checksum3, checksum4 });

    public static Checksum Create(ReadOnlySpan<Checksum> hashes)
    {
        Span<byte> destination = stackalloc byte[XXHash128SizeBytes];
        XxHash128.Hash(MemoryMarshal.AsBytes(hashes), destination);
        return From(destination);
    }

    public static Checksum Create(ArrayBuilder<Checksum> checksums)
    {
        // Max alloc 1 KB on stack
        const int maxStackAllocCount = 1024 / Checksum.HashSize;

        var checksumsCount = checksums.Count;
        if (checksumsCount <= maxStackAllocCount)
        {
            Span<Checksum> hashes = stackalloc Checksum[checksumsCount];
            for (var i = 0; i < checksumsCount; i++)
                hashes[i] = checksums[i];

            return Create(hashes);
        }
        else
        {
            using var pooledHash = s_incrementalHashPool.GetPooledObject();
            Span<Checksum> checksumsSpan = stackalloc Checksum[maxStackAllocCount];
            var checksumsIndex = 0;

            while (checksumsIndex < checksumsCount)
            {
                var count = Math.Min(maxStackAllocCount, checksumsCount - checksumsIndex);

                for (var checksumsSpanIndex = 0; checksumsSpanIndex < count; checksumsSpanIndex++, checksumsIndex++)
                    checksumsSpan[checksumsSpanIndex] = checksums[checksumsIndex];

                var hashSpan = checksumsSpan.Slice(0, count);
                pooledHash.Object.Append(MemoryMarshal.AsBytes(hashSpan));
            }

            Span<byte> hash = stackalloc byte[XXHash128SizeBytes];
            pooledHash.Object.GetHashAndReset(hash);
            return From(hash);
        }
    }

    public static Checksum Create(ImmutableArray<Checksum> checksums)
    {
        var hashes = ImmutableCollectionsMarshal.AsArray(checksums).AsSpan();

        return Create(hashes);
    }

    public static Checksum Create(ImmutableArray<byte> bytes)
        => Create(ImmutableCollectionsMarshal.AsArray(bytes).AsSpan());

    public static Checksum Create(ReadOnlySpan<byte> bytes)
    {
        Span<byte> destination = stackalloc byte[XXHash128SizeBytes];
        XxHash128.Hash(bytes, destination);
        return From(destination);
    }

    public static Checksum Create<T>(T value, ISerializerService serializer, CancellationToken cancellationToken)
        => Create(
            (value, serializer, cancellationToken),
            static (tuple, writer) =>
            {
                var (value, serializer, cancellationToken) = tuple;
                serializer.Serialize(value!, writer, cancellationToken);
            });
}
