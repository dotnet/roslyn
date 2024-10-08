// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Collections;

internal readonly partial struct SegmentedArray<T>
{
    /// <summary>
    /// Private helper class for use only by <see cref="SegmentedCollectionsMarshal"/>.
    /// </summary>
    internal static class PrivateMarshal
    {
        /// <inheritdoc cref="SegmentedCollectionsMarshal.AsSegments{T}(SegmentedArray{T})"/>
        public static T[][] AsSegments(SegmentedArray<T> array)
            => array._items;
    }
}
