// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal partial class BloomFilter : IObjectWritable
    {
        private const string SerializationFormat = "2";

        bool IObjectWritable.ShouldReuseInSerialization => true;

        public void WriteTo(ObjectWriter writer)
        {
            writer.WriteString(SerializationFormat);
            writer.WriteBoolean(_isCaseSensitive);
            writer.WriteInt32(_hashFunctionCount);
            WriteBitArray(writer, _bitArray);
        }

        private static void WriteBitArray(ObjectWriter writer, BitArray bitArray)
        {
            // Our serialization format doesn't round-trip bit arrays of non-byte lengths
            Contract.ThrowIfTrue(bitArray.Length % 8 != 0);

            writer.WriteInt32(bitArray.Length / 8);

            // This will hold the byte that we will write out after we process every 8 bits. This is
            // LSB, so we push bits into it from the MSB.
            byte b = 0;

            for (var i = 0; i < bitArray.Length; i++)
            {
                if (bitArray[i])
                {
                    b = (byte)(0x80 | b >> 1);
                }
                else
                {
                    b >>= 1;
                }

                if ((i + 1) % 8 == 0)
                {
                    // End of a byte, write out the byte
                    writer.WriteByte(b);
                }
            }
        }

        public static BloomFilter ReadFrom(ObjectReader reader)
        {
            var version = reader.ReadString();
            if (!string.Equals(version, SerializationFormat, StringComparison.Ordinal))
            {
                return null;
            }

            var isCaseSensitive = reader.ReadBoolean();
            var hashFunctionCount = reader.ReadInt32();
            var bitArray = ReadBitArray(reader);
            return new BloomFilter(bitArray, hashFunctionCount, isCaseSensitive);
        }

        private static BitArray ReadBitArray(ObjectReader reader)
        {
            // TODO: find a way to use pool
            var length = reader.ReadInt32();
            var bytes = new byte[length];

            for (var i = 0; i < bytes.Length; i++)
            {
                bytes[i] = reader.ReadByte();
            }

            return new BitArray(bytes);
        }
    }
}
