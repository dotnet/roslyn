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


                var friendlyNames = ArrayBuilder<string>.GetInstance();
                var locations = ArrayBuilder<Location>.GetInstance();

                var isError = symbol.TupleUnderlyingType.TypeKind == TypeKind.Error;
                visitor.WriteBoolean(isError);

                if (isError)
                {
                    var elementTypes = ArrayBuilder<ISymbol>.GetInstance();

                    foreach (var element in symbol.TupleElements)
                    {
                        elementTypes.Add(element.Type);
                    }

                    visitor.WriteSymbolKeyArray(elementTypes.ToImmutableAndFree());
                }
                else
                {
                    visitor.WriteSymbolKey(symbol.TupleUnderlyingType);
                }

                foreach (var element in symbol.TupleElements)
                {
                    friendlyNames.Add(element.IsImplicitlyDeclared ? null : element.Name);
                    locations.Add(element.Locations.FirstOrDefault());
                }

                visitor.WriteStringArray(friendlyNames.ToImmutableAndFree());
                visitor.WriteLocationArray(locations.ToImmutableAndFree());
            }

            public static SymbolKeyResolution Resolve(SymbolKeyReader reader)
            {
                var isError = reader.ReadBoolean();
                if (isError)
                {
                    var elementTypes = reader.ReadSymbolKeyArray().SelectAsArray(r => r.GetAnySymbol() as ITypeSymbol);
                    var elementNames = reader.ReadStringArray();
                    var elementLocations = reader.ReadLocationArray();

                    if (!elementTypes.Any(t => t == null))
                    {
                        try
                        {
                            var result = reader.Compilation.CreateTupleTypeSymbol(
                                elementTypes, elementNames, elementLocations);
                            return new SymbolKeyResolution(result);
                        }
                        catch (ArgumentException)
                        {
                        }
                    }
                }
                else
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
                    }
                }

                return new SymbolKeyResolution(reader.Compilation.ObjectType);
            }
        }
    }
}