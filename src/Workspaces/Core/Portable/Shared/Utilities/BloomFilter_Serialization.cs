// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal partial class BloomFilter
    {
        private const string SerializationFormat = "2";

        public async ValueTask WriteToAsync(ObjectWriter writer)
        {
            await writer.WriteStringAsync(SerializationFormat).ConfigureAwait(false);
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

        public static async ValueTask<BloomFilter> ReadFrom(ObjectReader reader)
        {
            var version = await reader.ReadStringAsync().ConfigureAwait(false);
            if (!string.Equals(version, SerializationFormat, StringComparison.Ordinal))
            {
                return null;
            }

            var isCaseSensitive = await reader.ReadBooleanAsync().ConfigureAwait(false);
            var hashFunctionCount = await reader.ReadInt32Async().ConfigureAwait(false);
            var bitArray = ReadBitArray(reader);
            return new BloomFilter(bitArray, hashFunctionCount, isCaseSensitive);
        }

        private static async ValueTask<BitArray> ReadBitArrayAsync(ObjectReader reader)
        {
            // TODO: find a way to use pool
            var length = await reader.ReadInt32Async().ConfigureAwait(false);
            var bytes = new byte[length];

            for (var i = 0; i < bytes.Length; i++)
                bytes[i] = await reader.ReadByteAsync().ConfigureAwait(false);

            return new BitArray(bytes);
        }
    }
}
