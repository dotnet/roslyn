// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.LanguageService;

internal static class ISemanticFactsExtensions
{
    public static bool IsSafeToChangeAssociativity<TBinaryExpressionSyntax>(
        this ISemanticFacts semanticFacts,
        TBinaryExpressionSyntax innerBinary,
        TBinaryExpressionSyntax parentBinary,
        SemanticModel semanticModel)
        where TBinaryExpressionSyntax : SyntaxNode
    {
        // Now we'll perform a few semantic checks to determine whether removal 
        // of the parentheses might break semantics. Note that we'll try and be 
        // fairly conservative with these. For example, we'll assume that failing 
        // any of these checks results in the parentheses being declared as necessary 
        // -- even if they could be removed depending on whether the parenthesized
        // expression appears on the left or right side of the parent binary expression.

        // First, does the binary expression result in an operator overload being 
        // called?
        var symbolInfo = semanticModel.GetSymbolInfo(innerBinary);
        if (AnySymbolIsUserDefinedOperator(symbolInfo))
            return false;

        // Second, check the type and converted type of the binary expression.
        // Are they the same?
        var innerTypeInfo = semanticModel.GetTypeInfo(innerBinary);
        if (innerTypeInfo is { Type: not null, ConvertedType: not null } &&
            !innerTypeInfo.Type.Equals(innerTypeInfo.ConvertedType))
        {
            return false;
        }

        // It's not safe to change associativity for dynamic variables as the actual type isn't known. See https://github.com/dotnet/roslyn/issues/47365
        if (innerTypeInfo.Type is IDynamicTypeSymbol)
            return false;

        semanticFacts.SyntaxFacts.GetPartsOfBinaryExpression(parentBinary, out var parentBinaryLeft, out var parentBinaryRight);

        // Only allow us to change associativity if all the types are the same.
        // for example, if we have: int + (int + long)  then we don't want to
        // change things such that we effectively have (int + int) + long
        if (!Equals(semanticModel.GetTypeInfo(parentBinaryLeft).Type,
                    semanticModel.GetTypeInfo(parentBinaryRight).Type))
        {
            return false;
        }

        if (!Equals(semanticModel.GetTypeInfo(parentBinaryLeft).ConvertedType,
                    semanticModel.GetTypeInfo(parentBinaryRight).ConvertedType))
        {
            return false;
        }

        // Floating point is not safe to change associativity of.  For example, if the user has "large * (large *
        // small)" then this will become "(large * large) * small.  And that could easily overflow to Inf (and other
        // badness).
        var outerTypeInfo = semanticModel.GetTypeInfo(parentBinary);
        if (IsFloatingPoint(innerTypeInfo) || IsFloatingPoint(outerTypeInfo))
            return false;

        if (semanticModel.GetOperation(parentBinary) is IBinaryOperation parentBinaryOp &&
            semanticModel.GetOperation(innerBinary) is IBinaryOperation innerBinaryOp)
        {
            if ((parentBinaryOp.IsChecked || innerBinaryOp.IsChecked) &&
                (IsArithmetic(parentBinaryOp) || IsArithmetic(innerBinaryOp)))
            {
                // For checked operations, we can't change which type of operator we're performing in a row as that
                // could lead to overflow if we end up doing something like an addition prior to a subtraction.
                return false;
            }
        }

        return true;

        static bool IsArithmetic(IBinaryOperation op)
        {
            return op.OperatorKind is BinaryOperatorKind.Add or
                BinaryOperatorKind.Subtract or
                BinaryOperatorKind.Multiply or
                BinaryOperatorKind.Divide;
        }
    }

    private static bool AnySymbolIsUserDefinedOperator(SymbolInfo symbolInfo)
    {
        if (IsUserDefinedOperator(symbolInfo.Symbol))
        {
            return true;
        }

        foreach (var symbol in symbolInfo.CandidateSymbols)
        {
            if (IsUserDefinedOperator(symbol))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsUserDefinedOperator([NotNullWhen(returnValue: true)] ISymbol? symbol)
        => symbol is IMethodSymbol methodSymbol &&
           methodSymbol.MethodKind == MethodKind.UserDefinedOperator;

    private static bool IsFloatingPoint(TypeInfo typeInfo)
        => IsFloatingPoint(typeInfo.Type) || IsFloatingPoint(typeInfo.ConvertedType);

    private static bool IsFloatingPoint([NotNullWhen(returnValue: true)] ITypeSymbol? type)
        => type?.SpecialType is SpecialType.System_Single or SpecialType.System_Double;

    public static IParameterSymbol? FindParameterForArgument(this ISemanticFacts semanticFacts, SemanticModel semanticModel, SyntaxNode argument, CancellationToken cancellationToken)
        => semanticFacts.FindParameterForArgument(semanticModel, argument, allowUncertainCandidates: false, allowParams: false, cancellationToken);

    public static IParameterSymbol? FindParameterForAttributeArgument(this ISemanticFacts semanticFacts, SemanticModel semanticModel, SyntaxNode argument, CancellationToken cancellationToken)
        => semanticFacts.FindParameterForAttributeArgument(semanticModel, argument, allowUncertainCandidates: false, allowParams: false, cancellationToken);
}
