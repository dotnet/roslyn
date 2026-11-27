// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language;

public abstract partial class TagHelperCollection
{
    /// <summary>
    ///  Represents a read-only collection of <see cref="TagHelperDescriptor"/> objects composed
    ///  from multiple contiguous memory segments, providing efficient indexed access across all segments.
    /// </summary>
    /// <remarks>
    ///  This collection is optimized for scenarios where <see cref="TagHelperDescriptor"/> items
    ///  are distributed across several memory segments, allowing for efficient access without merging
    ///  the segments into a single array. The collection is immutable and thread-safe for concurrent
    ///  read operations.
    /// </remarks>
    private sealed class MultiSegmentCollection : SegmentCollectionBase
    {
        private readonly ImmutableArray<ReadOnlyMemory<TagHelperDescriptor>> _segments;
        private readonly int[] _segmentStartIndices;
        private readonly int _count;

        public MultiSegmentCollection(ImmutableArray<ReadOnlyMemory<TagHelperDescriptor>> segments)
        {
            Debug.Assert(segments.Length > 0, "Segments cannot be empty.");

            _segments = segments;

            // Pre-calculate segment boundaries for efficient indexing
            _segmentStartIndices = new int[segments.Length];
            var count = 0;

            for (var i = 0; i < segments.Length; i++)
            {
                Debug.Assert(segments[i].Length > 0, "Segments cannot be empty.");

                _segmentStartIndices[i] = count;
                count += segments[i].Length;
            }

            _count = count;
        }

        protected override int SegmentCount => _segments.Length;

        protected override ReadOnlyMemory<TagHelperDescriptor> GetSegment(int index)
        {
            Debug.Assert(index >= 0 && index < _segments.Length);

            return _segments[index];
        }

        public override int Count => _count;

        public override TagHelperDescriptor this[int index]
        {
            get
            {
                ArgHelper.ThrowIfNegative(index);
                ArgHelper.ThrowIfGreaterThanOrEqual(index, Count);

                // Binary search to find the segment containing this index
                var segmentIndex = FindSegmentIndex(index);
                var localIndex = index - _segmentStartIndices[segmentIndex];

                return _segments[segmentIndex].Span[localIndex];
            }
        }

        private int FindSegmentIndex(int index)
        {
            var searchResult = _segmentStartIndices.BinarySearch(index);

            if (searchResult >= 0)
            {
                return searchResult;
            }

            var insertionPoint = ~searchResult;
            return insertionPoint - 1;
        }
    }
}
