// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.Fixers;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.MetaAnalyzers.Fixers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CSharpPreferIsKindFix))]
    [Shared]
    public sealed class CSharpPreferIsKindFix : PreferIsKindFix
    {
        protected override SyntaxNode? TryGetNodeToFix(SyntaxNode root, TextSpan span)
        {
            var binaryExpression = root.FindNode(span, getInnermostNodeForTie: true).FirstAncestorOrSelf<BinaryExpressionSyntax>();
            if (binaryExpression.Left is not InvocationExpressionSyntax)
            {
                return null;
            }

            return binaryExpression;
        }

        protected override void FixDiagnostic(DocumentEditor editor, SyntaxNode nodeToFix)
        {
            editor.ReplaceNode(
                nodeToFix,
                (nodeToFix, generator) =>
                {
                    var binaryExpression = (BinaryExpressionSyntax)nodeToFix;
                    var invocation = (InvocationExpressionSyntax)binaryExpression.Left;
                    var newInvocation = invocation
                        .WithExpression(ConvertKindNameToIsKind(invocation.Expression))
                        .AddArgumentListArguments(SyntaxFactory.Argument(binaryExpression.Right.WithoutTrailingTrivia()))
                        .WithTrailingTrivia(binaryExpression.Right.GetTrailingTrivia());
                    var negate = binaryExpression.OperatorToken.IsKind(SyntaxKind.ExclamationEqualsToken);
                    if (negate)
                    {
                        return SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, newInvocation.WithoutLeadingTrivia()).WithLeadingTrivia(newInvocation.GetLeadingTrivia());
                    }
                    else
                    {
                        return newInvocation;
                    }
                });
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
