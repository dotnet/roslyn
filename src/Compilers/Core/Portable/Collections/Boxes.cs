// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;

namespace Microsoft.CodeAnalysis
{
    internal static class Boxes
    {
        public static readonly object BoxedTrue = true;
        public static readonly object BoxedFalse = false;
        public static readonly object BoxedByteZero = (byte)0;
        public static readonly object BoxedSByteZero = (sbyte)0;
        public static readonly object BoxedInt16Zero = (short)0;
        public static readonly object BoxedUInt16Zero = (ushort)0;
        public static readonly object BoxedInt32Zero = 0;
        public static readonly object BoxedInt32One = 1;
        public static readonly object BoxedUInt32Zero = 0U;
        public static readonly object BoxedInt64Zero = 0L;
        public static readonly object BoxedUInt64Zero = 0UL;
        public static readonly object BoxedSingleZero = 0.0f;
        public static readonly object BoxedDoubleZero = 0.0;
        public static readonly object BoxedDecimalZero = 0m;

        private static readonly object?[] s_boxedAsciiChars = new object?[128];

        public static object Box(bool b)
        {
            return b ? BoxedTrue : BoxedFalse;
        }

        public static object Box(byte b)
        {
            return b == 0 ? BoxedByteZero : b;
        }

        public static object Box(sbyte sb)
        {
            return sb == 0 ? BoxedSByteZero : sb;
        }

        public static object Box(short s)
        {
            return s == 0 ? BoxedInt16Zero : s;
        }

        public static object Box(ushort us)
        {
            return us == 0 ? BoxedUInt16Zero : us;
        }

        public static object Box(int i)
        {
            switch (i)
            {
                case 0: return BoxedInt32Zero;
                case 1: return BoxedInt32One;
                default: return i;
            }
        }

        public static object Box(uint u)
        {
            return u == 0 ? BoxedUInt32Zero : u;
        }

        public static object Box(long l)
        {
            return l == 0 ? BoxedInt64Zero : l;
        }

        public static object Box(ulong ul)
        {
            return ul == 0 ? BoxedUInt64Zero : ul;
        }

        public static unsafe object Box(float f)
        {
            // There are many representations of zero in floating point.
            // Use the boxed value only if the bit pattern is all zeros.
            return *(int*)(&f) == 0 ? BoxedSingleZero : f;
        }

        public static object Box(double d)
        {
            // There are many representations of zero in floating point.
            // Use the boxed value only if the bit pattern is all zeros.
            return BitConverter.DoubleToInt64Bits(d) == 0 ? BoxedDoubleZero : d;
        }

        public static object Box(char c)
        {
            return c < 128
                ? s_boxedAsciiChars[c] ?? (s_boxedAsciiChars[c] = c)
                : c;
        }

        public static unsafe object Box(decimal d)
        {
            // There are many representation of zero in System.Decimal
            // Use the boxed value only if the bit pattern is all zeros.
            ulong* ptr = (ulong*)&d;
            return ptr[0] == 0 && ptr[1] == 0 ? BoxedDecimalZero : d;
        }
    }
}
