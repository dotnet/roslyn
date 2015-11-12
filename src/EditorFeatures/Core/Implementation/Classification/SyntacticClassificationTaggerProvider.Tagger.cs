// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Classification
{
    internal partial class SyntacticClassificationTaggerProvider
    {
        private partial class Tagger : ITagger<IClassificationTag>, IDisposable
        {
            private TagComputer _tagComputer;

            public Tagger(TagComputer tagComputer)
            {
                _tagComputer = tagComputer;
                _tagComputer.TagsChanged += OnTagsChanged;
            }

            public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

            public IEnumerable<ITagSpan<IClassificationTag>> GetTags(NormalizedSnapshotSpanCollection spans)
            {
                if (_tagComputer == null)
                {
                    throw new ObjectDisposedException("AbstractSyntacticClassificationTaggerProvider.Tagger");
                }

                return _tagComputer.GetTags(spans);
            }

            private void OnTagsChanged(object sender, SnapshotSpanEventArgs e)
            {
                TagsChanged?.Invoke(this, e);
            }

            public void Dispose()
            {
                if (_tagComputer != null)
                {
                    _tagComputer.TagsChanged -= OnTagsChanged;
                    _tagComputer.DecrementReferenceCountAndDisposeIfNecessary();
                    _tagComputer = null;
                }
            }
        }
    }
}
