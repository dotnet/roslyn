// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Preview;

internal class AbstractPreviewTaggerProvider<TTag> : ITaggerProvider
        where TTag : ITag
{
    private readonly object _key;
    private readonly TTag _tagInstance;

    protected AbstractPreviewTaggerProvider(object key, TTag tagInstance)
    {
        _key = key;
        _tagInstance = tagInstance;
    }

    public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        => new Tagger(buffer, _key, _tagInstance) as ITagger<T>;

    private sealed class Tagger : ITagger<TTag>
    {
        private readonly ITextBuffer _buffer;
        private readonly object _key;
        private readonly TTag _tagInstance;

        public Tagger(ITextBuffer buffer, object key, TTag tagInstance)
        {
            _buffer = buffer;
            _key = key;
            _tagInstance = tagInstance;
        }

        IEnumerable<ITagSpan<TTag>> ITagger<TTag>.GetTags(NormalizedSnapshotSpanCollection spans)
            => GetTags(spans);

        public IEnumerable<TagSpan<TTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (_buffer.Properties.TryGetProperty(_key, out NormalizedSnapshotSpanCollection matchingSpans))
            {
                var intersection = NormalizedSnapshotSpanCollection.Intersection(matchingSpans, spans);

                return intersection.Select(s => new TagSpan<TTag>(s, _tagInstance));
            }

            return [];
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged = (s, e) => { };
    }
}
