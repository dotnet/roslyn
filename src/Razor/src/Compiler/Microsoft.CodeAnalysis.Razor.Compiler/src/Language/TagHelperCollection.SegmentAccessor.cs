// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Razor.Language;

public abstract partial class TagHelperCollection
{
    /// <summary>
    ///  Provides read-only access to segments within a <see cref="TagHelperCollection"/>,
    ///  enabling enumeration and indexed retrieval of segments.
    /// </summary>
    /// <remarks>
    ///  Segments are represented as <see cref="ReadOnlyMemory{TagHelperDescriptor}"/>.
    /// </remarks>
    private readonly ref struct SegmentAccessor(TagHelperCollection collection)
    {
        public int Count => collection.SegmentCount;

        public ReadOnlyMemory<TagHelperDescriptor> this[int index]
            => collection.GetSegment(index);

        public SegmentEnumerator GetEnumerator()
            => new(collection);
    }

    private struct SegmentEnumerator(TagHelperCollection collection)
    {
        private int _index = -1;

        public readonly ReadOnlyMemory<TagHelperDescriptor> Current
            => collection.GetSegment(_index);

        public bool MoveNext()
        {
            var nextIndex = _index + 1;
            if (nextIndex < collection.SegmentCount)
            {
                _index = nextIndex;
                return true;
            }

            return false;
        }

        public void Reset()
        {
            _index = -1;
        }
    }
}
