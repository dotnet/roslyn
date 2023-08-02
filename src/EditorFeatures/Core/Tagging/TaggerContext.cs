// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Tagging
{
    internal class TaggerContext<TTag> where TTag : ITag
    {
        private readonly ImmutableDictionary<ITextBuffer, TagSpanIntervalTree<TTag>> _existingTags;

        private List<SnapshotSpan> _spansActuallyTagged;
        public readonly SegmentedList<ITagSpan<TTag>> TagSpans = new();

        public ImmutableArray<DocumentSnapshotSpan> SpansToTag { get; }
        public SnapshotPoint? CaretPosition { get; }

        /// <summary>
        /// The text that has changed between the last successful tagging and this new request to
        /// produce tags.  In order to be passed this value, <see cref="TaggerTextChangeBehavior.TrackTextChanges"/> 
        /// must be specified in <see cref="AbstractAsynchronousTaggerProvider{TTag}.TextChangeBehavior"/>.
        /// </summary>
        public TextChangeRange? TextChangeRange { get; }

        /// <summary>
        /// The state of the tagger.  Taggers can use this to keep track of information across calls
        /// to <see cref="AbstractAsynchronousTaggerProvider{TTag}.ProduceTagsAsync(TaggerContext{TTag}, CancellationToken)"/>.  Note: state will
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
            TextChangeRange? textChangeRange = null)
            : this(state: null, ImmutableArray.Create(new DocumentSnapshotSpan(document, snapshot.GetFullSpan())),
                   caretPosition, textChangeRange, existingTags: null)
        {
        }

        internal TaggerContext(
            object state,
            ImmutableArray<DocumentSnapshotSpan> spansToTag,
            SnapshotPoint? caretPosition,
            TextChangeRange? textChangeRange,
            ImmutableDictionary<ITextBuffer, TagSpanIntervalTree<TTag>> existingTags)
        {
            this.State = state;
            this.SpansToTag = spansToTag;
            this.CaretPosition = caretPosition;
            this.TextChangeRange = textChangeRange;

            _existingTags = existingTags;
        }

        public void AddTag(ITagSpan<TTag> tag)
            => TagSpans.Add(tag);

        public void ClearTags()
            => TagSpans.Clear();

        public ImmutableArray<SnapshotSpan> GetSpansActuallyTagged()
        {
            // If the actual tagger didn't report anything different, then we tagged everything we were asked to tag.
            if (_spansActuallyTagged is null)
                return SpansToTag.SelectAsArray(ds => ds.SnapshotSpan);

            return _spansActuallyTagged.ToImmutableArray();
        }

        /// <summary>
        /// Used to allow taggers to indicate what spans were actually tagged.  This is useful when the tagger decides
        /// to tag a different span than the entire file.  If a sub-span of a document is tagged then the tagger
        /// infrastructure will keep previously computed tags from before and after the sub-span and merge them with the
        /// newly produced tags.
        /// </summary>
        public void AddActuallyTaggedSpan(SnapshotSpan taggedSpan)
        {
            _spansActuallyTagged ??= new();
            _spansActuallyTagged.Add(taggedSpan);
        }

        public void ClearSpansActuallyTagged()
        {
            _spansActuallyTagged = new();
        }

        public bool HasExistingContainingTags(SnapshotPoint point)
            => _existingTags != null &&
               _existingTags.TryGetValue(point.Snapshot.TextBuffer, out var tree) &&
               tree.HasSpanThatContains(point);
    }
}
