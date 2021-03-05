// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.Fixers;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.MetaAnalyzers.Fixers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CSharpPreferIsKindFix))]
    [Shared]
    public sealed class CSharpPreferIsKindFix : PreferIsKindFix
    {
        protected override async Task<Document> ConvertKindToIsKindAsync(Document document, TextSpan sourceSpan, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var binaryExpression = root.FindNode(sourceSpan, getInnermostNodeForTie: true).FirstAncestorOrSelf<BinaryExpressionSyntax>();

            if (binaryExpression.Left is not InvocationExpressionSyntax invocation)
            {
                return document;
            }

            var newInvocation = invocation
                .WithExpression(ConvertKindNameToIsKind(invocation.Expression))
                .AddArgumentListArguments(SyntaxFactory.Argument(binaryExpression.Right.WithoutTrailingTrivia()))
                .WithTrailingTrivia(binaryExpression.Right.GetTrailingTrivia());
            var negate = binaryExpression.OperatorToken.IsKind(SyntaxKind.ExclamationEqualsToken);
            SyntaxNode newRoot;
            if (negate)
            {
                newRoot = root.ReplaceNode(binaryExpression, SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, newInvocation.WithoutLeadingTrivia()).WithLeadingTrivia(newInvocation.GetLeadingTrivia()));
            }
            else
            {
                newRoot = root.ReplaceNode(binaryExpression, newInvocation);
            }

            return document.WithSyntaxRoot(newRoot);
        }

        private static ExpressionSyntax ConvertKindNameToIsKind(ExpressionSyntax expression)
        {
            if (expression is MemberAccessExpressionSyntax memberAccessExpression)
            {
                return memberAccessExpression.WithName(SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("IsKind")));
            }
            else
            {
                return expression;
            }
        }
    }
}
