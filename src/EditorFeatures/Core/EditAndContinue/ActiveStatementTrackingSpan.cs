// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Contracts.EditAndContinue;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal readonly struct ActiveStatementTrackingSpan
    {
        public readonly ITrackingSpan Span;
        public readonly int Ordinal;
        public readonly ActiveStatementFlags Flags;
        public readonly DocumentId? UnmappedDocumentId;

        public ActiveStatementTrackingSpan(ITrackingSpan trackingSpan, int ordinal, ActiveStatementFlags flags, DocumentId? unmappedDocumentId)
        {
            Span = trackingSpan;
            Ordinal = ordinal;
            Flags = flags;
            UnmappedDocumentId = unmappedDocumentId;
        }

        /// <summary>
        /// True if at least one of the threads whom this active statement belongs to is in a leaf frame.
        /// </summary>
        public bool IsLeaf => (Flags & ActiveStatementFlags.LeafFrame) != 0;

        public static ActiveStatementTrackingSpan Create(ITextSnapshot snapshot, ActiveStatementSpan span)
            => new(snapshot.CreateTrackingSpan(snapshot.GetTextSpan(span.LineSpan).ToSpan(), SpanTrackingMode.EdgeExclusive), span.Ordinal, span.Flags, span.UnmappedDocumentId);
    }
}
