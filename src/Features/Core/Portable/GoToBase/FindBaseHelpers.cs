// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols.FindReferences;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.GoToBase
{
    internal static class FindBaseHelpers
    {
        public static ValueTask<ImmutableArray<ISymbol>> FindBasesAsync(
            ISymbol symbol, Solution solution, CancellationToken cancellationToken)
        {
            if (symbol is INamedTypeSymbol
                {
                    TypeKind: TypeKind.Class or TypeKind.Interface or TypeKind.Struct,
                } namedTypeSymbol)
            {
                var result = BaseTypeFinder.FindBaseTypesAndInterfaces(namedTypeSymbol).CastArray<ISymbol>();
                return ValueTaskFactory.FromResult(result);
            }

            if (symbol.Kind is SymbolKind.Property or
                SymbolKind.Method or
                SymbolKind.Event)
            {
                return BaseTypeFinder.FindOverriddenAndImplementedMembersAsync(symbol, solution, cancellationToken);
            }

            return ValueTaskFactory.FromResult(ImmutableArray<ISymbol>.Empty);
        }
    }
}
