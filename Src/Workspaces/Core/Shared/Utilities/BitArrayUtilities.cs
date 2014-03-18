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
            // TODO : think about a way to use pool for byte array.
            // BitArray will internally allocate another int array. probably need to drop BitArray usage.
            var bytes = new byte[(bitArray.Length + 7) / 8];
            bitArray.CopyTo(bytes, 0);

            writer.WriteString(SerializationFormat);
            writer.WriteInt32(bytes.Length);

            for (var i = 0; i < bytes.Length; i++)
            {
                writer.WriteByte(bytes[i]);
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