using System;
using System.Collections.Generic;

namespace Roslyn.Compilers
{
    public interface ISyntaxNode : IBaseSyntaxNode
    {
        /// <summary>
        /// The node that holds this node in its Children collection
        /// </summary>
        ISyntaxNode Parent { get; }

        /// <summary>
        /// The absolute span of this node in characters, not including leading and trailing trivia
        /// </summary>
        TextSpan Span { get; }

        /// <summary>
        /// The absolute span of this node in characters, including leading and trailing trivia
        /// </summary>
        TextSpan FullSpan { get; }

        bool IsStructuredTrivia { get; }

        /// <summary>
        /// Finds a token whose span includes the given position. 
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="findInsideTrivia">
        /// True to return tokens that are part of trivia.
        /// If false finds the token whose full span (including trivia) includes the position.
        /// </param>
        CommonSyntaxToken FindToken(int position, bool findInsideTrivia = false);

        /// <summary>
        /// Finds a trivia whose span includes the given position. 
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="findInsideTrivia">Whether to search inside structured trivia.</param>
        CommonSyntaxTrivia FindTrivia(int position, bool findInsideTrivia = false);

        /// <summary>
        /// Gets the first token of the tree rooted by this node. Skips zero-width tokens.
        /// </summary>
        /// <returns>The first token or <c>default(CommonSyntaxToken)</c> if it doesn't exist.</returns>
        CommonSyntaxToken GetFirstToken();

        /// <summary>
        /// Gets the first token of the tree rooted by this node.
        /// </summary>
        /// <param name="predicate">Only tokens for which this predicate returns true are included. Pass null to include all tokens.</param>
        /// <param name="stepInto">
        /// Applied on every structured trivia. Return false if the tokens included in the trivia should be skipped. 
        /// Pass null to skip all structured trivia.
        /// </param>
        /// <returns>The first token or <c>default(CommonSyntaxToken)</c> if it doesn't exist.</returns>
        CommonSyntaxToken GetFirstToken(Func<CommonSyntaxToken, bool> predicate, Func<CommonSyntaxTrivia, bool> stepInto = null);

        /// <summary>
        /// Gets the last token of the tree rooted by this node. Skips zero-width tokens.
        /// </summary>
        /// <returns>The last token or <c>default(CommonSyntaxToken)</c> if it doesn't exist.</returns>
        CommonSyntaxToken GetLastToken();

        /// <summary>
        /// Gets the last token of the tree rooted by this node. 
        /// </summary>
        /// <param name="predicate">Only tokens for which this predicate returns true are included. Pass null to include all tokens.</param>
        /// <param name="stepInto">Applied on every structured trivia. Return false if the tokens included in the trivia should be skipped. Pass null to skip all structured trivia.</param>
        /// <returns>The last token or <c>default(CommonSyntaxToken)</c> if it doesn't exist.</returns>
        CommonSyntaxToken GetLastToken(Func<CommonSyntaxToken, bool> predicate, Func<CommonSyntaxTrivia, bool> stepInto = null);

        /// <summary>
        /// Enumerates all tokens under this node.
        /// </summary>
        IEnumerable<CommonSyntaxToken> GetTokens();

        /// <summary>
        /// Enumerates all tokens under this node. Only tokens for which
        /// <paramref name="predicate"/> returns true are added into the list.
        /// </summary>
        IEnumerable<CommonSyntaxToken> GetTokens(Func<CommonSyntaxToken, bool> predicate);
    }
}