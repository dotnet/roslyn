// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Security.Cryptography;
using System.Text;
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
            new ObjectPool<IncrementalHash>(() => IncrementalHash.CreateHash(HashAlgorithmName.SHA256), size: 20);

        public static Checksum Create(string val)
        {
            using (var pooledHash = s_incrementalHashPool.GetPooledObject())
            using (var pooledBuffer = SharedPools.ByteArray.GetPooledObject())
            {
                var hash = pooledHash.Object;

                var stringBytes = MemoryMarshal.AsBytes(val.AsSpan());
                Debug.Assert(stringBytes.Length == val.Length * 2);
                var buffer = pooledBuffer.Object;

                var index = 0;
                while (index < stringBytes.Length)
                {
                    var remaining = stringBytes.Length - index;
                    var toCopy = Math.Min(remaining, buffer.Length);

                    stringBytes.Slice(index, toCopy).CopyTo(buffer);
                    hash.AppendData(buffer, 0, toCopy);

                    index += toCopy;
                }

                return From(hash.GetHashAndReset());
            }
        }

        public static Checksum Create(Stream stream)
        {
            using (var pooledHash = s_incrementalHashPool.GetPooledObject())
            using (var pooledBuffer = SharedPools.ByteArray.GetPooledObject())
            {
                stream.Seek(0, SeekOrigin.Begin);

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
                return Checksum.From(bytes);
            }
        }

        public static Checksum Create(WellKnownSynchronizationKind kind, IObjectWritable @object)
        {
            using (var stream = SerializableBytes.CreateWritableStream())
            using (var objectWriter = new ObjectWriter(stream))
            {
                objectWriter.WriteInt32((int)kind);
                @object.WriteTo(objectWriter);

                return Create(stream);
            }
        }

        public static Checksum Create(WellKnownSynchronizationKind kind, IEnumerable<Checksum> checksums)
        {
            using (var stream = SerializableBytes.CreateWritableStream())
            using (var writer = new ObjectWriter(stream))
            {
                writer.WriteInt32((int)kind);

                foreach (var checksum in checksums)
                {
                    checksum.WriteTo(writer);
                }

                return Create(stream);
            }
        }

        public static Checksum Create(WellKnownSynchronizationKind kind, ImmutableArray<byte> bytes)
        {
            using (var stream = SerializableBytes.CreateWritableStream())
            using (var writer = new ObjectWriter(stream))
            {
                writer.WriteInt32((int)kind);

                for (var i = 0; i < bytes.Length; i++)
                {
                    writer.WriteByte(bytes[i]);
                }

                return Create(stream);
            }
        }

        public static Checksum Create<T>(WellKnownSynchronizationKind kind, T value, ISerializerService serializer)
        {
            using (var stream = SerializableBytes.CreateWritableStream())
            using (var objectWriter = new ObjectWriter(stream))
            {
                objectWriter.WriteInt32((int)kind);
                serializer.Serialize(value, objectWriter, CancellationToken.None);
                return Create(stream);
            }
        }
    }
}
