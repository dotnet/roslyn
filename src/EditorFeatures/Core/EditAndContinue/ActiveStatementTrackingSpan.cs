// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Contracts.EditAndContinue;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal readonly struct ActiveStatementTrackingSpan(ITrackingSpan trackingSpan, int ordinal, ActiveStatementFlags flags, DocumentId? unmappedDocumentId)
    {
        public readonly ITrackingSpan Span = trackingSpan;
        public readonly int Ordinal = ordinal;
        public readonly ActiveStatementFlags Flags = flags;
        public readonly DocumentId? UnmappedDocumentId = unmappedDocumentId;

        /// <summary>
        /// True if at least one of the threads whom this active statement belongs to is in a leaf frame.
        /// </summary>
        public bool IsLeaf => (Flags & ActiveStatementFlags.LeafFrame) != 0;

        public static ActiveStatementTrackingSpan Create(ITextSnapshot snapshot, ActiveStatementSpan span)
            => new(snapshot.CreateTrackingSpan(snapshot.GetTextSpan(span.LineSpan).ToSpan(), SpanTrackingMode.EdgeExclusive), span.Ordinal, span.Flags, span.UnmappedDocumentId);
    }
}
