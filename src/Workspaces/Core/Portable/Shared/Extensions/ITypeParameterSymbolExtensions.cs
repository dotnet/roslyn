// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class ITypeParameterSymbolExtensions
    {
        public static INamedTypeSymbol? GetNamedTypeSymbolConstraint(this ITypeParameterSymbol typeParameter)
        {
            return typeParameter.ConstraintTypes.Select(GetNamedTypeSymbol).WhereNotNull().FirstOrDefault();
        }

        private static INamedTypeSymbol? GetNamedTypeSymbol(ITypeSymbol type)
        {
            return type is INamedTypeSymbol
                ? (INamedTypeSymbol)type
                : type is ITypeParameterSymbol
                    ? GetNamedTypeSymbolConstraint((ITypeParameterSymbol)type)
                    : null;
        }
    }
}
