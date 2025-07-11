// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static class ITypeParameterSymbolExtensions
{
    public static INamedTypeSymbol? GetNamedTypeSymbolConstraint(this ITypeParameterSymbol typeParameter)
        => typeParameter.ConstraintTypes.Select(GetNamedTypeSymbol).WhereNotNull().FirstOrDefault();

    private static INamedTypeSymbol? GetNamedTypeSymbol(ITypeSymbol type)
    {
        return type is INamedTypeSymbol
            ? (INamedTypeSymbol)type
            : type is ITypeParameterSymbol
                ? GetNamedTypeSymbolConstraint((ITypeParameterSymbol)type)
                : null;
    }
}
