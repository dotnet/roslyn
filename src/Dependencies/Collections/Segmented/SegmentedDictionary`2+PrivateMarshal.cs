// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Microsoft.CodeAnalysis.Collections;

internal sealed partial class SegmentedDictionary<TKey, TValue>
{
    /// <summary>
    /// Private helper class for use only by <see cref="SegmentedCollectionsMarshal"/>.
    /// </summary>
    internal static class PrivateMarshal
    {
        /// <inheritdoc cref="SegmentedCollectionsMarshal.GetValueRefOrNullRef{TKey, TValue}(SegmentedDictionary{TKey, TValue}, TKey)"/>
        public static ref TValue FindValue(SegmentedDictionary<TKey, TValue> dictionary, TKey key)
            => ref dictionary.FindValue(key);
    }
}
