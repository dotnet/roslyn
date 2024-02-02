// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Hashing;
using System.IO.Pipelines;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Serialization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
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

        public static async ValueTask<Checksum> CreateAsync<T>(T @object, Action<T, ObjectWriter> writeObject, CancellationToken cancellationToken)
        {
            using var stream = SerializableBytes.CreateWritableStream();

            var objectWriter = new ObjectWriter(PipeWriter.Create(stream), leaveOpen: true, cancellationToken);
            await using (var _1 = objectWriter.ConfigureAwait(false))
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

        public static async ValueTask<Checksum> CreateAsync(ArrayBuilder<Checksum> checksums, CancellationToken cancellationToken)
        {
            using var stream = SerializableBytes.CreateWritableStream();

            var objectWriter = new ObjectWriter(PipeWriter.Create(stream), leaveOpen: true, cancellationToken);
            await using (var _1 = objectWriter.ConfigureAwait(false))
            {
                foreach (var checksum in checksums)
                    checksum.WriteTo(objectWriter);
            }

            stream.Position = 0;
            return Create(stream);
        }

        public static async ValueTask<Checksum> CreateAsync(ImmutableArray<Checksum> checksums, CancellationToken cancellationToken)
        {
            using var stream = SerializableBytes.CreateWritableStream();

            var objectWriter = new ObjectWriter(PipeWriter.Create(stream), leaveOpen: true, cancellationToken);
            await using (var _1 = objectWriter.ConfigureAwait(false))
            {
                foreach (var checksum in checksums)
                    checksum.WriteTo(objectWriter);
            }

            stream.Position = 0;
            return Create(stream);
        }

        public static async ValueTask<Checksum> CreateAsync(ImmutableArray<byte> bytes, CancellationToken cancellationToken)
        {
            using var stream = SerializableBytes.CreateWritableStream();

            var objectWriter = new ObjectWriter(PipeWriter.Create(stream), leaveOpen: true, cancellationToken);
            await using (var _1 = objectWriter.ConfigureAwait(false))
            {
                for (var i = 0; i < bytes.Length; i++)
                    objectWriter.WriteByte(bytes[i]);
            }

            stream.Position = 0;
            return Create(stream);
        }

        public static async ValueTask<Checksum> CreateAsync<T>(T value, ISerializerService serializer, CancellationToken cancellationToken)
        {
            using var stream = SerializableBytes.CreateWritableStream();
            using var context = new SolutionReplicationContext();

            var objectWriter = new ObjectWriter(PipeWriter.Create(stream), leaveOpen: true, cancellationToken);
            await using (var _1 = objectWriter.ConfigureAwait(false))
            {
                await serializer.SerializeAsync(value!, objectWriter, context, cancellationToken).ConfigureAwait(false);
            }

            stream.Position = 0;
            return Create(stream);
        }

        public static async ValueTask<Checksum> CreateAsync(ParseOptions value, ISerializerService serializer, CancellationToken cancellationToken)
        {
            using var stream = SerializableBytes.CreateWritableStream();

            var objectWriter = new ObjectWriter(PipeWriter.Create(stream), leaveOpen: true, cancellationToken);
            await using (var _1 = objectWriter.ConfigureAwait(false))
            {
                serializer.SerializeParseOptions(value, objectWriter);
            }

            stream.Position = 0;
            return Create(stream);
        }
    }
}
