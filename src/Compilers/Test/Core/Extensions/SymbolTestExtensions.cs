// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Microsoft.CodeAnalysis.Test.Utilities;

public static class SymbolTestExtensions
{
    public static string ToTestDisplayString(this ISymbol? symbol)
    {
        Assert.NotNull(symbol);
        return symbol.ToDisplayString(SymbolDisplayFormat.TestFormat);
    }
}
