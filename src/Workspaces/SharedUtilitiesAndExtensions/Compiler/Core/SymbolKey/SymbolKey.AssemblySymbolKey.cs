// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis;

internal partial struct SymbolKey
{
    private sealed class AssemblySymbolKey : AbstractSymbolKey<IAssemblySymbol>
    {
        public static readonly AssemblySymbolKey Instance = new();

        public sealed override void Create(IAssemblySymbol symbol, SymbolKeyWriter visitor)
        {
            // If the format of this ever changed, then it's necessary to fixup the
            // SymbolKeyComparer.RemoveAssemblyKeys function.
            visitor.WriteString(symbol.Identity.Name);
        }

        protected sealed override SymbolKeyResolution Resolve(
            SymbolKeyReader reader, IAssemblySymbol? contextualSymbol, out string? failureReason)
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

            return CreateResolution(result, $"({nameof(AssemblySymbolKey)} '{assemblyName}' not found)", out failureReason);
        }
    }
}
