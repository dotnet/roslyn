// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace Roslyn.Test.Utilities
{
    internal static class ImmutableArrayInterop
    {
        internal static byte[] DangerousGetUnderlyingArray(this ImmutableArray<byte> array)
        {
            var union = new ByteArrayUnion();
            union.ImmutableArray = array;
            return union.MutableArray;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct ByteArrayUnion
        {
            [FieldOffset(0)]
            internal readonly byte[] MutableArray;

            [FieldOffset(0)]
            internal ImmutableArray<byte> ImmutableArray;
        }
    }
}
