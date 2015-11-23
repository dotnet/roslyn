using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Tagging
{
    internal class TaggerContext<TTag> where TTag : ITag
    {
        private readonly ImmutableDictionary<ITextBuffer, TagSpanIntervalTree<TTag>> _existingTags;

        internal IEnumerable<DocumentSnapshotSpan> _spansTagged;
        internal ImmutableArray<ITagSpan<TTag>>.Builder tagSpans = ImmutableArray.CreateBuilder<ITagSpan<TTag>>();

        public IEnumerable<DocumentSnapshotSpan> SpansToTag { get; }
        public SnapshotPoint? CaretPosition { get; }

        /// <summary>
        /// The text that has changed between the last successful tagging and this new request to
        /// produce tags.  In order to be passed this value, <see cref="TaggerTextChangeBehavior.TrackTextChanges"/> 
        /// must be specified in <see cref="AbstractAsynchronousTaggerProvider{TTag}.TextChangeBehavior"/>.
        /// </summary>
        public TextChangeRange? TextChangeRange { get; }
        public CancellationToken CancellationToken { get; }

        /// <summary>
        /// The state of the tagger.  Taggers can use this to keep track of information across calls
        /// to <see cref="AbstractAsynchronousTaggerProvider{TTag}.ProduceTagsAsync(TaggerContext{TTag})"/>.  Note: state will
        /// only be preserved if the tagger infrastructure fully updates itself with the tags that 
        /// were produced.  i.e. if that tagging pass is canceled, then the state set here will not
        /// be preserved and the previous preserved state will be used the next time ProduceTagsAsync
        /// is called.
        /// </summary>
        public object State { get; set; }

        // For testing only.
        internal TaggerContext(
            Document document, ITextSnapshot snapshot,
            SnapshotPoint? caretPosition = null,
            TextChangeRange? textChangeRange = null,
            CancellationToken cancellationToken = default(CancellationToken))
            : this(null, new[] { new DocumentSnapshotSpan(document, snapshot.GetFullSpan()) }, 
                  caretPosition, textChangeRange, null, cancellationToken)
        {
        }

        internal TaggerContext(
            object state,
            IEnumerable<DocumentSnapshotSpan> spansToTag,
            SnapshotPoint? caretPosition,
            TextChangeRange? textChangeRange,
            ImmutableDictionary<ITextBuffer, TagSpanIntervalTree<TTag>> existingTags,
            CancellationToken cancellationToken)
        {
            this.State = state;
            this.SpansToTag = spansToTag;
            this.CaretPosition = caretPosition;
            this.TextChangeRange = textChangeRange;
            this.CancellationToken = cancellationToken;

            _spansTagged = spansToTag;
            _existingTags = existingTags;
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
            if (spansTagged == null)
            {
                throw new ArgumentNullException(nameof(spansTagged));
            }

            this._spansTagged = spansTagged;
        }

        public IEnumerable<ITagSpan<TTag>> GetExistingTags(SnapshotSpan span)
        {
            TagSpanIntervalTree<TTag> tree;
            return _existingTags != null && _existingTags.TryGetValue(span.Snapshot.TextBuffer, out tree)
                ? tree.GetIntersectingSpans(span)
                : SpecializedCollections.EmptyEnumerable<ITagSpan<TTag>>();
        }
    }
}