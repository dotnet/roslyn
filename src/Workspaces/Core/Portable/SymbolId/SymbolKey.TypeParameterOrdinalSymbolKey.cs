﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    internal partial class SymbolKey
    {
        private static class TypeParameterOrdinalSymbolKey
        {
            public static void Create(ITypeParameterSymbol symbol, Visitor visitor)
            {
                Debug.Assert(visitor.WritingSignature);
                Debug.Assert(symbol.TypeParameterKind == TypeParameterKind.Method);
                visitor.WriteInteger(symbol.Ordinal);
            }

            public static SymbolKeyResolution Resolve(SymbolKeyReader reader)
            {
                var ordinal = reader.ReadInteger();
                var typeParameter = reader.CurrentMethod.TypeParameters[ordinal];
                return new SymbolKeyResolution(typeParameter);
            }
        }
    }
}