// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.CodeAnalysis.CSharp.SourceGeneration
{
    internal partial class CSharpCodeGenerator
    {
        private static TypeSyntax GenerateTypeSyntax(this ITypeSymbol symbol, bool onlyNames)
        {
            var typeSyntax = GenerateTypeSyntaxWithoutNullable(symbol, onlyNames);
            if (onlyNames)
                return typeSyntax;

            return symbol.NullableAnnotation != CodeAnalysis.NullableAnnotation.Annotated
                ? typeSyntax
                : NullableType(typeSyntax);
        }

        private static TypeSyntax GenerateTypeSyntaxWithoutNullable(this ITypeSymbol symbol, bool onlyNames)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.NamedType:
                    return GenerateNamedTypeSyntaxWithoutNullable((INamedTypeSymbol)symbol, onlyNames);
                case SymbolKind.ArrayType:
                    return GenerateArrayTypeSyntaxWithoutNullable((IArrayTypeSymbol)symbol, onlyNames);
                case SymbolKind.DynamicType:
                    return GenerateDynamicTypeSyntaxWithoutNullable((IDynamicTypeSymbol)symbol);
                case SymbolKind.ErrorType:
                case SymbolKind.PointerType:
                    break;
                case SymbolKind.TypeParameter:
                    return GenerateTypeParamterTypeSyntaxWithoutNullable((ITypeParameterSymbol)symbol);
                default:
                    break;
            }

            throw ExceptionUtilities.UnexpectedValue(symbol.Kind);
        }
    }
}
