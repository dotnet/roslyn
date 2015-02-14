// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging
{
    /// <summary>
    /// A specialization of <see cref="ITagProducer{TTag}" /> that only produces tags for single <see cref="Document" /> at a time.
    /// </summary>
    internal abstract class AbstractSingleDocumentTagProducer<TTag> : ITagProducer<TTag>
        where TTag : ITag
    {
        public virtual void Dispose()
        {
        }

        public virtual IEqualityComparer<TTag> TagComparer
        {
            get
            {
                return EqualityComparer<TTag>.Default;
            }
        }

        public Task<IEnumerable<ITagSpan<TTag>>> ProduceTagsAsync(IEnumerable<DocumentSnapshotSpan> snapshotSpans, SnapshotPoint? caretPosition, CancellationToken cancellationToken)
        {
            // This abstract class should only be used in places where the tagger will only ever be analyzing at most one
            // document and span. The .Single()s are appropriate here, and if you find yourself "fixing" a bug by replacing
            // them with .First() you don't understand this class in the first place.

            var snapshotSpan = snapshotSpans.Single().SnapshotSpan;
            var document = snapshotSpans.Single().Document;
            if (document == null)
            {
                return SpecializedTasks.EmptyEnumerable<ITagSpan<TTag>>();
            }

            return ProduceTagsAsync(document, snapshotSpan, GetCaretPosition(caretPosition, snapshotSpan), cancellationToken);
        }

        public abstract Task<IEnumerable<ITagSpan<TTag>>> ProduceTagsAsync(Document document, SnapshotSpan snapshotSpan, int? caretPosition, CancellationToken cancellationToken);

        private static int? GetCaretPosition(SnapshotPoint? caretPosition, SnapshotSpan snapshotSpan)
        {
            return caretPosition.HasValue && caretPosition.Value.Snapshot == snapshotSpan.Snapshot
                ? caretPosition.Value.Position : (int?)null;
        }
    }
}
