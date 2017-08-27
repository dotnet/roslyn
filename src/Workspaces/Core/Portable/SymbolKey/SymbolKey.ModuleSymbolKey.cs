// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;

namespace Microsoft.CodeAnalysis
{
    internal partial struct SymbolKey
    {
        private static class ModuleSymbolKey
        {
            public static void Create(IModuleSymbol symbol, SymbolKeyWriter visitor)
            {
                visitor.WriteSymbolKey(symbol.ContainingSymbol);
            }

            public static SymbolKeyResolution Resolve(SymbolKeyReader reader)
            {
                var containingSymbolResolution = reader.ReadSymbolKey();

                // Don't check ModuleIds for equality because in practice, no-one uses them,
                // and there is no way to set netmodule name programmatically using Roslyn
                var modules = GetAllSymbols<IAssemblySymbol>(containingSymbolResolution)
                    .SelectMany(a => a.Modules);

                return CreateSymbolInfo(modules);
            }
        }
    }
}
