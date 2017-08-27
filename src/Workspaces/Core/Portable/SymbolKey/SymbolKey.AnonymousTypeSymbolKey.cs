// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.CodeAnalysis
{
    internal partial struct SymbolKey
    {
        private static class AnonymousTypeSymbolKey
        {
            public static void Create(INamedTypeSymbol symbol, SymbolKeyWriter visitor)
            {
                Debug.Assert(symbol.IsAnonymousType);

                var properties = symbol.GetMembers().OfType<IPropertySymbol>().ToImmutableArray();
                var propertyTypes = properties.SelectAsArray(p => p.Type);
                var propertyNames = properties.SelectAsArray(p => p.Name);
                var propertyIsReadOnly = properties.SelectAsArray(p => p.SetMethod == null);
                var propertyLocations = properties.SelectAsArray(p => p.Locations.FirstOrDefault());

                visitor.WriteSymbolKeyArray(propertyTypes);
                visitor.WriteStringArray(propertyNames);
                visitor.WriteBooleanArray(propertyIsReadOnly);
                visitor.WriteLocationArray(propertyLocations);
            }

            public static SymbolKeyResolution Resolve(SymbolKeyReader reader)
            {
                var propertyTypeSymbols = reader.ReadSymbolKeyArray();
                var propertyTypes = propertyTypeSymbols.Select(r => GetFirstSymbol<ITypeSymbol>(r)).ToImmutableArray();
                var propertyNames = reader.ReadStringArray();
                var propertyIsReadOnly = reader.ReadBooleanArray();
                var propertyLocations = reader.ReadLocationArray();

                if (propertyTypes.Length == propertyNames.Length)
                {
                    try
                    {
                        var anonymousType = reader.Compilation.CreateAnonymousTypeSymbol(
                            propertyTypes, propertyNames, propertyIsReadOnly, propertyLocations);
                        return new SymbolKeyResolution(anonymousType);
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
