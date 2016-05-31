﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal partial class SymbolKey
    {
        private class PointerTypeSymbolKey
        {
            public static void Create(IPointerTypeSymbol symbol, Visitor visitor)
            {
                visitor.WriteSymbolKey(symbol.PointedAtType);
            }

            public static SymbolKeyResolution Resolve(SymbolKeyReader reader)
            {
                var pointedAtTypeResolution = reader.ReadSymbolKey();

                return CreateSymbolInfo(GetAllSymbols<ITypeSymbol>(pointedAtTypeResolution)
                    .Select(reader.Compilation.CreatePointerTypeSymbol));
            }
        }
    }
}