// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Copied from https://github.com/dotnet/runtime/blob/f1d463f46268382a8287d71aea929dadaa5dfef5/src/libraries/System.IO.Hashing/src/System/IO/Hashing/BitOperations.cs#L1C1-L18C2
// Remove once we can actually add a reference to System.IO.Hashing v8.0.0

using System.Runtime.CompilerServices;

namespace System.Numerics
{
    internal static class RuntimeBitOperations
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint RotateLeft(uint value, int offset)
            => (value << offset) | (value >> (32 - offset));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong RotateLeft(ulong value, int offset)
            => (value << offset) | (value >> (64 - offset));
    }
}
