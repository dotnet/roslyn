// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal static class INamedTypeSymbolExtensions
    {
        public static CodeGenerationAbstractNamedTypeSymbol ToCodeGenerationSymbol(this INamedTypeSymbol namedType)
        {
            if (namedType is CodeGenerationAbstractNamedTypeSymbol)
            {
                return (CodeGenerationAbstractNamedTypeSymbol)namedType;
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
                namedType.GetMembers().Where(s => !(s is INamedTypeSymbol)).ToList(),
                namedType.GetTypeMembers().Select(t => t.ToCodeGenerationSymbol()).ToList(),
                namedType.EnumUnderlyingType);
        }
    }
}
