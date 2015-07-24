// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Tagging
{
    /// <summary>
    /// Data source for the <see cref="AsynchronousTaggerProvider{TTag}"/>.  This type tells the
    /// <see cref="AsynchronousTaggerProvider{TTag}"/> when tags need to be recomputed, as well
    /// as producing the tags when requested.
    /// </summary>
    internal interface IAsynchronousTaggerDataSource<TTag> where TTag : ITag
    {
        /// <summary>
        /// Whether or not the <see cref="AsynchronousTaggerProvider{TTag}"/> should remove a tag
        /// from the user interface if the user makes an edit that intersects with the span of the
        /// tag.  Removing may be appropriate if it is undesirable for stale tag data to be 
        /// presented to the user.  However, removal may also lead to a more noticeable tagging 
        /// experience for the user if tags quickly get removed and re-added.
        /// 
        /// Note: if you want tags to be removed that intersect edits, you must pass 'true' for
        /// the <code>reportChangedSpans</code> parameter of your
        /// <see cref="TaggerEventSources.OnTextChanged(ITextBuffer, TaggerDelay, bool)"/> event
        /// source.  Otherwise, the tagger infrastructure will know know which text ranges have
        /// been affected, and thus which tags to remove.
        /// </summary>
        bool RemoveTagsThatIntersectEdits { get; }

        /// <summary>
        /// The behavior of tags that are created by the async tagger.  This will matter for tags
        /// created for a previous version of a document that are mapped forward by the async
        /// tagging architecture.  This value cannot be <see cref="SpanTrackingMode.Custom"/>.
        /// </summary>
        SpanTrackingMode SpanTrackingMode { get; }

        /// <summary>
        /// Whether or not the the first set of tags for this tagger should be computed synchronously.
        /// </summary>
        bool ComputeTagsSynchronouslyIfNoAsynchronousComputationHasCompleted { get; }

        /// <summary>
        /// <code>true</code> if the tagger infrastructure can avoid recomputing tags when the 
        /// user's caret moves to an already existing tag.  This is useful to avoid work for
        /// features like Highlighting if the user is navigating between highlight tags.
        /// </summary>
        bool IgnoreCaretMovementToExistingTag { get; }

        /// <summary>
        /// Options controlling this tagger.  The tagger infrastructure will check this option
        /// against the buffer it is associated with to see if it should tag or not.
        /// 
        /// An empty enumerable, or null, can be returned to indicate that this tagger should 
        /// run unconditionally.
        /// </summary>
        IEnumerable<Option<bool>> Options { get; }

        IEnumerable<PerLanguageOption<bool>> PerLanguageOptions { get; }

        /// <summary>
        /// The amount of time the tagger engine will wait after tags are computed before updating
        /// the UI.  Return 'null' to get the default delay.
        /// </summary>
        TaggerDelay? UIUpdateDelay { get; }

        /// <summary>
        /// Comparer used to determine if two <see cref="ITag"/>s are the same.  This is used by
        /// the <see cref="AsynchronousTaggerProvider{TTag}"/> to determine if a previous set of
        /// computed tags and a current set of computed tags should be considered the same or not.
        /// If they are the same, then the UI will not be updated.  If they are different then
        /// the UI will be updated for sets of tags that have been removed or added.
        /// </summary>
        /// <returns></returns>
        IEqualityComparer<TTag> TagComparer { get; }

        /// <summary>
        /// Creates the <see cref="ITaggerEventSource"/> that notifies the <see cref="AsynchronousTaggerProvider{TTag}"/>
        /// that it should recompute tags for the text buffer after an appropriate <see cref="TaggerDelay"/>.
        /// </summary>
        ITaggerEventSource CreateEventSource(ITextView textViewOpt, ITextBuffer subjectBuffer);

        /// <summary>
        /// Called by the <see cref="AsynchronousTaggerProvider{TTag}"/> infrastructure to determine
        /// the set of spans that it should asynchronously tag.  This will be called in response to
        /// notifications from the <see cref="ITaggerEventSource"/> that something has changed, and
        /// will only be called from the UI thread.  The tagger infrastructure will then determine
        /// the <see cref="DocumentSnapshotSpan"/>s associated with these <see cref="SnapshotSpan"/>s
        /// and will asycnhronously call into <see cref="ProduceTagsAsync"/> at some point in
        /// the future to produce tags for these spans.
        /// 
        /// Return <code>null</code> to get the default set of spans tagged.  This will normally be 
        /// the span of the entire text buffer.
        /// </summary>
        IEnumerable<SnapshotSpan> GetSpansToTag(ITextView textViewOpt, ITextBuffer subjectBuffer);

        /// <summary>
        /// Produce tags for the given spans.
        /// </summary>
        /// <param name="snapshotSpans">A list of SnapshotSpans and their corresponding documents
        /// that tags should be computed for. It is guaranteed to contain at least one element. In
        /// some scenarios, snapshotSpans may contain spans for snapshots that correspond to
        /// different buffers entirely. It is guaranteed, however, that there were not be multiple
        /// spans from different snapshots from the same buffer.</param>
        /// <param name="caretPosition">The caret position, if a caret position exists in one of the
        /// buffers included in snapshotSpans.</param>
        /// <param name="addTag">Callback to invoke when a new tag has been produced.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>A list of tag spans</returns>
        Task ProduceTagsAsync(IEnumerable<DocumentSnapshotSpan> snapshotSpans, SnapshotPoint? caretPosition, Action<ITagSpan<TTag>> addTag, CancellationToken cancellationToken);
    }
}