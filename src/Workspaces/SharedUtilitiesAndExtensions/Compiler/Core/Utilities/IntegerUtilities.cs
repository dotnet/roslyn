// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal static class IntegerUtilities
    {
        public static int CountOfBitsSet(long v)
        {
            // http://graphics.stanford.edu/~seander/bithacks.htm
            var c = 0;
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
            switch (value)
            {
                case long v: return HasOneBitSet(v);
                case ulong v: return HasOneBitSet(unchecked((long)v));
                case int v: return HasOneBitSet(v);
                case uint v: return HasOneBitSet(v);
                case short v: return HasOneBitSet(v);
                case ushort v: return HasOneBitSet(v);
                case sbyte v: return HasOneBitSet(v);
                case byte v: return HasOneBitSet(v);
                default: return false;
            }
        }

        public static bool HasOneBitSet(long v)
            => CountOfBitsSet(v) == 1;

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
            => type switch
            {
                SpecialType.System_SByte => unchecked((sbyte)v),
                SpecialType.System_Byte => unchecked((byte)v),
                SpecialType.System_Int16 => unchecked((short)v),
                SpecialType.System_UInt16 => unchecked((ushort)v),
                SpecialType.System_Int32 => unchecked((int)v),
                SpecialType.System_UInt32 => unchecked((uint)v),
                _ => v,
            };

        public static ulong ToUnsigned(long v)
            => unchecked((ulong)v);

        public static ulong ToUInt64(object? o)
            => o is ulong ? (ulong)o : unchecked((ulong)System.Convert.ToInt64(o));

        public static long ToInt64(object? o)
            => o is ulong ul ? unchecked((long)ul) : System.Convert.ToInt64(o);

        public static bool IsIntegral([NotNullWhen(true)] object? value)
            => value switch
            {
                sbyte _ => true,
                byte _ => true,
                short _ => true,
                ushort _ => true,
                int _ => true,
                uint _ => true,
                long _ => true,
                ulong _ => true,
                _ => false,
            };
    }
}
