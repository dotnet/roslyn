// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.AspNetCore.Razor.Utilities;

#if DEBUG
using System.Diagnostics;
#endif

namespace Microsoft.AspNetCore.Razor.Language;

public abstract partial class TagHelperCollection
{
    /// <summary>
    ///  Provides a builder for efficiently constructing a collection of tag helper descriptor
    ///  segments, ensuring that each segment contains only unique descriptors based on their checksums.
    ///  De-duplication is performed by slicing the original segment into smaller segments
    ///  to avoid copying each unique descriptor into a new array.
    /// </summary>
    private ref struct SegmentBuilder
    {
        private MemoryBuilder<ReadOnlyMemory<TagHelperDescriptor>> _builder;
        private readonly HashSet<Checksum> _seenChecksums;

        public SegmentBuilder()
            : this(capacity: 8)
        {
        }

        public SegmentBuilder(int capacity)
        {
            if (capacity < 8)
            {
                capacity = 8;
            }

            _builder = new MemoryBuilder<ReadOnlyMemory<TagHelperDescriptor>>(capacity, clearArray: true);
            ChecksumSetPool.Default.GetPooledObject(out _seenChecksums);
        }

        public void Dispose()
        {
            _builder.Dispose();
            ChecksumSetPool.Default.Return(_seenChecksums);
        }

        /// <summary>
        /// Adds a segment of TagHelperDescriptor items, appending only unique items based on
        /// their checksum to the underlying collection.
        /// </summary>
        /// <param name="segment">
        ///  A read-only memory region containing the TagHelperDescriptor items to add. Only items
        ///  with unique checksums, not previously added, are appended.
        /// </param>
        /// <remarks>
        ///  If the segment contains duplicate items (by checksum), only the first occurrence is
        ///  added; subsequent duplicates are ignored. The method preserves the order of unique
        ///  items as they appear in the segment.
        /// </remarks>
        public void AddSegment(ReadOnlyMemory<TagHelperDescriptor> segment)
        {
            var span = segment.Span;
            var segmentStart = 0;

            for (var i = 0; i < span.Length; i++)
            {
                if (_seenChecksums.Add(span[i].Checksum))
                {
                    // Item is unique, continue building current segment
                    continue;
                }

                // Found duplicate - close current segment if it has items
                if (i > segmentStart)
                {
                    // Create a slice from the original segment, avoiding array allocation
                    var uniqueSegment = segment[segmentStart..i];
                    _builder.Append(uniqueSegment);
                }

                // Start new segment after this duplicate
                segmentStart = i + 1;
            }

            // Close final segment if it has items
            if (segmentStart < span.Length)
            {
                var finalSegment = segment[segmentStart..];
                _builder.Append(finalSegment);
            }
        }

        public readonly TagHelperCollection ToCollection()
        {
            var segments = _builder.AsMemory().Span;

#if DEBUG
            foreach (var segment in segments)
            {
                Debug.Assert(!segment.IsEmpty, "SegmentBuilder should not contain an empty segment.");
            }
#endif

            return segments switch
            {
                [] => Empty,
                [var singleSegment] => new SingleSegmentCollection(singleSegment),
                _ => new MultiSegmentCollection(ImmutableCollectionsMarshal.AsImmutableArray(segments.ToArray()))
            };
        }
    }
}
