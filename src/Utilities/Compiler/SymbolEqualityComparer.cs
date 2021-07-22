// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis
{
#if !CODEANALYSIS_V3_OR_BETTER
    internal class SymbolEqualityComparer
    {
        private SymbolEqualityComparer()
        {

        }

#pragma warning disable CA1822 // Mark members as static
        public bool Equals(ISymbol? symbol1, ISymbol? symbol2) =>
#pragma warning restore CA1822 // Mark members as static
            object.Equals(symbol1, symbol2);

        public static SymbolEqualityComparer Default { get; } = new();
    }
#endif
}
