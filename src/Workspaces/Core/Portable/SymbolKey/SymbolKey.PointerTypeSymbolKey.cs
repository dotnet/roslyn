// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis
{
    internal partial struct SymbolKey
    {
        private static class PointerTypeSymbolKey
        {
            public static void Create(IPointerTypeSymbol symbol, SymbolKeyWriter visitor)
                => visitor.WriteSymbolKey(symbol.PointedAtType);

            public static SymbolKeyResolution Resolve(SymbolKeyReader reader)
            {
                var pointedAtTypeResolution = reader.ReadSymbolKey();

                using var result = PooledArrayBuilder<IPointerTypeSymbol>.GetInstance(pointedAtTypeResolution.SymbolCount);
                foreach (var typeSymbol in pointedAtTypeResolution.OfType<ITypeSymbol>())
                {
                    result.AddIfNotNull(reader.Compilation.CreatePointerTypeSymbol(typeSymbol));
                }

                return CreateResolution(result);
            }
        }
    }
}
