// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Tagging
{
    /// <summary>
    /// Produces <see cref="ITag"/>s for a given <see cref="SnapshotSpan"/>s in a
    /// <see cref="Document"/>.
    /// </summary>
    internal interface ITagProducer<TTag> : IDisposable
        where TTag : ITag
    {
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
        /// When the asynchronous tagger infrastructure has a set of spans it would like to tag, it
        /// will first call into this method to allow the ITagProducer to select a set of snapshot 
        /// spans that should be tagged first and shown to the user right away.  After that set 
        /// has been tagged, the tagger infrastructure will then call into the ITagProducer to tag 
        /// the full set.
        /// 
        /// An ITagProducer can use this to prioritize tagging just a portion of one of these spans.
        /// For example, the portion of the span that may be visible to the user in an editor.
        /// 
        /// An ITagProducer can return <code>null</code> to indicate that there are no snapshot 
        /// spans that should have priority.
        /// </summary>
        IEnumerable<DocumentSnapshotSpan> GetPrioritySpans(IEnumerable<DocumentSnapshotSpan> snapshotSpans);

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
        /// <param name="cancellationToken"></param>
        /// <returns>A list of tag spans</returns>
        Task<IEnumerable<ITagSpan<TTag>>> ProduceTagsAsync(IEnumerable<DocumentSnapshotSpan> snapshotSpans, SnapshotPoint? caretPosition, CancellationToken cancellationToken);
    }
}
