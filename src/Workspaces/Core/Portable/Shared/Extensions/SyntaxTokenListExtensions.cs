// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class SyntaxTokenListExtensions
    {
        public static SyntaxTokenList ToSyntaxTokenList(this IEnumerable<SyntaxToken> tokens)
            => new SyntaxTokenList(tokens);

        public static SyntaxTokenList ToSyntaxTokenListAndFree(this ArrayBuilder<SyntaxToken> tokens)
        {
            var tokenList = new SyntaxTokenList(tokens);
            tokens.Free();
            return tokenList;
        }
    }
}
