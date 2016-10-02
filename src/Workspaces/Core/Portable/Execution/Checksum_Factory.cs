// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using Roslyn.Utilities;
using System.Security.Cryptography;

namespace Microsoft.CodeAnalysis.Execution
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

        public static Checksum Create(string kind, Checksum checksum)
        {
            using (var stream = SerializableBytes.CreateWritableStream())
            using (var writer = new ObjectWriter(stream))
            {
                writer.WriteString(kind);
                checksum.WriteTo(writer);

                return Create(stream);
            }
        }

        public static Checksum Create<TChecksums>(string kind, TChecksums checksums)
            where TChecksums : IEnumerable<Checksum>
        {
            using (var stream = SerializableBytes.CreateWritableStream())
            using (var writer = new ObjectWriter(stream))
            {
                writer.WriteString(kind);

                foreach (var checksum in checksums)
                {
                    checksum.WriteTo(writer);
                }

                return Create(stream);
            }
        }

        public static Checksum Create<T>(string kind, ImmutableArray<T> objects) where T : ChecksumObject
        {
            using (var pool = Creator.CreateList<Checksum>())
            {
                for (var i = 0; i < objects.Length; i++)
                {
                    pool.Object.Add(objects[i].Checksum);
                }

                return Create(kind, pool.Object);
            }
        }

        public static Checksum Create<T>(T value, string kind, Action<T, ObjectWriter, CancellationToken> writer)
        {
            using (var stream = SerializableBytes.CreateWritableStream())
            using (var objectWriter = new ObjectWriter(stream))
            {
                objectWriter.WriteString(kind);
                writer(value, objectWriter, CancellationToken.None);
                return Create(stream);
            }
        }

        public static Checksum Create<T1, T2>(T1 value1, T2 value2, string kind, Action<T1, T2, ObjectWriter, CancellationToken> writer)
        {
            using (var stream = SerializableBytes.CreateWritableStream())
            using (var objectWriter = new ObjectWriter(stream))
            {
                objectWriter.WriteString(kind);
                writer(value1, value2, objectWriter, CancellationToken.None);
                return Create(stream);
            }
        }
    }
}
