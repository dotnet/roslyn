// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// NOTE: This code is derived from an implementation originally in dotnet/runtime:
// https://github.com/dotnet/runtime/blob/v5.0.7/src/libraries/Common/src/System/Collections/Generic/BitHelper.cs
//
// See the commentary in https://github.com/dotnet/roslyn/pull/50156 for notes on incorporating changes made to the
// reference implementation.

using System;

namespace Microsoft.CodeAnalysis.Collections.Internal
{
    internal ref struct BitHelper
    {
        private const int IntSize = sizeof(int) * 8;
        private readonly Span<int> _span;

        internal BitHelper(Span<int> span, bool clear)
        {
            if (clear)
            {
                span.Clear();
            }
            _span = span;
        }

        internal readonly void MarkBit(int bitPosition)
        {
            var bitArrayIndex = bitPosition / IntSize;
            if ((uint)bitArrayIndex < (uint)_span.Length)
            {
                _span[bitArrayIndex] |= (1 << (bitPosition % IntSize));
            }
        }

        internal readonly bool IsMarked(int bitPosition)
        {
            var bitArrayIndex = bitPosition / IntSize;
            return
                (uint)bitArrayIndex < (uint)_span.Length &&
                (_span[bitArrayIndex] & (1 << (bitPosition % IntSize))) != 0;
        }

        /// <summary>How many ints must be allocated to represent n bits. Returns (n+31)/32, but avoids overflow.</summary>
        internal static int ToIntArrayLength(int n) => n > 0 ? ((n - 1) / IntSize + 1) : 0;
    }
}
