// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Simplification;

internal sealed partial class CSharpExtensionMethodReducer : AbstractCSharpReducer
{
    private static readonly ObjectPool<IReductionRewriter> s_pool = new(
        () => new Rewriter(s_pool));

    private static readonly Func<InvocationExpressionSyntax, SemanticModel, CSharpSimplifierOptions, CancellationToken, SyntaxNode> s_simplifyExtensionMethod = SimplifyExtensionMethod;

    public CSharpExtensionMethodReducer() : base(s_pool)
    {
    }

    protected override bool IsApplicable(CSharpSimplifierOptions options)
       => true;

    private static SyntaxNode SimplifyExtensionMethod(
        InvocationExpressionSyntax node,
        SemanticModel semanticModel,
        CSharpSimplifierOptions options,
        CancellationToken cancellationToken)
    {
        var rewrittenNode = node;

        if (node.Expression.Kind() == SyntaxKind.SimpleMemberAccessExpression)
        {
            var memberAccessName = (MemberAccessExpressionSyntax)node.Expression;
            rewrittenNode = TryReduceExtensionMethod(node, semanticModel, rewrittenNode, memberAccessName.Name);
        }
        else if (node.Expression is SimpleNameSyntax)
        {
            rewrittenNode = TryReduceExtensionMethod(node, semanticModel, rewrittenNode, (SimpleNameSyntax)node.Expression);
        }

        return rewrittenNode;
    }

    private static InvocationExpressionSyntax TryReduceExtensionMethod(InvocationExpressionSyntax node, SemanticModel semanticModel, InvocationExpressionSyntax rewrittenNode, SimpleNameSyntax expressionName)
    {
        var targetSymbol = semanticModel.GetSymbolInfo(expressionName);

        if (targetSymbol.Symbol != null && targetSymbol.Symbol.Kind == SymbolKind.Method)
        {
            var targetMethodSymbol = (IMethodSymbol)targetSymbol.Symbol;
            if (!targetMethodSymbol.IsReducedExtension())
            {
                var argumentList = node.ArgumentList;
                var noOfArguments = argumentList.Arguments.Count;

                if (noOfArguments > 0)
                {
                    MemberAccessExpressionSyntax newMemberAccess = null;
                    var invocationExpressionNodeExpression = node.Expression;

                    // Ensure the first expression is parenthesized so that we don't cause any
                    // precedence issues when we take the extension method and tack it on the 
                    // end of it.
                    var expression = argumentList.Arguments[0].Expression.Parenthesize();

                    if (node.Expression.Kind() == SyntaxKind.SimpleMemberAccessExpression)
                    {
                        newMemberAccess = SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression, expression,
                            ((MemberAccessExpressionSyntax)invocationExpressionNodeExpression).OperatorToken,
                            ((MemberAccessExpressionSyntax)invocationExpressionNodeExpression).Name);
                    }
                    else if (node.Expression.Kind() == SyntaxKind.IdentifierName)
                    {
                        newMemberAccess = SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression, expression,
                            (IdentifierNameSyntax)invocationExpressionNodeExpression.WithoutLeadingTrivia());
                    }
                    else if (node.Expression.Kind() == SyntaxKind.GenericName)
                    {
                        newMemberAccess = SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression, expression,
                            (GenericNameSyntax)invocationExpressionNodeExpression.WithoutLeadingTrivia());
                    }
                    else
                    {
                        Debug.Assert(false, "The expression kind is not MemberAccessExpression or IdentifierName or GenericName to be converted to Member Access Expression for Ext Method Reduction");
                    }

                    if (newMemberAccess == null)
                    {
                        return node;
                    }

                    // Preserve Trivia
                    newMemberAccess = newMemberAccess.WithLeadingTrivia(node.GetLeadingTrivia());

                    // Below removes the first argument
                    // we need to reuse the separators to maintain existing formatting & comments in the arguments itself
                    var newArguments = SyntaxFactory.SeparatedList<ArgumentSyntax>(argumentList.Arguments.GetWithSeparators().AsEnumerable().Skip(2));

                    var rewrittenArgumentList = argumentList.WithArguments(newArguments);
                    var candidateRewrittenNode = SyntaxFactory.InvocationExpression(newMemberAccess, rewrittenArgumentList);

                    var oldSymbol = semanticModel.GetSymbolInfo(node).Symbol;
                    var newSymbol = semanticModel.GetSpeculativeSymbolInfo(
                        node.SpanStart,
                        candidateRewrittenNode,
                        SpeculativeBindingOption.BindAsExpression).Symbol;

                    if (oldSymbol != null &&
                        newSymbol is IMethodSymbol newMethod &&
                        oldSymbol.Equals(newMethod.GetConstructedReducedFrom()))
                    {
                        rewrittenNode = candidateRewrittenNode;
                    }
                }
            }
        }

        return rewrittenNode;
    }
}
