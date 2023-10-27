// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Serialization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    // various factory methods. all these are just helper methods
    internal partial record class Checksum
    {
        // https://github.com/dotnet/runtime/blob/f2db6d6093c54e5eeb9db2d8dcbe15b2db92ad8c/src/libraries/System.Security.Cryptography.Algorithms/src/System/Security/Cryptography/SHA256.cs#L18-L19
        private const int SHA256HashSizeBytes = 256 / 8;

#if NET5_0_OR_GREATER
        private static readonly ObjectPool<IncrementalHash> s_incrementalHashPool =
            new(() => IncrementalHash.CreateHash(HashAlgorithmName.SHA256), size: 20);
#else
        private static readonly ObjectPool<SHA256> s_incrementalHashPool =
            new(SHA256.Create, size: 20);
#endif

#if !NET5_0_OR_GREATER
        // Dedicated pools for the byte[]s we use to create checksums from two or three existing checksums. Sized to
        // exactly the space needed to splat the existing checksum data into the array and then hash it.

        private static readonly ObjectPool<byte[]> s_twoChecksumByteArrayPool = new(() => new byte[HashSize * 2]);
        private static readonly ObjectPool<byte[]> s_threeChecksumByteArrayPool = new(() => new byte[HashSize * 3]);
#endif

        public static Checksum Create(IEnumerable<string> values)
        {
#if NET5_0_OR_GREATER
            using var pooledHash = s_incrementalHashPool.GetPooledObject();

            foreach (var value in values)
            {
                pooledHash.Object.AppendData(MemoryMarshal.AsBytes(value.AsSpan()));
                pooledHash.Object.AppendData(MemoryMarshal.AsBytes("\0".AsSpan()));
            }

            Span<byte> hash = stackalloc byte[SHA256HashSizeBytes];
            pooledHash.Object.GetHashAndReset(hash);
            return From(hash);
#else
            using var pooledHash = s_incrementalHashPool.GetPooledObject();
            using var pooledBuffer = SharedPools.ByteArray.GetPooledObject();
            var hash = pooledHash.Object;

            hash.Initialize();
            foreach (var value in values)
            {
                AppendData(hash, pooledBuffer.Object, value);
                AppendData(hash, pooledBuffer.Object, "\0");
            }

            hash.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            return From(hash.Hash);
#endif
        }

        public static Checksum Create(string value)
        {
#if NET5_0_OR_GREATER
            Span<byte> hash = stackalloc byte[SHA256HashSizeBytes];
            SHA256.HashData(MemoryMarshal.AsBytes(value.AsSpan()), hash);
            return From(hash);
#else
            using var pooledHash = s_incrementalHashPool.GetPooledObject();
            using var pooledBuffer = SharedPools.ByteArray.GetPooledObject();
            var hash = pooledHash.Object;
            hash.Initialize();

            AppendData(hash, pooledBuffer.Object, value);

            hash.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            return From(hash.Hash);
#endif
        }

        public static Checksum Create(Stream stream)
        {
#if NET7_0_OR_GREATER
            Span<byte> hash = stackalloc byte[SHA256HashSizeBytes];
            SHA256.HashData(stream, hash);
            return From(hash);
#elif NET5_0_OR_GREATER
            using var pooledHash = s_incrementalHashPool.GetPooledObject();
            Span<byte> buffer = stackalloc byte[SharedPools.ByteBufferSize];

            int bytesRead;
            do
            {
                bytesRead = stream.Read(buffer);
                if (bytesRead > 0)
                {
                    pooledHash.Object.AppendData(buffer[..bytesRead]);
                }
            }
            while (bytesRead > 0);

            Span<byte> hash = stackalloc byte[SHA256HashSizeBytes];
            pooledHash.Object.GetHashAndReset(hash);
            return From(hash);
#else
            using var pooledHash = s_incrementalHashPool.GetPooledObject();
            using var pooledBuffer = SharedPools.ByteArray.GetPooledObject();

            var hash = pooledHash.Object;
            hash.Initialize();

            var buffer = pooledBuffer.Object;
            var bufferLength = buffer.Length;
            int bytesRead;
            do
            {
                bytesRead = stream.Read(buffer, 0, bufferLength);
                if (bytesRead > 0)
                {
                    hash.TransformBlock(buffer, 0, bytesRead, null, 0);
                }
            }
            while (bytesRead > 0);

            hash.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            var bytes = hash.Hash;

            // if bytes array is bigger than certain size, checksum
            // will truncate it to predetermined size. for more detail,
            // see the Checksum type
            //
            // the truncation can happen since different hash algorithm or 
            // same algorithm on different platform can have different hash size
            // which might be bigger than the Checksum HashSize.
            //
            // hash algorithm used here should remain functionally correct even
            // after the truncation
            return From(bytes);
#endif
        }

        public static Checksum Create(IObjectWritable @object)
        {
            using var stream = SerializableBytes.CreateWritableStream();

            using (var objectWriter = new ObjectWriter(stream, leaveOpen: true))
            {
                @object.WriteTo(objectWriter);
            }

            stream.Position = 0;
            return Create(stream);
        }

        public static Checksum Create(Checksum checksum1, Checksum checksum2)
        {
#if NET
            return CreateUsingSpans(checksum1, checksum2);
#else
            return CreateUsingByteArrays(checksum1, checksum2);
#endif
        }

        public static Checksum Create(Checksum checksum1, Checksum checksum2, Checksum checksum3)
        {
#if NET
            return CreateUsingSpans(checksum1, checksum2, checksum3);
#else
            return CreateUsingByteArrays(checksum1, checksum2, checksum3);
#endif
        }

#if !NET5_0_OR_GREATER

        private static Checksum CreateUsingByteArrays(Checksum checksum1, Checksum checksum2)
        {
            using var bytes = s_twoChecksumByteArrayPool.GetPooledObject();

            var bytesSpan = bytes.Object.AsSpan();
            checksum1.WriteTo(bytesSpan);
            checksum2.WriteTo(bytesSpan.Slice(HashSize));

            using var hash = s_incrementalHashPool.GetPooledObject();
            hash.Object.Initialize();

            hash.Object.TransformBlock(bytes.Object, 0, bytes.Object.Length, null, 0);

            hash.Object.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            return From(hash.Object.Hash);
        }

        private static Checksum CreateUsingByteArrays(Checksum checksum1, Checksum checksum2, Checksum checksum3)
        {
            using var bytes = s_threeChecksumByteArrayPool.GetPooledObject();

            var bytesSpan = bytes.Object.AsSpan();
            checksum1.WriteTo(bytesSpan);
            checksum2.WriteTo(bytesSpan.Slice(HashSize));
            checksum3.WriteTo(bytesSpan.Slice(2 * HashSize));

            using var hash = s_incrementalHashPool.GetPooledObject();
            hash.Object.Initialize();

            hash.Object.TransformBlock(bytes.Object, 0, bytes.Object.Length, null, 0);

            hash.Object.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            return From(hash.Object.Hash);
        }

#else

        // Optimized helpers that do not need to allocate any arrays to combine hashes.

        private static Checksum CreateUsingSpans(Checksum checksum1, Checksum checksum2)
        {
            Span<HashData> checksums = stackalloc HashData[] { checksum1.Hash, checksum2.Hash };
            Span<byte> hashResultSpan = stackalloc byte[SHA256HashSizeBytes];

            SHA256.HashData(MemoryMarshal.AsBytes(checksums), hashResultSpan);

            return From(hashResultSpan);
        }

        private static Checksum CreateUsingSpans(Checksum checksum1, Checksum checksum2, Checksum checksum3)
        {
            Span<HashData> checksums = stackalloc HashData[] { checksum1.Hash, checksum2.Hash, checksum3.Hash };
            Span<byte> hashResultSpan = stackalloc byte[SHA256HashSizeBytes];

            SHA256.HashData(MemoryMarshal.AsBytes(checksums), hashResultSpan);

            return From(hashResultSpan);
        }

#endif

        public static Checksum Create(IEnumerable<Checksum> checksums)
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

#if !NET5_0_OR_GREATER
        private static void AppendData(SHA256 hash, byte[] buffer, string value)
        {
            var stringBytes = MemoryMarshal.AsBytes(value.AsSpan());
            Debug.Assert(stringBytes.Length == value.Length * 2);

            var index = 0;
            while (index < stringBytes.Length)
            {
                var remaining = stringBytes.Length - index;
                var toCopy = Math.Min(remaining, buffer.Length);

                stringBytes.Slice(index, toCopy).CopyTo(buffer);
                hash.TransformBlock(buffer, 0, toCopy, null, 0);

                index += toCopy;
            }
        }
#endif
    }
}
