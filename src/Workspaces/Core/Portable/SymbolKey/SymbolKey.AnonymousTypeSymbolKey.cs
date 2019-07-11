// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;

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
                using var propertyTypes = PooledArrayBuilder<ITypeSymbol>.GetInstance();
                using var propertyNames = PooledArrayBuilder<string>.GetInstance();
                using var propertyIsReadOnly = PooledArrayBuilder<bool>.GetInstance();
                using var propertyLocations = PooledArrayBuilder<Location>.GetInstance();

                reader.FillSymbolArray(propertyTypes);
                reader.FillStringArray(propertyNames);
                reader.FillBooleanArray(propertyIsReadOnly);
                reader.FillLocationArray(propertyLocations);

                if (propertyTypes.Count == propertyNames.Count)
                {
                    try
                    {
                        var anonymousType = reader.Compilation.CreateAnonymousTypeSymbol(
                            propertyTypes.ToImmutable(), propertyNames.ToImmutable(),
                            propertyIsReadOnly.ToImmutable(), propertyLocations.ToImmutable());
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
