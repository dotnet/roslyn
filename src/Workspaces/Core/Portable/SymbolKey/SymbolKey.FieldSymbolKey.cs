// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Symbols
{
    internal partial struct SymbolKey
    {
        private static class FieldSymbolKey
        {
            public static void Create(IFieldSymbol symbol, SymbolKeyWriter writer)
            {
                writer.WriteString(symbol.MetadataName);
                writer.WriteSymbolKey(symbol.ContainingType);
            }

            public static ResolvedSymbolInfo Resolve(SymbolKeyReader reader)
            {
                var metadataName = reader.ReadString();
                var resolvedContainingType = reader.ReadSymbolKey();

                var fields = reader.GetMembersWithName<IFieldSymbol>(resolvedContainingType, metadataName);

                return ResolvedSymbolInfo.Create(fields);
            }
        }
    }
}
