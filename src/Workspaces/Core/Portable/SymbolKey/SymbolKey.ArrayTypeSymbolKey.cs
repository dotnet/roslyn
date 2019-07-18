// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

            public static SymbolKeyResolution Resolve(SymbolKeyReader reader)
            {
                var elementTypeResolution = reader.ReadSymbolKey();
                var rank = reader.ReadInteger();

                using var result = PooledArrayBuilder<IArrayTypeSymbol>.GetInstance(elementTypeResolution.SymbolCount);
                foreach (var typeSymbol in elementTypeResolution.OfType<ITypeSymbol>())
                {
                    result.AddIfNotNull(reader.Compilation.CreateArrayTypeSymbol(typeSymbol, rank));
                }

                return CreateResolution(result);
            }
        }
    }
}
