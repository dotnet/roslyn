using Roslyn.Compilers;
using Roslyn.Compilers.Common;
using Roslyn.Services.Shared.Extensions;
using Roslyn.Services.Shared.Utilities;
using Roslyn.Utilities;

namespace Roslyn.Services.Formatting
{
    internal abstract partial class TreeData
    {
        private class Node : TreeData
        {
            // TODO : should I support rootNode not being the root node of whole tree?
            private readonly CommonSyntaxNode rootNode;

            public Node(CommonSyntaxNode rootNode)
            {
                Contract.ThrowIfNull(rootNode);
                Contract.ThrowIfFalse(rootNode.GetFirstToken(CommonSyntaxHelper.Any).Kind != 0);
                Contract.ThrowIfFalse(rootNode.GetFirstToken(CommonSyntaxHelper.Any).FullSpan.Start == 0);

                this.rootNode = rootNode;
            }

            private string GetLeadingTriviaText(CommonSyntaxToken token)
            {
                if (!token.HasLeadingTrivia)
                {
                    return string.Empty;
                }

                var builder = StringBuilderPool.Allocate();
                foreach (var trivia in token.LeadingTrivia)
                {
                    builder.Append(trivia.GetFullText());
                }

                return StringBuilderPool.ReturnAndFree(builder);
            }

            private string GetTrailingTriviaText(CommonSyntaxToken token)
            {
                if (!token.HasTrailingTrivia)
                {
                    return string.Empty;
                }

                var builder = StringBuilderPool.Allocate();
                foreach (var trivia in token.TrailingTrivia)
                {
                    builder.Append(trivia.GetFullText());
                }

                return StringBuilderPool.ReturnAndFree(builder);
            }

            public override int GetColumnOfToken(CommonSyntaxToken token, int tabSize)
            {
                Contract.ThrowIfTrue(token.Kind == 0);

                // first find one that has new line text
                var startToken = GetTokenWithLineBreaks(token);

                // get last line text from text between them
                var lineText = GetTextBetween(startToken, token).GetLastLineText();

                return lineText.ConvertStringTextPositionToColumn(tabSize, lineText.Length);
            }

            private CommonSyntaxToken GetTokenWithLineBreaks(CommonSyntaxToken token)
            {
                var currentToken = token.GetPreviousToken(CommonSyntaxHelper.Any);

                while (currentToken.Kind != 0)
                {
                    if (currentToken.GetFullText().IndexOf('\n') >= 0)
                    {
                        return currentToken;
                    }

                    currentToken = currentToken.GetPreviousToken(CommonSyntaxHelper.Any);
                }

                return default(CommonSyntaxToken);
            }

            public override string GetTextBetween(CommonSyntaxToken token1, CommonSyntaxToken token2)
            {
                Contract.ThrowIfTrue(token1.Kind == 0 && token2.Kind == 0);
                Contract.ThrowIfTrue(token1.Equals(token2));

                if (token1.Kind == 0)
                {
                    return GetLeadingTriviaText(token2);
                }

                if (token2.Kind == 0)
                {
                    return GetTrailingTriviaText(token1);
                }

                Contract.ThrowIfFalse(token1.FullSpan.Start <= token2.FullSpan.Start);

                if (token1.FullSpan.End == token2.FullSpan.Start)
                {
                    return GetTextBetweenTwoAdjacentTokens(token1, token2);
                }

                var builder = StringBuilderPool.Allocate();
                builder.Append(GetTrailingTriviaText(token1));

                for (var token = token1.GetNextToken(CommonSyntaxHelper.Any); token.FullSpan.End <= token2.FullSpan.Start; token = token.GetNextToken(CommonSyntaxHelper.Any))
                {
                    builder.Append(token.GetFullText());
                }

                builder.Append(GetLeadingTriviaText(token2));

                return StringBuilderPool.ReturnAndFree(builder);
            }

            private string GetTextBetweenTwoAdjacentTokens(CommonSyntaxToken token1, CommonSyntaxToken token2)
            {
                var trailingText = GetTrailingTriviaText(token1);
                var leadingText = GetLeadingTriviaText(token2);

                return trailingText + leadingText;
            }

            public override CommonSyntaxNode Root
            {
                get { return this.rootNode; }
            }
        }
    }
}
