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
    /// What the async tagger infrastructure should do in the presence of text edits.
    /// </summary>
    [Flags]
    internal enum TaggerTextChangeBehavior
    {
        /// <summary>
        /// The async tagger infrastructure will not track any text changes and will not do 
        /// anything special in the presence of them.
        /// </summary>
        None = 0,

        /// <summary>
        /// The async tagger infrastructure will track text changes to the subject buffer it is 
        /// attached to.  The text changes will be provided to the <see cref="AsynchronousTaggerContext{TTag, TState}"/>
        /// that is passed to <see cref="IAsynchronousTaggerDataSource{TTag, TState}.ProduceTagsAsync"/>.
        /// </summary>
        TrackTextChanges = 1 << 0,

        /// <summary>
        /// The async tagger infrastructure will not track text changes to the subject buffer it is 
        /// attached to.  The text changes will be provided to the <see cref="AsynchronousTaggerContext{TTag, TState}"/>
        /// that is passed to <see cref="IAsynchronousTaggerDataSource{TTag, TState}.ProduceTagsAsync"/>.
        /// 
        /// Tags that intersect the text change range will immediately removed.
        /// </summary>
        RemoveTagsThatIntersectEdits = TrackTextChanges | (1 << 1)
    }

    /// <summary>
    /// Data source for the <see cref="AsynchronousTaggerProvider{TTag, TState}"/>.  This type tells the
    /// <see cref="AsynchronousTaggerProvider{TTag, TState}"/> when tags need to be recomputed, as well
    /// as producing the tags when requested.
    /// </summary>
    internal interface IAsynchronousTaggerDataSource<TTag, TState> where TTag : ITag
    {
        /// <summary>
        /// The behavior the tagger engine will have when text changes happen to the subject buffer
        /// it is attached to.  Most taggers can simply use <see cref="TaggerTextChangeBehavior.None"/>.
        /// However, advanced taggers that want to perform specialized behavior depending on what has
        /// actually changed in the file can specify <see cref="TaggerTextChangeBehavior.TrackTextChanges"/>.
        /// 
        /// If this is specified the tagger engine will track text changes and pass them along as
        /// <see cref="AsynchronousTaggerContext{TTag, TState}.TextChangeRange"/> when calling 
        /// <see cref="ProduceTagsAsync"/>.
        /// </summary>
        TaggerTextChangeBehavior TextChangeBehavior { get; }

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
        /// Comparer used to determine if two <see cref="ITag"/>s are the same.  This is used by
        /// the <see cref="AsynchronousTaggerProvider{TTag, TState}"/> to determine if a previous set of
        /// computed tags and a current set of computed tags should be considered the same or not.
        /// If they are the same, then the UI will not be updated.  If they are different then
        /// the UI will be updated for sets of tags that have been removed or added.
        /// </summary>
        /// <returns></returns>
        IEqualityComparer<TTag> TagComparer { get; }

        /// <summary>
        /// Creates the <see cref="ITaggerEventSource"/> that notifies the <see cref="AsynchronousTaggerProvider{TTag, TState}"/>
        /// that it should recompute tags for the text buffer after an appropriate <see cref="TaggerDelay"/>.
        /// </summary>
        ITaggerEventSource CreateEventSource(ITextView textViewOpt, ITextBuffer subjectBuffer);

        /// <summary>
        /// Called by the <see cref="AsynchronousTaggerProvider{TTag, TState}"/> infrastructure to determine
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
        /// Produce tags for the given context.
        /// </summary>
        Task ProduceTagsAsync(AsynchronousTaggerContext<TTag,TState> context);
    }
}