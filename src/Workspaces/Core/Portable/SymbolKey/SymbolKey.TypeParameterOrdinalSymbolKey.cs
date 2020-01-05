// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal partial struct SymbolKey
    {
        private static class TypeParameterOrdinalSymbolKey
        {
            public static void Create(ITypeParameterSymbol symbol, int methodIndex, SymbolKeyWriter visitor)
            {
                Contract.ThrowIfFalse(symbol.TypeParameterKind == TypeParameterKind.Method);
                visitor.WriteInteger(methodIndex);
                visitor.WriteInteger(symbol.Ordinal);
            }

            public static SymbolKeyResolution Resolve(SymbolKeyReader reader)
            {
                var methodIndex = reader.ReadInteger();
                var ordinal = reader.ReadInteger();
                var method = reader.ResolveMethod(methodIndex);
                var typeParameter = method?.TypeParameters[ordinal];
                return typeParameter == null
                    ? default
                    : new SymbolKeyResolution(typeParameter);
            }
        }
    }
}
