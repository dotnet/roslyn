// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Microsoft.CodeAnalysis
{
    internal partial struct SymbolKey
    {
        private static class PointerTypeSymbolKey
        {
            public static void Create(IPointerTypeSymbol symbol, SymbolKeyWriter visitor)
                => visitor.WriteSymbolKey(symbol.PointedAtType);

            public static SymbolKeyResolution Resolve(SymbolKeyReader reader, out string? failureReason)
            {
                var pointedAtTypeResolution = reader.ReadSymbolKey(out var pointedAtTypeFailureReason);

                if (pointedAtTypeFailureReason != null)
                {
                    failureReason = $"({nameof(PointerTypeSymbolKey)} {nameof(pointedAtTypeResolution)} failed -> {pointedAtTypeFailureReason})";
                    return default;
                }

                using var result = PooledArrayBuilder<IPointerTypeSymbol>.GetInstance(pointedAtTypeResolution.SymbolCount);
                foreach (var typeSymbol in pointedAtTypeResolution.OfType<ITypeSymbol>())
                {
                    result.AddIfNotNull(reader.Compilation.CreatePointerTypeSymbol(typeSymbol));
                }

                return CreateResolution(result, $"({nameof(PointerTypeSymbolKey)} could not resolve)", out failureReason);
            }
        }
    }
}
