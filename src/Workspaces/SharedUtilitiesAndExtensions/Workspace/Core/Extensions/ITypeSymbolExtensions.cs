// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static partial class ITypeSymbolExtensions
{
    public static IList<ITypeParameterSymbol> GetReferencedMethodTypeParameters(
        this ITypeSymbol? type, IList<ITypeParameterSymbol>? result = null)
    {
        result ??= [];
        type?.Accept(new CollectTypeParameterSymbolsVisitor(result, onlyMethodTypeParameters: true));
        return result;
    }

    public static IList<ITypeParameterSymbol> GetReferencedTypeParameters(
        this ITypeSymbol? type, IList<ITypeParameterSymbol>? result = null)
    {
        result ??= [];
        type?.Accept(new CollectTypeParameterSymbolsVisitor(result, onlyMethodTypeParameters: false));
        return result;
    }
}
