﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Preview
{
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
        {
            return new Tagger(buffer, _key, _tagInstance) as ITagger<T>;
        }

        private class Tagger : ITagger<TTag>
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

            public IEnumerable<ITagSpan<TTag>> GetTags(NormalizedSnapshotSpanCollection spans)
            {
                if (_buffer.Properties.TryGetProperty(_key, out NormalizedSnapshotSpanCollection matchingSpans))
                {
                    var intersection = NormalizedSnapshotSpanCollection.Intersection(matchingSpans, spans);

                    return intersection.Select(s => new TagSpan<TTag>(s, _tagInstance));
                }

                return SpecializedCollections.EmptyEnumerable<ITagSpan<TTag>>();
            }

            public event EventHandler<SnapshotSpanEventArgs> TagsChanged = (s, e) => { };
        }
    }
}
