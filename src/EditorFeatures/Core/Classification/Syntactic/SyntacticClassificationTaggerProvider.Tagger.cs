// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Classification
{
    internal partial class SyntacticClassificationTaggerProvider
    {
        private class Tagger : ITagger<IClassificationTag>, IDisposable
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
                if (_tagComputer == null)
                    throw new ObjectDisposedException(GetType().FullName);

                return _tagComputer.GetTags(spans);
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
