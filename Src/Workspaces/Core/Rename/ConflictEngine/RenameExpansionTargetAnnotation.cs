using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Rename.ConflictEngine
{
    /// <summary>
    /// This annotation will be placed on syntax nodes or tokens that need to be expanded during the rename process.
    /// </summary>
    [Serializable]
    internal class RenameExpansionTargetAnnotation : RenameAnnotation
    {
        /// <summary>
        /// List of spans of the original syntax tree that will be covered by this expanded syntax node.
        /// E.g. in "Dim y = x + x", when renaming x, this annotation will be placed on the assignment statemtent
        /// and this list will contain two entries (both spans of the identifer tokens for "x").
        /// </summary>
        public readonly IReadOnlyCollection<TextSpan> OriginalSpans;

        /// <summary>
        /// Start position for the original node on which this annotation is applied.
        /// </summary>
        public readonly int OriginalStart;

        public RenameExpansionTargetAnnotation(IReadOnlyCollection<TextSpan> originalSpans, int originalStart, long sessionId)
            : base(sessionId)
        {
            this.OriginalSpans = originalSpans;
            this.OriginalStart = originalStart;
        }
    }
}