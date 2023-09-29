// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.PrivateAnalyzers;

internal static class CSharpSyntaxNodeExtensions
{
    public static bool IsInExpressionTree(
        [NotNullWhen(true)] this SyntaxNode? node,
        SemanticModel semanticModel,
        [NotNullWhen(true)] INamedTypeSymbol? expressionType,
        CancellationToken cancellationToken)
    {
        if (expressionType != null)
        {
            for (var current = node; current != null; current = current.Parent)
            {
                if (current.IsAnyLambda())
                {
                    var typeInfo = semanticModel.GetTypeInfo(current, cancellationToken);
                    if (expressionType.Equals(typeInfo.ConvertedType?.OriginalDefinition))
                        return true;
                }
                else if (current is SelectOrGroupClauseSyntax or OrderingSyntax)
                {
                    var info = semanticModel.GetSymbolInfo(current, cancellationToken);
                    if (AnyTakesExpressionTree(info, expressionType))
                        return true;
                }
                else if (current is QueryClauseSyntax queryClause)
                {
                    var info = semanticModel.GetQueryClauseInfo(queryClause, cancellationToken);
                    if (AnyTakesExpressionTree(info.CastInfo, expressionType) ||
                        AnyTakesExpressionTree(info.OperationInfo, expressionType))
                    {
                        return true;
                    }
                }
            }
        }

        return false;

        static bool AnyTakesExpressionTree(SymbolInfo info, INamedTypeSymbol expressionType)
        {
            if (TakesExpressionTree(info.Symbol, expressionType))
            {
                return true;
            }

            foreach (var symbol in info.CandidateSymbols)
            {
                if (TakesExpressionTree(symbol, expressionType))
                {
                    return true;
                }
            }

            return false;
        }

        static bool TakesExpressionTree([NotNullWhen(true)] ISymbol? symbol, INamedTypeSymbol expressionType)
        {
            if (symbol is IMethodSymbol { Parameters: [var firstParameter, ..] }
                && SymbolEqualityComparer.Default.Equals(expressionType, firstParameter.Type?.OriginalDefinition))
            {
                return true;
            }

            return false;
        }
    }

    public static bool IsAnyLambda(this SyntaxNode? node)
    {
        return node.IsKind(SyntaxKind.ParenthesizedLambdaExpression)
            || node.IsKind(SyntaxKind.SimpleLambdaExpression);
    }
}
