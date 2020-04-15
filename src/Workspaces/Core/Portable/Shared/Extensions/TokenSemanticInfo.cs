// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal struct TokenSemanticInfo
    {
        public static readonly TokenSemanticInfo Empty = new TokenSemanticInfo(
            null, null, ImmutableArray<ISymbol>.Empty, null, null, default);

        public readonly ISymbol DeclaredSymbol;
        public readonly IAliasSymbol AliasSymbol;
        public readonly ImmutableArray<ISymbol> ReferencedSymbols;
        public readonly ITypeSymbol Type;
        public readonly ITypeSymbol ConvertedType;
        public readonly TextSpan Span;

        public TokenSemanticInfo(
            ISymbol declaredSymbol,
            IAliasSymbol aliasSymbol,
            ImmutableArray<ISymbol> referencedSymbols,
            ITypeSymbol type,
            ITypeSymbol convertedType,
            TextSpan span)
        {
            DeclaredSymbol = declaredSymbol;
            AliasSymbol = aliasSymbol;
            ReferencedSymbols = referencedSymbols;
            Type = type;
            ConvertedType = convertedType;
            Span = span;
        }

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
}
