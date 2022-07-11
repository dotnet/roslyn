// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

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
