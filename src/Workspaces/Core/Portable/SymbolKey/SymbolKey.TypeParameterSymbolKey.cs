﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis
{
    internal partial struct SymbolKey
    {
        private static class TypeParameterSymbolKey
        {
            public static void Create(ITypeParameterSymbol symbol, SymbolKeyWriter visitor)
            {
                visitor.WriteString(symbol.MetadataName);
                visitor.WriteSymbolKey(symbol.ContainingSymbol);
            }

            public static SymbolKeyResolution Resolve(SymbolKeyReader reader)
            {
                var metadataName = reader.ReadString();
                var containingSymbolResolution = reader.ReadSymbolKey();

                using var result = PooledArrayBuilder<ITypeParameterSymbol>.GetInstance();
                foreach (var containingSymbol in containingSymbolResolution)
                {
                    foreach (var typeParam in containingSymbol.GetTypeParameters())
                    {
                        if (typeParam.MetadataName == metadataName)
                        {
                            result.AddIfNotNull(typeParam);
                        }
                    }
                }

                return CreateResolution(result);
            }
        }
    }
}
