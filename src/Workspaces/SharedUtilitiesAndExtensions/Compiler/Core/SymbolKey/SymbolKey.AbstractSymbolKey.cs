// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis
{
    internal partial struct SymbolKey
    {
        private abstract class AbstractSymbolKey<TSymbol>
            where TSymbol : class, ISymbol
        {
            public abstract void Create(TSymbol symbol, SymbolKeyWriter writer);

            public SymbolKeyResolution Resolve(SymbolKeyReader reader, out string? failureReason)
                => Resolve(reader, reader.CurrentContextualSymbol as TSymbol, out failureReason);

            protected abstract SymbolKeyResolution Resolve(SymbolKeyReader reader, TSymbol? contextualSymbol, out string? failureReason);
        }
    }
}
