// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
