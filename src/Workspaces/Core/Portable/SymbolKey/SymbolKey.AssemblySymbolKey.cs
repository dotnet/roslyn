// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
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

                return CreateSymbolInfo(GetAssemblySymbols(
                    assemblyName, reader.Compilation, reader.IgnoreAssemblyKey));
            }

            private static IEnumerable<IAssemblySymbol> GetAssemblySymbols(
                string assemblyName, Compilation compilation, bool ignoreAssemblyKey)
            {
                if (ignoreAssemblyKey || compilation.Assembly.Identity.Name == assemblyName)
                {
                    yield return compilation.Assembly;
                }

                // Might need keys for symbols from previous script compilations.
                foreach (var assembly in compilation.GetReferencedAssemblySymbols())
                {
                    if (ignoreAssemblyKey || assembly.Identity.Name == assemblyName)
                    {
                        yield return assembly;
                    }
                }
            }
        }
    }
}
