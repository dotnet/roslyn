// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal static class IntegerUtilities
    {
        public static int CountOfBitsSet(long v)
        {
            // http://graphics.stanford.edu/~seander/bithacks.htm
            int c = 0;
            while (v != 0)
            {
                // clear the least significant bit set
                v &= unchecked(v - 1);
                c++;
            }

            return c;
        }

        public static bool HasOneBitSet(IComparable value)
        {
            return value.TypeSwitch(
                (long v) => HasOneBitSet((long)v),
                (ulong v) => HasOneBitSet(unchecked((long)v)),
                (int v) => HasOneBitSet((long)v),
                (uint v) => HasOneBitSet((long)v),
                (short v) => HasOneBitSet((long)v),
                (ushort v) => HasOneBitSet((long)v),
                (sbyte v) => HasOneBitSet((long)v),
                (byte v) => HasOneBitSet((long)v));
        }

        public static bool HasOneBitSet(long v)
        {
            return CountOfBitsSet(v) == 1;
        }

        public static int LogBase2(long v)
        {
            var log = 0;

            while ((v >>= 1) != 0)
            {
                log++;
            }

            return log;
        }

        /// <summary>
        /// Helper as VB's CType doesn't work without arithmetic overflow.
        /// </summary>
        public static long Convert(long v, SpecialType type)
        {
            switch (type)
            {
                case SpecialType.System_SByte:
                    return unchecked((sbyte)v);
                case SpecialType.System_Byte:
                    return unchecked((byte)v);
                case SpecialType.System_Int16:
                    return unchecked((short)v);
                case SpecialType.System_UInt16:
                    return unchecked((ushort)v);
                case SpecialType.System_Int32:
                    return unchecked((int)v);
                case SpecialType.System_UInt32:
                    return unchecked((uint)v);
                default:
                    return v;
            }
        }

        public static ulong ToUnsigned(long v)
        {
            return unchecked((ulong)v);
        }

        public static ulong ToUInt64(object o)
        {
            return o is ulong ? (ulong)o : unchecked((ulong)System.Convert.ToInt64(o));
        }

        public static long ToInt64(object o)
        {
            return o is ulong ? unchecked((long)(ulong)o) : System.Convert.ToInt64(o);
        }
    }
}
