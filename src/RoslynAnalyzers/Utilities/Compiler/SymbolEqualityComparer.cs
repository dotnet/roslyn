// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if !CODEANALYSIS_V3_OR_BETTER
namespace Microsoft.CodeAnalysis
{
    using System.Collections.Generic;

    internal sealed class SymbolEqualityComparer : IEqualityComparer<ISymbol?>
    {
        private SymbolEqualityComparer()
        {
        }

        public bool Equals(ISymbol? x, ISymbol? y)
            => x is null
                ? y is null
                : x.Equals(y);

        public int GetHashCode(ISymbol? obj)
            => obj?.GetHashCode() ?? 0;

        public static SymbolEqualityComparer Default { get; } = new();
    }
}
#endif
