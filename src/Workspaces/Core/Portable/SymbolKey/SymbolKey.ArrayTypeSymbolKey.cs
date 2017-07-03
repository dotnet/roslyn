// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Roslyn.Utilities;

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

                return CreateSymbolInfo(GetAllSymbols<ITypeSymbol>(elementTypeResolution)
                            .Select(s => reader.Compilation.CreateArrayTypeSymbol(s, rank)));
            }
        }
    }
}
