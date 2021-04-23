// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Tagging
{
    internal partial class AbstractAsynchronousTaggerProvider<TTag>
    {
        /// <summary>
        /// <see cref="Tagger"/> is a thin wrapper we create around the single shared <see cref="TagSource"/>.
        /// Clients can request and dispose these at will.  Once the last wrapper is disposed, the underlying
        /// <see cref="TagSource"/> will finally be disposed as well.
        /// </summary>
        private sealed partial class Tagger : ITagger<TTag>, IDisposable
        {
            private readonly TagSource _tagSource;

            public Tagger(TagSource tagSource)
            {
                _tagSource = tagSource;
                _tagSource.OnTaggerAdded(this);
            }

            public event EventHandler<SnapshotSpanEventArgs> TagsChanged
            {
                add => _tagSource.TagsChanged += value;
                remove => _tagSource.TagsChanged -= value;
            }

            public void Dispose()
                => _tagSource.OnTaggerDisposed(this);

            public IEnumerable<ITagSpan<TTag>> GetTags(NormalizedSnapshotSpanCollection requestedSpans)
                => _tagSource.GetTags(requestedSpans);
        }
    }
}
