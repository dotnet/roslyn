// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using Microsoft.CodeAnalysis.Serialization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    // various factory methods.
    // all these are just helper methods
    internal partial class Checksum
    {
        public static Checksum Create(Stream stream)
        {
            // REVIEW: should we cache SHA1CryptoServiceProvider
            using (var algorithm = SHA1.Create())
            {
                stream.Seek(0, SeekOrigin.Begin);
                return new Checksum(algorithm.ComputeHash(stream));
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

        public static Checksum Create<T>(WellKnownSynchronizationKind kind, T value, Serializer serializer)
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