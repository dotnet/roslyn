﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis
{
    internal partial struct SymbolKey
    {
        private static class AssemblySymbolKey
        {
            public static void Create(IAssemblySymbol symbol, SymbolKeyWriter visitor)
            {
                // If the format of this ever changed, then it's necessary to fixup the
                // SymbolKeyComparer.RemoveAssemblyKeys function.
                visitor.WriteString(symbol.Identity.Name);
            }

            public static SymbolKeyResolution Resolve(SymbolKeyReader reader)
            {
                var assemblyName = reader.ReadString();
                var compilation = reader.Compilation;
                var ignoreAssemblyKey = reader.IgnoreAssemblyKey;

                using var result = PooledArrayBuilder<IAssemblySymbol>.GetInstance();
                if (ignoreAssemblyKey || compilation.Assembly.Identity.Name == assemblyName)
                {
                    result.AddIfNotNull(compilation.Assembly);
                }

                // Might need keys for symbols from previous script compilations.
                foreach (var assembly in compilation.GetReferencedAssemblySymbols())
                {
                    if (ignoreAssemblyKey || assembly.Identity.Name == assemblyName)
                    {
                        result.AddIfNotNull(assembly);
                    }
                }

                return CreateResolution(result);
            }
        }
    }
}
