// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.CodeAnalysis.Symbols
{
    internal partial struct SymbolKey
    {
        private static class AnonymousTypeSymbolKey
        {
            public static void Create(INamedTypeSymbol symbol, SymbolKeyWriter writer)
            {
                Debug.Assert(symbol.IsAnonymousType);

                var properties = symbol.GetMembers().OfType<IPropertySymbol>().ToImmutableArray();
                var propertyTypes = properties.SelectAsArray(p => p.Type);
                var propertyNames = properties.SelectAsArray(p => p.Name);
                var propertyIsReadOnly = properties.SelectAsArray(p => p.SetMethod == null);
                var propertyLocations = properties.SelectAsArray(p => p.Locations.FirstOrDefault());

                writer.WriteSymbolKeyArray(propertyTypes);
                writer.WriteStringArray(propertyNames);
                writer.WriteBooleanArray(propertyIsReadOnly);
                writer.WriteLocationArray(propertyLocations);
            }

            public static ResolvedSymbolInfo Resolve(SymbolKeyReader reader)
            {
                var resolvedPropertyTypes = reader.ReadSymbolKeyArray();
                var propertyTypes = resolvedPropertyTypes.SelectAsArray(r => r.GetFirstSymbol<ITypeSymbol>());
                var propertyNames = reader.ReadStringArray();
                var propertyIsReadOnly = reader.ReadBooleanArray();
                var propertyLocations = reader.ReadLocationArray();

                if (propertyTypes.Length == propertyNames.Length)
                {
                    try
                    {
                        var anonymousType = reader.Compilation.CreateAnonymousTypeSymbol(
                            propertyTypes, propertyNames, propertyIsReadOnly, propertyLocations);

                        return new ResolvedSymbolInfo(anonymousType);
                    }
                    catch (ArgumentException)
                    {
                    }
                }

                // TODO(dustinca): Is object the best thing to return here? It seems reasonable for type inferrers,
                // but I would expect symbol resolution to return a default SymbolKeyResolution if it couldn't be found.
                // At the very least, it should include make this a candidate symbol and set a reason.
                return new ResolvedSymbolInfo(reader.Compilation.ObjectType);
            }
        }
    }
}
