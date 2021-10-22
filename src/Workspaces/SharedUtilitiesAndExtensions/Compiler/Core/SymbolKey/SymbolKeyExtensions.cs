// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;

namespace Microsoft.CodeAnalysis
{
    internal static class SymbolKeyExtensions
    {
        public static SymbolKey GetSymbolKey(this ISymbol? symbol, CancellationToken cancellationToken = default)
            => SymbolKey.Create(symbol, cancellationToken);
    }
}
