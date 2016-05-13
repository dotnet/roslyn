using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.ConvertToInterpolatedString
{
    internal partial class ConvertToInterpolatedStringRefactoringProvider
    {
        private class InterpolatedStringRewriter : CSharpSyntaxRewriter
        {
            private readonly ImmutableArray<ExpressionSyntax> expandedArguments;

            private InterpolatedStringRewriter(ImmutableArray<ExpressionSyntax> expandedArguments)
            {
                this.expandedArguments = expandedArguments;
            }

            public override SyntaxNode VisitInterpolation(InterpolationSyntax node)
            {
                var literalExpression = node.Expression as LiteralExpressionSyntax;
                if (literalExpression != null && literalExpression.IsKind(SyntaxKind.NumericLiteralExpression))
                {
                    var index = (int)literalExpression.Token.Value;
                    if (index >= 0 && index < expandedArguments.Length)
                    {
                        return node.WithExpression(FixTrivia(expandedArguments[index]));
                    }
                }

                return base.VisitInterpolation(node);
            }

            /// <summary>
            /// Since C# interpolations cannot be on more than one line, we need to remove newlines if possible.
            /// </summary>
            public static ExpressionSyntax FixTrivia(ExpressionSyntax node) =>
                node.ReplaceTokens(node.DescendantTokens(descendIntoTrivia: true), RemoveTriviaForTokens);

            private static SyntaxToken RemoveTriviaForTokens(SyntaxToken originalToken, SyntaxToken rewrittenToken) =>
                rewrittenToken
                    .WithLeadingTrivia(
                        ReplaceSyntaxKindsWithElasticSpace(rewrittenToken.LeadingTrivia, SyntaxKind.WhitespaceTrivia, SyntaxKind.EndOfLineTrivia))
                    .WithTrailingTrivia(
                        ReplaceSyntaxKindsWithElasticSpace(rewrittenToken.TrailingTrivia, SyntaxKind.WhitespaceTrivia, SyntaxKind.EndOfLineTrivia));

            private static SyntaxTriviaList ReplaceSyntaxKindsWithElasticSpace(SyntaxTriviaList trivialList, params SyntaxKind[] kinds) =>
                trivialList.Select(x => kinds.Any(y => x.IsKind(y)) ? SyntaxFactory.ElasticSpace : x).ToSyntaxTriviaList();

            public static InterpolatedStringExpressionSyntax Visit(InterpolatedStringExpressionSyntax interpolatedString, ImmutableArray<ExpressionSyntax> expandedArguments)
            {
                return ((InterpolatedStringExpressionSyntax)new InterpolatedStringRewriter(expandedArguments).Visit(interpolatedString)).WithAdditionalAnnotations(SpecializedFormattingAnnotation);
            }
        }
    }
}
