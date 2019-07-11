// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis
{
    internal partial struct SymbolKey
    {
        private static class EventSymbolKey
        {
            public static void Create(IEventSymbol symbol, SymbolKeyWriter visitor)
            {
                visitor.WriteString(symbol.MetadataName);
                visitor.WriteSymbolKey(symbol.ContainingType);
            }

            public static SymbolKeyResolution Resolve(SymbolKeyReader reader)
            {
                var metadataName = reader.ReadString();
                var containingTypeResolution = reader.ReadSymbolKey();

                using var result = PooledArrayBuilder<IEventSymbol>.GetInstance();
                foreach (var containingSymbol in containingTypeResolution)
                {
                    if (containingSymbol is INamedTypeSymbol containingType)
                    {
                        foreach (var member in containingType.GetMembers(metadataName))
                        {
                            if (member is IEventSymbol ev)
                            {
                                result.AddIfNotNull(ev);
                            }
                        }
                    }
                }

                return CreateSymbolInfo(result);
            }
        }
    }
}
