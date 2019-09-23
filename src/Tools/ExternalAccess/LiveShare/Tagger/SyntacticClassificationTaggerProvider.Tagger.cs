// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.ExternalAccess.LiveShare.Tagger
{
    /// <summary>
    /// this is almost straight copy from typescript for syntatic LSP experiement.
    /// we won't attemp to change code to follow Roslyn style until we have result of the experiement
    /// </summary>
    internal sealed partial class SyntacticClassificationTaggerProvider
    {
        private class Tagger : ITagger<IClassificationTag>, IDisposable
        {
            private TagComputer tagComputer;

            public Tagger(TagComputer tagComputer)
            {
                this.tagComputer = tagComputer;
                this.tagComputer.TagsChanged += OnTagsChanged;
            }

            public event EventHandler<SnapshotSpanEventArgs> /*ITagger<IClassificationTag>.*/TagsChanged;

            IEnumerable<ITagSpan<IClassificationTag>> ITagger<IClassificationTag>.GetTags(NormalizedSnapshotSpanCollection spans)
            {
                if (this.tagComputer == null)
                {
                    throw new ObjectDisposedException(nameof(SyntacticClassificationTaggerProvider) + "." + nameof(Tagger));
                }

                return this.tagComputer.GetTags(spans);
            }

            private void OnTagsChanged(object sender, SnapshotSpanEventArgs e)
            {
                TagsChanged?.Invoke(this, e);
            }

            public void Dispose()
            {
                if (this.tagComputer != null)
                {
                    this.tagComputer.TagsChanged -= OnTagsChanged;
                    this.tagComputer.DecrementReferenceCountAndDisposeIfNecessary();
                    this.tagComputer = null;
                }
            }
        }
    }
}
