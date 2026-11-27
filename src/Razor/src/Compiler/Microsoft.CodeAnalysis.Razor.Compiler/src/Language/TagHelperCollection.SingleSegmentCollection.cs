// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Razor.Language;

public abstract partial class TagHelperCollection
{
    /// <summary>
    ///  Represents a collection of <see cref="TagHelperDescriptor"/> objects that contains
    ///  a single contiguous segment.
    /// </summary>
    private sealed class SingleSegmentCollection : SegmentCollectionBase
    {
        private readonly ReadOnlyMemory<TagHelperDescriptor> _segment;

        public SingleSegmentCollection(TagHelperDescriptor item)
        {
            _segment = new[] { item };
        }

        public SingleSegmentCollection(ReadOnlyMemory<TagHelperDescriptor> segment)
        {
            Debug.Assert(segment.Length > 0, "Segments cannot be empty.");

            _segment = segment;
        }

        protected override int SegmentCount => 1;

        protected override ReadOnlyMemory<TagHelperDescriptor> GetSegment(int index)
        {
            Debug.Assert(index == 0);

            return _segment;
        }

        public override int Count => _segment.Length;

        public override TagHelperDescriptor this[int index]
        {
            get
            {
                ArgHelper.ThrowIfNegative(index);
                ArgHelper.ThrowIfGreaterThanOrEqual(index, Count);

                return _segment.Span[index];
            }
        }
    }
}
