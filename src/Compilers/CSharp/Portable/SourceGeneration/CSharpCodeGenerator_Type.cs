// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.SourceGeneration;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.CodeAnalysis.CSharp.SourceGeneration
{
    internal partial class CSharpCodeGenerator
    {
        private static TypeSyntax GenerateTypeSyntax(this ITypeSymbol symbol)
        {
            var typeSyntax = GenerateTypeSyntaxWithoutNullable(symbol);
            return symbol.NullableAnnotation != CodeAnalysis.NullableAnnotation.Annotated
                ? typeSyntax
                : NullableType(typeSyntax);
        }

        private static TypeSyntax GenerateTypeSyntaxWithoutNullable(this ITypeSymbol symbol)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.NamedType:
                    return GenerateNamedTypeSyntaxWithoutNullable((INamedTypeSymbol)symbol);
                case SymbolKind.ArrayType:
                    return GenerateArrayTypeSyntaxWithoutNullable((IArrayTypeSymbol)symbol);
                case SymbolKind.DynamicType:
                case SymbolKind.ErrorType:
                case SymbolKind.PointerType:
                case SymbolKind.TypeParameter:
                    break;
                default:
                    break;
            }

            throw ExceptionUtilities.UnexpectedValue(symbol.Kind);
        }
    }
}
