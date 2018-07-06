// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Symbols
{
    internal partial struct SymbolKey
    {
        private static class TupleTypeSymbolKey
        {
            public static void Create(INamedTypeSymbol symbol, SymbolKeyWriter writer)
            {
                Debug.Assert(symbol.IsTupleType);

                var friendlyNames = ArrayBuilder<string>.GetInstance();
                var locations = ArrayBuilder<Location>.GetInstance();

                var isError = symbol.TupleUnderlyingType.TypeKind == TypeKind.Error;
                writer.WriteBoolean(isError);

                if (isError)
                {
                    var elementTypes = ArrayBuilder<ISymbol>.GetInstance();

                    foreach (var element in symbol.TupleElements)
                    {
                        elementTypes.Add(element.Type);
                    }

                    writer.WriteSymbolKeyArray(elementTypes.ToImmutableAndFree());
                }
                else
                {
                    writer.WriteSymbolKey(symbol.TupleUnderlyingType);
                }

                foreach (var element in symbol.TupleElements)
                {
                    friendlyNames.Add(element.IsImplicitlyDeclared ? null : element.Name);
                    locations.Add(element.Locations.FirstOrDefault() ?? Location.None);
                }

                writer.WriteStringArray(friendlyNames.ToImmutableAndFree());
                writer.WriteLocationArray(locations.ToImmutableAndFree());
            }

            public static ResolvedSymbolInfo Resolve(SymbolKeyReader reader)
            {
                var isError = reader.ReadBoolean();
                if (isError)
                {
                    var elementTypes = reader.ReadSymbolKeyArray().SelectAsArray(r => r.GetAnySymbol() as ITypeSymbol);
                    var elementNames = reader.ReadStringArray();
                    var elementLocations = ReadElementLocations(reader);

                    if (!elementTypes.Any(t => t == null))
                    {
                        try
                        {
                            var tupleTypeSymbol = reader.Compilation.CreateTupleTypeSymbol(
                                elementTypes, elementNames, elementLocations);

                            return new ResolvedSymbolInfo(tupleTypeSymbol);
                        }
                        catch (ArgumentException)
                        {
                        }
                    }
                }
                else
                {
                    var resolvedUnderlyingType = reader.ReadSymbolKey();
                    var elementNames = reader.ReadStringArray();
                    var elementLocations = ReadElementLocations(reader);

                    try
                    {
                        var result = ArrayBuilder<INamedTypeSymbol>.GetInstance();

                        foreach (var underlyingType in resolvedUnderlyingType.GetAllSymbols<INamedTypeSymbol>())
                        {
                            var tupleTypeSymbol = reader.Compilation.CreateTupleTypeSymbol(underlyingType, elementNames, elementLocations);
                            result.Add(tupleTypeSymbol);
                        }

                        return ResolvedSymbolInfo.Create(result.ToImmutableAndFree());
                    }
                    catch (ArgumentException)
                    {
                    }
                }

                return new ResolvedSymbolInfo(reader.Compilation.ObjectType);
            }

            private static ImmutableArray<Location> ReadElementLocations(SymbolKeyReader reader)
            {
                // Compiler API requires that all the locations are non-null, or that there is a default
                // immutable array passed in.
                var elementLocations = reader.ReadLocationArray();
                if (elementLocations.All(loc => loc == null))
                {
                    elementLocations = default;
                }

                return elementLocations;
            }
        }
    }
}
