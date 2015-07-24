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
    internal interface ITagProducer<TTag>
        where TTag : ITag
    {
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
