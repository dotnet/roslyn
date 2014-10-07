using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Rename.ConflictEngine
{
    /// <summary>
    /// This annotation will be placed on tokens that need purely textual renames within the tokens, without any semantic checks.
    /// For example, we place these annotations on string literal tokens when renaming sub-strings within a string that match the original rename symbol text.
    /// We also place these annotations on tokens whose comment trivia needs textual renames of sub-strings matching the original rename symbol text.
    /// </summary>
    [Serializable]
    internal class RenameWithinTokenAnnotation : RenameAnnotation
    {
        /// <summary>
        /// List of spans of the original syntax tree that need to be renamed.
        /// Note that a token might require more than one partial renames inside it, hence the we store all the spans within the token's full span that require a rename.
        /// </summary>
        public readonly IReadOnlyCollection<TextSpan> OriginalRenameSpansWithinToken;

        /// <summary>
        /// Start position of full span of the original token.
        /// </summary>
        public readonly int OriginalTokenFullSpanStart;

        /// <summary>
        /// Flag indicating whether the annotation corresponds to a partial rename within a string token.
        /// </summary>
        public readonly bool IsRenameInString;

        /// <summary>
        /// Flag indicating whether the annotation corresponds to a partial rename within a comment trivia of a token.
        /// </summary>
        public readonly bool IsRenameInComment;

        public RenameWithinTokenAnnotation(IReadOnlyCollection<TextSpan> originalRenameSpansWithinToken, int originalTokenFullSpanStart, bool isRenameInString, bool isRenameInComment, long sessionId)
            : base(sessionId)
        {
            this.OriginalRenameSpansWithinToken = originalRenameSpansWithinToken;
            this.OriginalTokenFullSpanStart = originalTokenFullSpanStart;
            this.IsRenameInString = isRenameInString;
            this.IsRenameInComment = isRenameInComment;
        }
    }
}