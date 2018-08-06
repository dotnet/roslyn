// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace Roslyn.Utilities
{
    internal static class ImmutableByteArrayInterop
    {
        internal static byte[] DangerousGetUnderlyingArray(this ImmutableArray<byte> array)
        {
            var union = new ArrayUnion();
            union.ImmutableArray = array;
            return union.MutableArray;
        }

        internal static ImmutableArray<byte> DangerousCreateFromUnderlyingArray(ref byte[] array)
        {
            var union = new ArrayUnion();
            union.MutableArray = array;
            array = null;
            return union.ImmutableArray;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct ArrayUnion
        {
            [FieldOffset(0)]
            internal byte[] MutableArray;

            [FieldOffset(0)]
            internal ImmutableArray<byte> ImmutableArray;
        }
    }

    internal static class ImmutableInt32ArrayInterop
    {
        internal static int[] DangerousGetUnderlyingArray(this ImmutableArray<int> array)
        {
            var union = new ArrayUnion();
            union.ImmutableArray = array;
            return union.MutableArray;
        }

        internal static ImmutableArray<int> DangerousCreateFromUnderlyingArray(ref int[] array)
        {
            var union = new ArrayUnion();
            union.MutableArray = array;
            array = null;
            return union.ImmutableArray;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct ArrayUnion
        {
            [FieldOffset(0)]
            internal int[] MutableArray;

            [FieldOffset(0)]
            internal ImmutableArray<int> ImmutableArray;
        }
    }
}
