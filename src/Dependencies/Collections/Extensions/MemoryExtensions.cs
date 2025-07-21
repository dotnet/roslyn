// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace System
{
    /// <remarks>
    /// Defines polyfill methods and overloads or alternative names of existing Span related methods defined in System.
    /// </remarks>
    internal static class RoslynMemoryExtensions
    {
        /// <summary>
        /// Variant of <see cref="System.MemoryExtensions.BinarySearch{T, TComparer}(ReadOnlySpan{T}, T, TComparer)"/>.
        /// </summary>
        public static int BinarySearch<TElement, TValue>(this ReadOnlySpan<TElement> span, TValue value, Func<TElement, TValue, int> comparer)
        {
            int low = 0;
            int high = span.Length - 1;

            while (low <= high)
            {
                int middle = low + ((high - low) >> 1);
                int comparison = comparer(span[middle], value);

                if (comparison == 0)
                {
                    return middle;
                }

                if (comparison > 0)
                {
                    high = middle - 1;
                }
                else
                {
                    low = middle + 1;
                }
            }

            return ~low;
        }
    }
}
