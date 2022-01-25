// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Tagging
{
    internal abstract partial class AbstractAsynchronousTaggerProvider<TTag>
    {
        private partial class TagSource : IEqualityComparer<ITagSpan<TTag>>
        {
            public bool Equals(ITagSpan<TTag> x, ITagSpan<TTag> y)
                => x.Span == y.Span && EqualityComparer<TTag>.Default.Equals(x.Tag, y.Tag);

            public int GetHashCode(ITagSpan<TTag> obj)
                => Hash.Combine(obj.Span.GetHashCode(), EqualityComparer<TTag>.Default.GetHashCode(obj.Tag));
        }
    }
}
