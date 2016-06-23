// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.CodeAnalysis
{
    internal partial struct SymbolKey
    {
        private static class TupleTypeSymbolKey
        {
            public static void Create(INamedTypeSymbol symbol, SymbolKeyWriter visitor)
            {
                Debug.Assert(symbol.IsTupleType);
                visitor.WriteSymbolKey(symbol.TupleUnderlyingType);
                visitor.WriteStringArray(symbol.TupleElementNames);
            }

            public static int GetHashCode(GetHashCodeReader reader)
            {
                // The hash of the underlying type is good enough, we don't need to include names.
                var symbolKeyHashCode = reader.ReadSymbolKey();
                var elementNames = reader.ReadStringArray();

                return symbolKeyHashCode;
            }

            public static SymbolKeyResolution Resolve(SymbolKeyReader reader)
            {
                var underlyingTypeResolution = reader.ReadSymbolKey();
                var tupleElementNames = reader.ReadStringArray();

                try
                {
                    var result = GetAllSymbols<INamedTypeSymbol>(underlyingTypeResolution).Select(
                        t => reader.Compilation.CreateTupleTypeSymbol(t, tupleElementNames));
                    return CreateSymbolInfo(result);
                }
                catch (ArgumentException)
                {
                    return new SymbolKeyResolution(reader.Compilation.ObjectType);
                }
            }
        }
    }
}