// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Classification
{
    internal partial class SyntacticClassificationTaggerProvider
    {
        private class Tagger : IAccurateTagger<IClassificationTag>, IDisposable
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

            public IEnumerable<ITagSpan<IClassificationTag>> GetAllTags(NormalizedSnapshotSpanCollection spans, CancellationToken cancellationToken)
            {
                if (_tagComputer == null)
                {
                    throw new ObjectDisposedException("AbstractSyntacticClassificationTaggerProvider.Tagger");
                }

                return _tagComputer.GetAllTags(spans, cancellationToken);
            }

            private void OnTagsChanged(object sender, SnapshotSpanEventArgs e)
                => TagsChanged?.Invoke(this, e);

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
