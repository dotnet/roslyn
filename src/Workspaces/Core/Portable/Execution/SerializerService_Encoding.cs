// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Serialization
{
    internal partial class SerializerService
    {
        // cache for encoding. 
        // typical low number, high volumn data cache.
        private static readonly ConcurrentDictionary<Encoding, byte[]> s_encodingCache = new ConcurrentDictionary<Encoding, byte[]>(concurrencyLevel: 2, capacity: 5);

        private const byte NoEncodingSerialization = 0;
        private const byte EncodingSerialization = 1;

        public static void WriteTo(Encoding? encoding, ObjectWriter writer, CancellationToken cancellationToken)
        {
            if (encoding == null)
            {
                WriteNoEncodingTo(encoding, writer, cancellationToken);
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var value = GetEncodingBytes(encoding);
            if (value == null)
            {
                // we couldn't serialize encoding, act like there is no encoding.
                WriteNoEncodingTo(encoding, writer, cancellationToken);
                return;
            }

            // write data out
            writer.WriteByte(EncodingSerialization);
            writer.WriteValue(value.AsSpan());
        }

        private static void WriteNoEncodingTo(Encoding? encoding, ObjectWriter writer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            writer.WriteByte(NoEncodingSerialization);
            writer.WriteString(encoding?.WebName);
        }

        private static byte[]? GetEncodingBytes(Encoding encoding)
        {
            try
            {
                if (!s_encodingCache.TryGetValue(encoding, out var value))
                {
                    // we don't have cache, cache it
                    var formatter = new BinaryFormatter();
                    using var stream = SerializableBytes.CreateWritableStream();

                    // unfortunately, this is only way to properly clone encoding
                    formatter.Serialize(stream, encoding);
                    value = stream.ToArray();

                    // add if not already exist. otherwise, noop
                    s_encodingCache.TryAdd(encoding, value);
                }

                return value;
            }
            catch (SerializationException)
            {
                // even though Encoding is supposed to be serializable, 
                // not every Encoding follows the rule strictly.
                // in such as, behave like there was no encoding
                return null;
            }
        }

        public static Encoding? ReadEncodingFrom(ObjectReader reader, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var serialized = reader.ReadByte();
            if (serialized == EncodingSerialization)
            {
                var array = (byte[])reader.ReadValue();
                var formatter = new BinaryFormatter();

                return (Encoding)formatter.Deserialize(new MemoryStream(array));
            }

            return ReadEncodingFrom(serialized, reader, cancellationToken);
        }

        private static Encoding? ReadEncodingFrom(byte serialized, ObjectReader reader, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (serialized != NoEncodingSerialization)
            {
                return null;
            }

            var webName = reader.ReadString();
            return webName == null ? null : Encoding.GetEncoding(webName);
        }
    }
}
