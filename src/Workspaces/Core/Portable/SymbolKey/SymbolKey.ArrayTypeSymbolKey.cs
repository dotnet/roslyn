// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Symbols
{
    internal partial struct SymbolKey
    {
        private static class ArrayTypeSymbolKey
        {
            public static void Create(IArrayTypeSymbol symbol, SymbolKeyWriter writer)
            {
                writer.WriteSymbolKey(symbol.ElementType);
                writer.WriteInteger(symbol.Rank);
            }

            public static SymbolKeyResolution Resolve(SymbolKeyReader reader)
            {
                var resolvedElementType = reader.ReadSymbolKey();
                var rank = reader.ReadInteger();

                var symbols = resolvedElementType
                    .GetAllSymbols<ITypeSymbol>()
                    .SelectAsArray(s => reader.Compilation.CreateArrayTypeSymbol(s, rank));

                return SymbolKeyResolution.Create(symbols);
            }
        }
    }
}
