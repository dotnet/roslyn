// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Classification;

internal partial class SyntacticClassificationTaggerProvider
{
    private sealed class Tagger : EfficientTagger<IClassificationTag>, IDisposable
    {
        private TagComputer? _tagComputer;

        public Tagger(TagComputer tagComputer)
        {
            _tagComputer = tagComputer;
            _tagComputer.TagsChanged += OnTagsChanged;
        }

        public override void AddTags(
            NormalizedSnapshotSpanCollection spans,
            SegmentedList<ITagSpan<IClassificationTag>> tags)
        {
            var tagComputer = _tagComputer ?? throw new ObjectDisposedException(GetType().FullName);
            tagComputer.AddTags(spans, tags);
        }

        public override void Dispose()
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
