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
        using var stream = SerializableBytes.CreateWritableStream();

        using (var objectWriter = new ObjectWriter(stream, leaveOpen: true))
        {
            writeObject(@object, objectWriter);
        }

        stream.Position = 0;
        return Create(stream);
    }

    public static Checksum Create(Checksum checksum1, Checksum checksum2)
        => Create(stackalloc[] { checksum1, checksum2 });

    public static Checksum Create(Checksum checksum1, Checksum checksum2, Checksum checksum3)
        => Create(stackalloc[] { checksum1, checksum2, checksum3 });

    public static Checksum Create(ReadOnlySpan<Checksum> hashes)
    {
        Span<byte> destination = stackalloc byte[XXHash128SizeBytes];
        XxHash128.Hash(MemoryMarshal.AsBytes(hashes), destination);
        return From(destination);
    }

    public static Checksum Create(ArrayBuilder<Checksum> checksums)
    {
        using var stream = SerializableBytes.CreateWritableStream();

        using (var writer = new ObjectWriter(stream, leaveOpen: true))
        {
            foreach (var checksum in checksums)
                checksum.WriteTo(writer);
        }

        stream.Position = 0;
        return Create(stream);
    }

    public static Checksum Create(ImmutableArray<Checksum> checksums)
    {
        using var stream = SerializableBytes.CreateWritableStream();

        using (var writer = new ObjectWriter(stream, leaveOpen: true))
        {
            foreach (var checksum in checksums)
                checksum.WriteTo(writer);
        }

        stream.Position = 0;
        return Create(stream);
    }

    public static Checksum Create(ImmutableArray<byte> bytes)
    {
        using var stream = SerializableBytes.CreateWritableStream();

        using (var writer = new ObjectWriter(stream, leaveOpen: true))
        {
            for (var i = 0; i < bytes.Length; i++)
                writer.WriteByte(bytes[i]);
        }

        stream.Position = 0;
        return Create(stream);
    }

    public static Checksum Create<T>(T value, ISerializerService serializer)
    {
        using var stream = SerializableBytes.CreateWritableStream();
        using var context = new SolutionReplicationContext();

        using (var objectWriter = new ObjectWriter(stream, leaveOpen: true))
        {
            serializer.Serialize(value!, objectWriter, context, CancellationToken.None);
        }

        stream.Position = 0;
        return Create(stream);
    }

    public static Checksum Create(ParseOptions value, ISerializerService serializer)
    {
        using var stream = SerializableBytes.CreateWritableStream();

        using (var objectWriter = new ObjectWriter(stream, leaveOpen: true))
        {
            serializer.SerializeParseOptions(value, objectWriter);
        }

        stream.Position = 0;
        return Create(stream);
    }
}
