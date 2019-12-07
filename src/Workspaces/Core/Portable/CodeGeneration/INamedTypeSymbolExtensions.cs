// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal static class INamedTypeSymbolExtensions
    {
        public static CodeGenerationAbstractNamedTypeSymbol ToCodeGenerationSymbol(this INamedTypeSymbol namedType)
        {
            if (namedType is CodeGenerationAbstractNamedTypeSymbol typeSymbol)
            {
                return typeSymbol;
            }

            return new CodeGenerationNamedTypeSymbol(
                namedType.ContainingType,
                namedType.GetAttributes(),
                namedType.DeclaredAccessibility,
                namedType.GetSymbolModifiers(),
                namedType.TypeKind,
                namedType.Name,
                namedType.TypeParameters,
                namedType.BaseType,
                namedType.Interfaces,
                namedType.SpecialType,
                namedType.NullableAnnotation,
                namedType.GetMembers().WhereAsArray(s => !(s is INamedTypeSymbol)),
                namedType.GetTypeMembers().SelectAsArray(t => t.ToCodeGenerationSymbol()),
                namedType.EnumUnderlyingType);
        }
    }
}
