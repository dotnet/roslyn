// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal static class BitArrayUtilities
    {
        private const string SerializationFormat = "0";

        public static void WriteTo(this BitArray bitArray, ObjectWriter writer)
        {
            // Our serialization format doesn't round-trip bit arrays of non-byte lengths
            Contract.ThrowIfTrue(bitArray.Length % 8 != 0);

            writer.WriteString(SerializationFormat);
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

        public static BitArray ReadFrom(this ObjectReader reader)
        {
            var version = reader.ReadString();
            if (!string.Equals(version, SerializationFormat, StringComparison.Ordinal))
            {
                return null;
            }

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