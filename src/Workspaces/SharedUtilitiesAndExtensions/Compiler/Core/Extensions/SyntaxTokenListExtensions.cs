// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static class SyntaxTokenListExtensions
{
    public static SyntaxTokenList ToSyntaxTokenList(this IEnumerable<SyntaxToken> tokens)
        => new(tokens);

    public static SyntaxTokenList ToSyntaxTokenListAndFree(this ArrayBuilder<SyntaxToken> tokens)
    {
        var tokenList = new SyntaxTokenList(tokens);
        tokens.Free();
        return tokenList;
    }
}
