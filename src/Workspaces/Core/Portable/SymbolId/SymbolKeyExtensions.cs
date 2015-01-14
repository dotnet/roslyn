// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;

namespace Microsoft.CodeAnalysis
{
    internal static class SymbolKeyExtensions
    {
        public static SymbolKey GetSymbolKey(this ISymbol symbol)
        {
            return SymbolKey.Create(symbol, null, CancellationToken.None);
        }

#if false
        internal static SymbolKey GetSymbolKey(this ISymbol symbol, Compilation compilation, CancellationToken cancellationToken)
        {
            return SymbolKey.Create(symbol, compilation, cancellationToken);
        }
#endif
    }
}
