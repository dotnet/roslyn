// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;

namespace Microsoft.CodeAnalysis
{
    internal static class SymbolKeyExtensions
    {
        public static SymbolKey GetSymbolKey(this ISymbol symbol, CancellationToken cancellationToken = default)
            => SymbolKey.Create(symbol, cancellationToken);
    }
}
