// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
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
        /// Creates the <see cref="ITagProducer{TTag}"/> which will be used by the 
        /// <see cref="AsynchronousTaggerProvider{TTag}"/> to produce tags asynchronously.
        /// </summary>
        ITagProducer<TTag> CreateTagProducer();
    }
}