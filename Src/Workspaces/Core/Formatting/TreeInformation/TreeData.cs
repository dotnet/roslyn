using System.Collections.Generic;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;
using Roslyn.Services.Shared.Utilities;
using Roslyn.Utilities;

namespace Roslyn.Services.Formatting
{
    /// <summary>
    /// this provides information about the syntax tree formatting service is formatting.
    /// this provides necessary abstraction between different kinds of syntax trees so that ones that contain
    /// actual text or cache can answer queries more efficiently.
    /// </summary>
    internal abstract partial class TreeData
    {
        public static TreeData Create(CommonSyntaxTree tree)
        {
#if DEBUG
            return new Debug(tree);
#else
            return new Tree(tree);
#endif
        }

        public static TreeData Create(CommonSyntaxNode root)
        {
            return new Node(root);
        }

        public abstract CommonSyntaxNode Root { get; }
        public abstract string GetTextBetween(CommonSyntaxToken token1, CommonSyntaxToken token2);
        public abstract int GetColumnOfToken(CommonSyntaxToken token, int tabSize);

        public virtual ValueTuple<TextSpan, TriviaData> GetFirstTriviaDataAndSpan(CommonSyntaxToken firstTokenInTree, TriviaData leadingTriviaInformation)
        {
            return ValueTuple.Create(TextSpan.FromBounds(this.StartPosition, firstTokenInTree.Span.Start), leadingTriviaInformation);
        }

        public virtual ValueTuple<TextSpan, TriviaData> GetLastTriviaDataAndSpan(CommonSyntaxToken lastTokenInTree, TriviaData trailingTriviaInformation)
        {
            return ValueTuple.Create(TextSpan.FromBounds(lastTokenInTree.Span.End, this.EndPosition), trailingTriviaInformation);
        }

        public int StartPosition { get { return this.Root.FullSpan.Start; } }
        public int EndPosition { get { return this.Root.FullSpan.End; } }

        public bool IsFirstToken(CommonSyntaxToken token)
        {
            var previousToken = token.GetPreviousToken(CommonSyntaxHelper.Any);
            return (previousToken.Kind == 0) ? true : false;
        }

        public bool IsLastToken(CommonSyntaxToken token)
        {
            if (this.IsEndOfFileToken(token))
            {
                return false;
            }

            var nextToken = token.GetNextToken(CommonSyntaxHelper.Any);
            return (nextToken.Kind == 0 || this.IsEndOfFileToken(nextToken)) ? true : false;
        }

        public IEnumerable<CommonSyntaxToken> GetApplicableTokens(TextSpan span)
        {
            return this.Root.DescendantTokens(span);
        }

        public bool IsEndOfFileToken(CommonSyntaxToken token)
        {
            return this.Root.FullSpan.End == token.Span.Start;
        }
    }
}
