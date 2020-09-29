// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Microsoft.CodeAnalysis
{
    internal partial struct SymbolKey
    {
        private static class ArrayTypeSymbolKey
        {
            public static void Create(IArrayTypeSymbol symbol, SymbolKeyWriter visitor)
            {
                visitor.WriteSymbolKey(symbol.ElementType);
                visitor.WriteInteger(symbol.Rank);
            }

            public static SymbolKeyResolution Resolve(SymbolKeyReader reader, out string? failureReason)
            {
                var elementTypeResolution = reader.ReadSymbolKey(out var elementTypeFailureReason);
                var rank = reader.ReadInteger();

                if (elementTypeFailureReason != null)
                {
                    failureReason = $"({nameof(ArrayTypeSymbolKey)} {nameof(elementTypeResolution)} failed -> {elementTypeFailureReason})";
                    return default;
                }

                using var result = PooledArrayBuilder<IArrayTypeSymbol>.GetInstance(elementTypeResolution.SymbolCount);
                foreach (var typeSymbol in elementTypeResolution.OfType<ITypeSymbol>())
                    result.AddIfNotNull(reader.Compilation.CreateArrayTypeSymbol(typeSymbol, rank));

                return CreateResolution(result, $"({nameof(ArrayTypeSymbolKey)})", out failureReason);
            }
        }
    }
}
