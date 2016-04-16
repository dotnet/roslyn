// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Simplification
{
    internal partial class CSharpExtensionMethodReducer : AbstractCSharpReducer
    {
        public override IExpressionRewriter CreateExpressionRewriter(OptionSet optionSet, CancellationToken cancellationToken)
        {
            return new Rewriter(optionSet, cancellationToken);
        }

        private static SyntaxNode SimplifyExtensionMethod(
            InvocationExpressionSyntax node,
            SemanticModel semanticModel,
            OptionSet optionSet,
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

                        if (node.Expression.Kind() == SyntaxKind.SimpleMemberAccessExpression)
                        {
                            newMemberAccess = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, argumentList.Arguments[0].Expression, ((MemberAccessExpressionSyntax)invocationExpressionNodeExpression).OperatorToken, ((MemberAccessExpressionSyntax)invocationExpressionNodeExpression).Name);
                        }
                        else if (node.Expression.Kind() == SyntaxKind.IdentifierName)
                        {
                            newMemberAccess = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, argumentList.Arguments[0].Expression, (IdentifierNameSyntax)invocationExpressionNodeExpression.WithoutLeadingTrivia());
                        }
                        else if (node.Expression.Kind() == SyntaxKind.GenericName)
                        {
                            newMemberAccess = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, argumentList.Arguments[0].Expression, (GenericNameSyntax)invocationExpressionNodeExpression.WithoutLeadingTrivia());
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

                        if (oldSymbol != null && newSymbol != null)
                        {
                            if (newSymbol.Kind == SymbolKind.Method && oldSymbol.Equals(((IMethodSymbol)newSymbol).GetConstructedReducedFrom()))
                            {
                                rewrittenNode = candidateRewrittenNode;
                            }
                        }
                    }
                }
            }

            return rewrittenNode;
        }
    }
}
