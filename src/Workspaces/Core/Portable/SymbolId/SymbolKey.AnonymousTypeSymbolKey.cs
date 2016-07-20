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

                var properties = symbol.GetMembers().OfType<IPropertySymbol>().ToArray();
                var propertyTypes = properties.Select(p => p.Type).ToImmutableArray();
                var propertyNames = properties.Select(p => p.Name).ToImmutableArray();

                visitor.WriteSymbolKeyArray(propertyTypes);
                visitor.WriteStringArray(propertyNames);
            }

            public static int GetHashCode(GetHashCodeReader reader)
            {
                // The hash of the underlying type is good enough, we don't need to include names.
                var symbolKeyHashCode = reader.ReadSymbolKeyArrayHashCode();
                var elementNames = reader.ReadStringArray();

                return symbolKeyHashCode;
            }

            public static SymbolKeyResolution Resolve(SymbolKeyReader reader)
            {
                var propertyTypeSymbols = reader.ReadSymbolKeyArray();
                var propertyTypes = propertyTypeSymbols.Select(r => GetFirstSymbol<ITypeSymbol>(r)).ToImmutableArray();
                var propertyNames = reader.ReadStringArray();

                if (propertyTypes.Length == propertyNames.Length)
                {
                    try
                    {
                        var anonymousType = reader.Compilation.CreateAnonymousTypeSymbol(propertyTypes, propertyNames);
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