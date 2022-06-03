// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Serialization;
using Roslyn.Utilities;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    // various factory methods. all these are just helper methods
    internal partial class Checksum
    {
        private static readonly ObjectPool<IncrementalHash> s_incrementalHashPool =
            new(() => IncrementalHash.CreateHash(HashAlgorithmName.SHA256), size: 20);

        public static Checksum Create(IEnumerable<string> values)
        {
            using var pooledHash = s_incrementalHashPool.GetPooledObject();
            using var pooledBuffer = SharedPools.ByteArray.GetPooledObject();
            var hash = pooledHash.Object;

            foreach (var value in values)
            {
                AppendData(hash, pooledBuffer.Object, value);
                AppendData(hash, pooledBuffer.Object, "\0");
            }

            return From(hash.GetHashAndReset());
        }

        public static Checksum Create(string value)
        {
            using var pooledHash = s_incrementalHashPool.GetPooledObject();
            using var pooledBuffer = SharedPools.ByteArray.GetPooledObject();
            var hash = pooledHash.Object;

            AppendData(hash, pooledBuffer.Object, value);

            return From(hash.GetHashAndReset());
        }

        public static Checksum Create(Stream stream)
        {
            using var pooledHash = s_incrementalHashPool.GetPooledObject();
            using var pooledBuffer = SharedPools.ByteArray.GetPooledObject();

            var hash = pooledHash.Object;

            var buffer = pooledBuffer.Object;
            var bufferLength = buffer.Length;
            int bytesRead;
            do
            {
                bytesRead = stream.Read(buffer, 0, bufferLength);
                if (bytesRead > 0)
                {
                    hash.AppendData(buffer, 0, bytesRead);
                }
            }
            while (bytesRead > 0);

            var bytes = hash.GetHashAndReset();

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
            using var stream = SerializableBytes.CreateWritableStream();

            using (var writer = new ObjectWriter(stream, leaveOpen: true))
            {
                checksum1.WriteTo(writer);
                checksum2.WriteTo(writer);
            }

            stream.Position = 0;
            return Create(stream);
        }

        public static Checksum Create(Checksum checksum1, Checksum checksum2, Checksum checksum3)
        {
            using var stream = SerializableBytes.CreateWritableStream();

            using (var writer = new ObjectWriter(stream, leaveOpen: true))
            {
                checksum1.WriteTo(writer);
                checksum2.WriteTo(writer);
                checksum3.WriteTo(writer);
            }

            stream.Position = 0;
            return Create(stream);
        }

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

        private static void AppendData(IncrementalHash hash, byte[] buffer, string value)
        {
            var stringBytes = MemoryMarshal.AsBytes(value.AsSpan());
            Debug.Assert(stringBytes.Length == value.Length * 2);

            var index = 0;
            while (index < stringBytes.Length)
            {
                var remaining = stringBytes.Length - index;
                var toCopy = Math.Min(remaining, buffer.Length);

                stringBytes.Slice(index, toCopy).CopyTo(buffer);
                hash.AppendData(buffer, 0, toCopy);

                index += toCopy;
            }
        }
    }
}
