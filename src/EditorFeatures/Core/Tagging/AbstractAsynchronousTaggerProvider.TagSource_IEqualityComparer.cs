// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Tagging;

internal abstract partial class AbstractAsynchronousTaggerProvider<TTag>
{
    private partial class TagSource : IEqualityComparer<TagSpan<TTag>>
    {
        public bool Equals(TagSpan<TTag>? x, TagSpan<TTag>? y)
        {
            if (x == y)
                return true;

            if (x is null || y is null)
                return false;

            return x.Span == y.Span && _dataSource.TagEquals(x.Tag, y.Tag);
        }

        /// <summary>
        /// For the purposes of hashing, just hash spans.  This will prevent most collisions.  And the rare
        /// collision of two tag spans with the same span will be handled by checking if their tags are the same
        /// through <see cref="Equals(TagSpan{TTag}, TagSpan{TTag})"/>.  This prevents us from having to
        /// define a suitable hashing strategy for all our tags.
        /// </summary>
        public int GetHashCode(TagSpan<TTag> obj)
            => obj.Span.Span.GetHashCode();
    }
}
