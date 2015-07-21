using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Tagging
{
    internal class AsynchronousTaggerContext<TTag, TState> where TTag : ITag
    {
        public TState State { get; set; }
        public IEnumerable<DocumentSnapshotSpan> SpansToTag { get; }
        public SnapshotPoint? CaretPosition { get; }
        public CancellationToken CancellationToken { get; }

        internal IEnumerable<DocumentSnapshotSpan> spansTagged;
        internal ImmutableArray<ITagSpan<TTag>>.Builder tagSpans = ImmutableArray.CreateBuilder<ITagSpan<TTag>>();

        internal AsynchronousTaggerContext(
            TState state,
            IEnumerable<DocumentSnapshotSpan> spansToTag,
            SnapshotPoint? caretPosition,
            CancellationToken cancellationToken)
        {
            this.State = state;
            this.SpansToTag = spansToTag;
            this.CaretPosition = caretPosition;
            this.CancellationToken = cancellationToken;

            this.spansTagged = spansToTag;
        }

        public void AddTag(ITagSpan<TTag> tag)
        {
            tagSpans.Add(tag);
        }

        /// <summary>
        /// Used to allow taggers to indicate what spans were actually tagged.  This is useful 
        /// when the tagger decides to tag a different span than the entire file.  If a sub-span
        /// of a document is tagged then the tagger infrastructure will keep previously computed
        /// tags from before and after the sub-span and merge them with the newly produced tags.
        /// </summary>
        public void SetSpansTagged(IEnumerable<DocumentSnapshotSpan> spansTagged)
        {
            this.spansTagged = spansTagged;
        }
    }
}
