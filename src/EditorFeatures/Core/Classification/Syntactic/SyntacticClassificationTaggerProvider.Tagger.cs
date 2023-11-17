// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Classification
{
    internal partial class SyntacticClassificationTaggerProvider
    {
        internal sealed class Tagger : ITagger<IClassificationTag>, IDisposable
        {
            private TagComputer? _tagComputer;

            public Tagger(TagComputer tagComputer)
            {
                _tagComputer = tagComputer;
                _tagComputer.TagsChanged += OnTagsChanged;
            }

            public event EventHandler<SnapshotSpanEventArgs>? TagsChanged;

            public IEnumerable<ITagSpan<IClassificationTag>> GetTags(NormalizedSnapshotSpanCollection spans)
            {
                var tagComputer = _tagComputer ?? throw new ObjectDisposedException(GetType().FullName);

                var pooledTags = Classifier.GetPooledList<ITagSpan<IClassificationTag>>(out var tags);
                tagComputer.AddTags(spans, tags);

                if (tags.Count == 0)
                {
                    pooledTags.Dispose();
                    return Array.Empty<ITagSpan<IClassificationTag>>();
                }

                // intentionally do not dispose.
                return tags;
            }

            public void AddTags(
                NormalizedSnapshotSpanCollection spans,
                SegmentedList<ITagSpan<IClassificationTag>> tags)
            {
                var tagComputer = _tagComputer ?? throw new ObjectDisposedException(GetType().FullName);
                tagComputer.AddTags(spans, tags);
            }

            private void OnTagsChanged(object? sender, SnapshotSpanEventArgs e)
                => TagsChanged?.Invoke(this, e);

            public void Dispose()
            {
                if (_tagComputer != null)
                {
                    _tagComputer.TagsChanged -= OnTagsChanged;
                    _tagComputer.DecrementReferenceCount();
                    _tagComputer = null;
                }
            }
        }
    }
}
