// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis;

internal partial struct SymbolKey
{
    private sealed class ModuleSymbolKey : AbstractSymbolKey<IModuleSymbol>
    {
        public static readonly ModuleSymbolKey Instance = new();

        public sealed override void Create(IModuleSymbol symbol, SymbolKeyWriter visitor)
            => visitor.WriteSymbolKey(symbol.ContainingSymbol);

        protected sealed override SymbolKeyResolution Resolve(
            SymbolKeyReader reader, IModuleSymbol? contextualSymbol, out string? failureReason)
        {
            var containingSymbolResolution = reader.ReadSymbolKey(contextualSymbol?.ContainingSymbol, out var containingSymbolFailureReason);

            if (containingSymbolFailureReason != null)
            {
                failureReason = $"({nameof(ModuleSymbolKey)} {nameof(containingSymbolResolution)} failed -> {containingSymbolFailureReason})";
                return default;
            }

            using var result = PooledArrayBuilder<IModuleSymbol>.GetInstance(containingSymbolResolution.SymbolCount);
            foreach (var symbol in containingSymbolResolution)
            {
                if (symbol is not IAssemblySymbol assembly)
                    continue;

                // Don't check ModuleIds for equality because in practice, no-one uses them,
                // and there is no way to set netmodule name programmatically using Roslyn
                var assemblyModules = assembly.Modules;
                if (assemblyModules is ImmutableArray<IModuleSymbol> modules)
                {
                    // Avoid allocations if possible
                    // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/2136177
                    result.AddValuesIfNotNull(modules);
                }
                else
                {
                    // Visual Basic implementation of IAssemblySymbol.Modules relies on covariance of IEnumerable<T>, so
                    // the preceding concrete type check will fail.
                    result.AddValuesIfNotNull(assemblyModules);
                }
            }

            return CreateResolution(result, $"({nameof(ModuleSymbolKey)} failed)", out failureReason);
        }
    }
}
