// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Symbols
{
    internal partial struct SymbolKey
    {
        private static class EventSymbolKey
        {
            public static void Create(IEventSymbol symbol, SymbolKeyWriter writer)
            {
                writer.WriteString(symbol.MetadataName);
                writer.WriteSymbolKey(symbol.ContainingType);
            }

            public static SymbolKeyResolution Resolve(SymbolKeyReader reader)
            {
                var metadataName = reader.ReadString();
                var resolvedContainingType = reader.ReadSymbolKey();

                var events = GetMembersWithName<IEventSymbol>(resolvedContainingType, metadataName);

                return SymbolKeyResolution.Create(events);
            }
        }
    }
}
