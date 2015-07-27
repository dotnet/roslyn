// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Tagging
{
    internal abstract partial class AbstractAsynchronousTaggerProvider<TTag>
    {
        private class TagSpanComparer : IEqualityComparer<ITagSpan<TTag>>
        {
            private readonly IEqualityComparer<TTag> _tagComparer;

            public TagSpanComparer(IEqualityComparer<TTag> tagComparer)
            {
                _tagComparer = tagComparer;
            }

            public bool Equals(ITagSpan<TTag> x, ITagSpan<TTag> y)
            {
                if (x.Span != y.Span)
                {
                    return false;
                }

                return _tagComparer.Equals(x.Tag, y.Tag);
            }

            public int GetHashCode(ITagSpan<TTag> obj)
            {
                return obj.Span.GetHashCode() ^ _tagComparer.GetHashCode(obj.Tag);
            }
        }
    }
}