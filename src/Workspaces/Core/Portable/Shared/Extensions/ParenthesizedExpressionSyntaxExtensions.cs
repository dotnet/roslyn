// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Extensions
{
    internal static class CommonParenthesizedExpressionSyntaxExtensions
    {
        public static bool IsSafeToChangeAssociativity(
            this SyntaxNode parenthesizedExpression, SyntaxNode innerExpression,
            SyntaxNode parentBinaryLeft, SyntaxNode parentBinaryRight, SemanticModel semanticModel)
        {
            // Now we'll perform a few semantic checks to determine whether removal 
            // of the parentheses might break semantics. Note that we'll try and be 
            // fairly conservative with these. For example, we'll assume that failing 
            // any of these checks results in the parentheses being declared as necessary 
            // -- even if they could be removed depending on whether the parenthesized
            // expression appears on the left or right side of the parent binary expression.

            // First, does the binary expression result in an operator overload being 
            // called?
            var symbolInfo = semanticModel.GetSymbolInfo(innerExpression);
            if (AnySymbolIsUserDefinedOperator(symbolInfo))
            {
                return false;
            }

            // Second, check the type and converted type of the binary expression.
            // Are they the same?
            var innerTypeInfo = semanticModel.GetTypeInfo(innerExpression);
            if (innerTypeInfo.Type != null && innerTypeInfo.ConvertedType != null)
            {
                if (!innerTypeInfo.Type.Equals(innerTypeInfo.ConvertedType))
                {
                    return false;
                }
            }

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

            // Floating point is not safe to change associativity of.  For example,
            // if the user has "large * (large * small)" then this will become 
            // "(large * large) * small.  And that could easily overflow to Inf (and 
            // other badness).
            var parentBinary = parenthesizedExpression.Parent;
            var outerTypeInfo = semanticModel.GetTypeInfo(parentBinary);
            if (IsFloatingPoint(innerTypeInfo) || IsFloatingPoint(outerTypeInfo))
            {
                return false;
            }

            return true;
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
            => type?.SpecialType == SpecialType.System_Single || type?.SpecialType == SpecialType.System_Double;
    }
}
