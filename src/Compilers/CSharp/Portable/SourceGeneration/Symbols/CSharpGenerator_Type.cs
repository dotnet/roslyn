// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.CodeAnalysis.CSharp.SourceGeneration
{
    internal partial class CSharpGenerator
    {
        private TypeSyntax GenerateTypeSyntax(ITypeSymbol symbol, bool onlyNames)
        {
            var typeSyntax = GenerateTypeSyntaxWithoutNullable(symbol, onlyNames);
            if (onlyNames)
                return typeSyntax;

            return symbol.NullableAnnotation != CodeAnalysis.NullableAnnotation.Annotated
                ? typeSyntax
                : NullableType(typeSyntax);
        }

        private static TypeSyntax GenerateTypeSyntaxWithoutNullable(ITypeSymbol symbol, bool onlyNames)
            => symbol.Kind switch
            {
                SymbolKind.NamedType => GenerateNamedTypeSyntaxWithoutNullable((INamedTypeSymbol)symbol, onlyNames),
                SymbolKind.ArrayType => GenerateArrayTypeSyntaxWithoutNullable((IArrayTypeSymbol)symbol, onlyNames),
                SymbolKind.DynamicType => GenerateDynamicTypeSyntaxWithoutNullable((IDynamicTypeSymbol)symbol),
                SymbolKind.PointerType => GeneratePointerTypeSyntaxWithoutNullable((IPointerTypeSymbol)symbol, onlyNames),
                SymbolKind.TypeParameter => GenerateTypeParameterTypeSyntaxWithoutNullable((ITypeParameterSymbol)symbol),
                _ => throw ExceptionUtilities.UnexpectedValue(symbol.Kind),
            };
    }
}
