// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CodeGeneration;

internal static class INamedTypeSymbolExtensions
{
    public static CodeGenerationAbstractNamedTypeSymbol ToCodeGenerationSymbol(this INamedTypeSymbol namedType)
    {
        if (namedType is CodeGenerationAbstractNamedTypeSymbol typeSymbol)
        {
            return typeSymbol;
        }

        return (CodeGenerationAbstractNamedTypeSymbol)CodeGenerationSymbolMappingFactory.Instance.CreateNamedTypeSymbol(
            namedType.ContainingAssembly,
            namedType.ContainingType,
            namedType.GetAttributes(),
            namedType.DeclaredAccessibility,
            namedType.GetSymbolModifiers(),
            namedType.IsRecord,
            namedType.TypeKind,
            namedType.Name,
            namedType.TypeParameters,
            namedType.BaseType,
            namedType.Interfaces,
            namedType.SpecialType,
            namedType.NullableAnnotation,
            namedType.GetMembers().WhereAsArray(s => s is not INamedTypeSymbol),
            namedType.GetTypeMembers().SelectAsArray(t => t.ToCodeGenerationSymbol()),
            namedType.EnumUnderlyingType);
    }
}
