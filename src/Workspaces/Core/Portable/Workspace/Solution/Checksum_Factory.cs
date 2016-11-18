// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Roslyn.Utilities;
using System.Security.Cryptography;
using Microsoft.CodeAnalysis.Serialization;

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

        public static Checksum Create<T>(T value, string kind, Serializer serializer)
        {
            using (var stream = SerializableBytes.CreateWritableStream())
            using (var objectWriter = new ObjectWriter(stream))
            {
                objectWriter.WriteString(kind);
                serializer.Serialize(value, objectWriter, CancellationToken.None);
                return Create(stream);
            }
        }

        public static Checksum Create(IObjectWritable @object, string kind)
        {
            using (var stream = SerializableBytes.CreateWritableStream())
            using (var objectWriter = new ObjectWriter(stream))
            {
                objectWriter.WriteString(kind);
                @object.WriteTo(objectWriter);

                return Create(stream);
            }
        }
    }
}
