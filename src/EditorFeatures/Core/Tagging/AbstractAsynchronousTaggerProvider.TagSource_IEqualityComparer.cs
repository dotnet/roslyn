// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Tagging
{
    internal abstract partial class AbstractAsynchronousTaggerProvider<TTag>
    {
        private partial class TagSource
        {
            private sealed class TagSpanComparer : IEqualityComparer<ITagSpan<TTag>>
            {
                private readonly AbstractAsynchronousTaggerProvider<TTag> _provider;
                private readonly ITextSnapshot _snapshot;

                public TagSpanComparer(
                    AbstractAsynchronousTaggerProvider<TTag> provider,
                    ITextSnapshot snapshot)
                {
                    _provider = provider;
                    _snapshot = snapshot;
                }

                public bool Equals(ITagSpan<TTag> x, ITagSpan<TTag> y)
                    => x.Span == y.Span && _provider.Equals(_snapshot, x.Tag, y.Tag);

                /// <summary>
                /// For the purposes of hashing, just hash spans.  This will prevent most collisions.  And the rare
                /// collision of two tag spans with the same span will be handled by checking if their tags are the same
                /// through <see cref="Equals(ITagSpan{TTag}, ITagSpan{TTag})"/>.  This prevents us from having to
                /// define a suitable hashing strategy for all our tags.
                public int GetHashCode(ITagSpan<TTag> obj)
                    => obj.Span.TranslateTo(_snapshot, _provider.SpanTrackingMode).Span.GetHashCode();
            }
        }
    }
}
