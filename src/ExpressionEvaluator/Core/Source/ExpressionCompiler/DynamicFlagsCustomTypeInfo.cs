// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal static class DynamicFlagsCustomTypeInfo
    {
        internal static ReadOnlyCollection<byte> ToBytes(ArrayBuilder<bool> dynamicFlags, int startIndex = 0)
        {
            Debug.Assert(dynamicFlags != null);
            Debug.Assert(startIndex >= 0);

            int numFlags = dynamicFlags.Count - startIndex;
            if (numFlags == 0)
            {
                return null;
            }

            int numBytes = (numFlags + 7) / 8;
            byte[] bytes = new byte[numBytes];
            bool seenTrue = false;
            for (int b = 0; b < numBytes; b++)
            {
                for (int i = 0; i < 8; i++)
                {
                    var f = b * 8 + i;
                    if (f >= numFlags)
                    {
                        Debug.Assert(f == numFlags);
                        goto ALL_FLAGS_READ;
                    }

                    if (dynamicFlags[startIndex + f])
                    {
                        seenTrue = true;
                        bytes[b] |= (byte)(1 << i);
                    }
                }
            }

ALL_FLAGS_READ:

            return seenTrue ? new ReadOnlyCollection<byte>(bytes) : null;
        }

        internal static bool GetFlag(ReadOnlyCollection<byte> bytes, int index)
        {
            Debug.Assert(index >= 0);
            if (bytes == null)
            {
                return false;
            }
            var b = index / 8;
            return b < bytes.Count &&
                (bytes[b] & (1 << (index % 8))) != 0;
        }

        /// <remarks>
        /// Not guaranteed to add the same number of flags as would
        /// appear in a System.Runtime.CompilerServices.DynamicAttribute.
        /// It may have more (for padding) or fewer (for compactness) falses.
        /// It is, however, guaranteed to include the last true.
        /// </remarks>
        internal static void CopyTo(ReadOnlyCollection<byte> bytes, ArrayBuilder<bool> builder)
        {
            if (bytes == null)
            {
                return;
            }

            foreach (byte b in bytes)
            {
                for (int i = 0; i < 8; i++)
                {
                    builder.Add((b & (1 << i)) != 0);
                }
            }
        }

        internal static ReadOnlyCollection<byte> SkipOne(ReadOnlyCollection<byte> bytes)
        {
            if (bytes == null)
            {
                return bytes;
            }

            var builder = ArrayBuilder<bool>.GetInstance();
            CopyTo(bytes, builder);
            var result = ToBytes(builder, startIndex: 1);
            builder.Free();
            return result;
        }
    }
}
