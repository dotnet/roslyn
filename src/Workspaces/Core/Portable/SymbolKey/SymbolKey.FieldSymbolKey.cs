// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal partial struct SymbolKey
    {
        private static class FieldSymbolKey
        {
            public static void Create(IFieldSymbol symbol, SymbolKeyWriter visitor)
            {
                visitor.WriteString(symbol.MetadataName);
                visitor.WriteSymbolKey(symbol.ContainingType);
            }

            public static int GetHashCode(GetHashCodeReader reader)
            {
                return Hash.Combine(reader.ReadString(), reader.ReadSymbolKey());
            }

            public static SymbolKeyResolution Resolve(SymbolKeyReader reader)
            {
                var metadataName = reader.ReadString();
                var containingTypeResolution = reader.ReadSymbolKey();

                var fields = GetAllSymbols<INamedTypeSymbol>(containingTypeResolution)
                    .SelectMany(t => t.GetMembers(metadataName)).OfType<IFieldSymbol>();
                return CreateSymbolInfo(fields);
            }
        }
    }
}