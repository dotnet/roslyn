// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Collections;

internal readonly partial struct ImmutableSegmentedDictionary<TKey, TValue>
{
    public partial class Builder
    {
        /// <summary>
        /// Private helper class for use only by <see cref="SegmentedCollectionsMarshal"/>.
        /// </summary>
        internal static class PrivateMarshal
        {
            /// <inheritdoc cref="SegmentedCollectionsMarshal.GetValueRefOrNullRef{TKey, TValue}(ImmutableSegmentedDictionary{TKey, TValue}.Builder, TKey)"/>
            public static ref TValue FindValue(Builder dictionary, TKey key)
                => ref SegmentedCollectionsMarshal.GetValueRefOrNullRef(dictionary._builder.GetOrCreateMutableDictionary(), key);
        }
    }
}
