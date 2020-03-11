﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis
{
    internal partial struct SymbolKey
    {
        private static class ModuleSymbolKey
        {
            public static void Create(IModuleSymbol symbol, SymbolKeyWriter visitor)
                => visitor.WriteSymbolKey(symbol.ContainingSymbol);

            public static SymbolKeyResolution Resolve(SymbolKeyReader reader)
            {
                var containingSymbolResolution = reader.ReadSymbolKey();

                using var result = PooledArrayBuilder<IModuleSymbol>.GetInstance();
                foreach (var assembly in containingSymbolResolution.OfType<IAssemblySymbol>())
                {
                    // Don't check ModuleIds for equality because in practice, no-one uses them,
                    // and there is no way to set netmodule name programmatically using Roslyn
                    result.AddValuesIfNotNull(assembly.Modules);
                }

                return CreateResolution(result);
            }
        }
    }
}
