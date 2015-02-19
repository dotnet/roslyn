// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Collections
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
        public static readonly object BoxedUInt32Zero = 0U;
        public static readonly object BoxedInt64Zero = 0L;
        public static readonly object BoxedUInt64Zero = 0UL;

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
            return i == 0 ? BoxedInt32Zero : i;
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
    }
}
