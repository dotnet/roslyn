// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.Threading;
using Microsoft.AspNetCore.Razor.Utilities;

namespace Microsoft.AspNetCore.Razor.Language;

public abstract partial class TagHelperCollection
{
    /// <summary>
    ///  Represents an abstract collection of tag helper descriptors organized into segments.
    /// </summary>
    /// <remarks>
    ///  SegmentCollection provides efficient lookup and indexing for tag helper descriptors by
    ///  utilizing a segmented internal structure. This class is intended to be used as a base
    ///  for specialized collections that require optimized access patterns for large numbers of
    ///  tag helpers. Thread safety and mutability depend on the implementation of the derived class.
    /// </remarks>
    private abstract class SegmentCollectionBase : TagHelperCollection
    {
        private LazyValue<TagHelperCollection, Dictionary<Checksum, int>> _lazyLookupTable = new(collection =>
        {
            var lookupTable = new Dictionary<Checksum, int>(collection.Count);
            var index = 0;

            foreach (var segment in collection.Segments)
            {
                foreach (var item in segment.Span)
                {
                    lookupTable.Add(item.Checksum, index++);
                }
            }

            return lookupTable;
        });

        private LazyValue<TagHelperCollection, Checksum> _lazyChecksum = new(collection =>
        {
            var builder = new Checksum.Builder();

            foreach (var segment in collection.Segments)
            {
                foreach (var item in segment.Span)
                {
                    builder.Append(item.Checksum);
                }
            }

            return builder.FreeAndGetChecksum();
        });

        private bool UseLookupTable => Count > 8;

        private Dictionary<Checksum, int> LookupTable
        {
            get
            {
                Debug.Assert(UseLookupTable);
                return _lazyLookupTable.GetValue(this);
            }
        }

        internal override Checksum Checksum
            => _lazyChecksum.GetValue(this);

        public override int IndexOf(TagHelperDescriptor item)
        {
            if (UseLookupTable)
            {
                return LookupTable.TryGetValue(item.Checksum, out var index)
                    ? index
                    : -1;
            }

            var currentOffset = 0;

            foreach (var segment in Segments)
            {
                var index = segment.Span.IndexOf(item);

                if (index >= 0)
                {
                    return currentOffset + index;
                }

                currentOffset += segment.Length;
            }

            return -1;
        }

        public override void CopyTo(Span<TagHelperDescriptor> destination)
        {
            ArgHelper.ThrowIfDestinationTooShort(destination, Count);

            foreach (var segment in Segments)
            {
                segment.Span.CopyTo(destination);
                destination = destination[segment.Length..];
            }
        }
    }
}
