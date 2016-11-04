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

                var frieldlyNames = ArrayBuilder<String>.GetInstance();
                var locations = ArrayBuilder<Location>.GetInstance();

                foreach(var element in symbol.TupleElements)
                {
                    frieldlyNames.Add(element.IsImplicitlyDeclared? null: element.Name);
                    locations.Add(element.Locations.FirstOrDefault());
                }

                visitor.WriteStringArray(frieldlyNames.ToImmutableAndFree());
                visitor.WriteLocationArray(locations.ToImmutableAndFree());
            }

            public static SymbolKeyResolution Resolve(SymbolKeyReader reader)
            {
                var underlyingTypeResolution = reader.ReadSymbolKey();
                var elementNames = reader.ReadStringArray();
                var elementLocations = reader.ReadLocationArray();

                try
                {
                    var result = GetAllSymbols<INamedTypeSymbol>(underlyingTypeResolution).Select(
                        t => reader.Compilation.CreateTupleTypeSymbol(t, elementNames, elementLocations));
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