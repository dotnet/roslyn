// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal partial class BloomFilter : IObjectWritable
    {
        private const string SerializationFormat = "1";

        public void WriteTo(ObjectWriter writer)
        {
            writer.WriteString(SerializationFormat);
            writer.WriteBoolean(isCaseSensitive);
            writer.WriteInt32(hashFunctionCount);
            bitArray.WriteTo(writer);
        }

        public static BloomFilter ReadFrom(ObjectReader reader)
        {
            var version = reader.ReadString();
            if (!string.Equals(version, SerializationFormat, StringComparison.Ordinal))
            {
                return null;
            }

            var isCaseSensitive = reader.ReadBoolean();
            int hashFunctionCount = reader.ReadInt32();
            var bitArray = BitArrayUtilities.ReadFrom(reader);
            if (bitArray == null)
            {
                return null;
            }

            return new BloomFilter(bitArray, hashFunctionCount, isCaseSensitive);
        }
    }
}