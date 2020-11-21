// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Shared.Collections
{
    internal static class Ordinal
    {
        internal static int ToUpperOrdinal(ReadOnlySpan<char> source, Span<char> destination)
        {
            if (source.Overlaps(destination))
                throw new InvalidOperationException(SR.InvalidOperation_SpanOverlappedOperation);

            // Assuming that changing case does not affect length
            if (destination.Length < source.Length)
                return -1;

            OrdinalCasing.ToUpperInvariantMode(source, destination);
            return source.Length;
        }
    }
}
