// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal readonly struct TokenSemanticInfo(
    ISymbol declaredSymbol,
    IAliasSymbol aliasSymbol,
    ImmutableArray<ISymbol> referencedSymbols,
    ITypeSymbol type,
    ITypeSymbol convertedType,
    TextSpan span)
{
    public static readonly TokenSemanticInfo Empty = new(
        null, null, [], null, null, default);

    public readonly ISymbol DeclaredSymbol = declaredSymbol;
    public readonly IAliasSymbol AliasSymbol = aliasSymbol;
    public readonly ImmutableArray<ISymbol> ReferencedSymbols = referencedSymbols;
    public readonly ITypeSymbol Type = type;
    public readonly ITypeSymbol ConvertedType = convertedType;
    public readonly TextSpan Span = span;

    public ImmutableArray<ISymbol> GetSymbols(bool includeType)
    {
        var result = ArrayBuilder<ISymbol>.GetInstance();
        result.AddIfNotNull(DeclaredSymbol);
        result.AddIfNotNull(AliasSymbol);
        result.AddRange(ReferencedSymbols);

        if (includeType)
        {
            result.AddIfNotNull(Type ?? ConvertedType);
        }

        return result.ToImmutableAndFree();
    }

    public ISymbol GetAnySymbol(bool includeType)
        => GetSymbols(includeType).FirstOrDefault();
}
