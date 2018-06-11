// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Symbols
{
    internal partial struct SymbolKey
    {
        private static class AssemblySymbolKey
        {
            public static void Create(IAssemblySymbol symbol, SymbolKeyWriter writer)
            {
                // If the format of this ever changed, then it's necessary to fixup the
                // SymbolKeyComparer.RemoveAssemblyKeys function.
                writer.WriteString(symbol.Identity.Name);
            }

            public static SymbolKeyResolution Resolve(SymbolKeyReader reader)
            {
                var assemblyName = reader.ReadString();
                var assemblySymbols = GetAssemblySymbols(assemblyName, reader.Compilation, reader.IgnoreAssemblyKey);

                return SymbolKeyResolution.Create(assemblySymbols);
            }

            private static ImmutableArray<IAssemblySymbol> GetAssemblySymbols(
                string assemblyName, Compilation compilation, bool ignoreAssemblyKey)
            {
                var result = ArrayBuilder<IAssemblySymbol>.GetInstance();

                if (ignoreAssemblyKey || compilation.Assembly.Identity.Name == assemblyName)
                {
                    result.Add(compilation.Assembly);
                }

                // Might need keys for symbols from previous script compilations.
                foreach (var assembly in compilation.GetReferencedAssemblySymbols())
                {
                    if (ignoreAssemblyKey || assembly.Identity.Name == assemblyName)
                    {
                        result.Add(assembly);
                    }
                }

                return result.ToImmutableAndFree();
            }
        }
    }
}
