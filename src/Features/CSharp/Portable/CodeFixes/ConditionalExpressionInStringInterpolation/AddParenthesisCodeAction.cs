// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.ConditionalExpressionInStringInterpolation
{
    internal partial class CSharpAddParenthesisAroundConditionalExpressionInInterpolatedStringCodeFixProvider
    {
        private sealed class AddParenthesisCodeAction : CodeAction
        {
            public AddParenthesisCodeAction(Document document, ConditionalExpressionSyntax conditionalExpressionSyntax, InterpolationSyntax interpolationSyntax)
            {
                this.Document = document;
                this.ConditionalExpressionSyntax = conditionalExpressionSyntax;
                this.InterpolationSyntax = interpolationSyntax;
            }

            private Document Document { get; }
            private ConditionalExpressionSyntax ConditionalExpressionSyntax { get; }
            private InterpolationSyntax InterpolationSyntax { get; }

            public override string Title => CSharpFeaturesResources.AddParenthesisAroundConditionalExpressionInInterpolatedString;

            protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                var root = await Document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var newInterpolationSyntax = GetParenthesizedConditionalExpressionSyntax(ConditionalExpressionSyntax, InterpolationSyntax);
                var newRoot = root.ReplaceNode(InterpolationSyntax, newInterpolationSyntax);
                return Document.WithSyntaxRoot(newRoot);
            }

            private static SyntaxNode GetParenthesizedConditionalExpressionSyntax(ConditionalExpressionSyntax conditionalExpressionSyntax, InterpolationSyntax interpolationSyntax)
            {

                var formatClause = interpolationSyntax.FormatClause;
                if (formatClause != null)
                {
                    var formatStringToken = formatClause.FormatStringToken;
                    var interpolationExpression = interpolationSyntax.Expression;
                    var newConditionalExpressionSyntax =
                        conditionalExpressionSyntax
                        .WithColonToken(SyntaxFactory.Token(SyntaxKind.ColonToken).WithTriviaFrom(formatClause.ColonToken))
                        .WithWhenFalse(SyntaxFactory.ParseExpression(formatStringToken.ValueText));
                    return SyntaxFactory.Interpolation(
                        interpolationSyntax.OpenBraceToken,
                        SyntaxFactory.ParenthesizedExpression(newConditionalExpressionSyntax).WithTriviaFrom(conditionalExpressionSyntax),
                        null,
                        null,
                        interpolationSyntax.CloseBraceToken.WithTriviaFrom(formatStringToken));
                }

                return conditionalExpressionSyntax;
            }
        }
    }
}
