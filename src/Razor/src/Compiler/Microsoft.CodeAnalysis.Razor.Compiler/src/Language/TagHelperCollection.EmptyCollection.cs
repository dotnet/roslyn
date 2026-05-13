// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.Utilities;

namespace Microsoft.AspNetCore.Razor.Language;

public abstract partial class TagHelperCollection
{
    /// <summary>
    ///  Represents an immutable, empty collection of tag helpers.
    /// </summary>
    private sealed class EmptyCollection : TagHelperCollection
    {
        public static readonly EmptyCollection Instance = new();

        private EmptyCollection()
        {
        }

        public override int Count => 0;

        public override TagHelperDescriptor this[int index]
            => throw new IndexOutOfRangeException();

        internal override Checksum Checksum => Checksum.Null;

        public override int IndexOf(TagHelperDescriptor item) => -1;

        public override void CopyTo(Span<TagHelperDescriptor> destination)
        {
            // Nothing to copy.
        }

        protected override int SegmentCount => 0;

        protected override ReadOnlyMemory<TagHelperDescriptor> GetSegment(int index)
            => Assumed.Unreachable<ReadOnlyMemory<TagHelperDescriptor>>();
    }
}
