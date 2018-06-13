// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.CodeAnalysis.ConvertLinq.ConvertForEachToLinqQuery
{
    internal static class Helpers
    {
        public static SyntaxTrivia[] GetTrivia(IEnumerable<SyntaxToken> tokens)
            => tokens.SelectMany(token => GetTrivia(token)).ToArray();

        public static SyntaxTrivia[] GetTrivia(SyntaxNodeOrToken nodeOrToken)
            => nodeOrToken.GetLeadingTrivia().Concat(nodeOrToken.GetTrailingTrivia()).ToArray();

        public static SyntaxTrivia[] GetTrivia(params SyntaxNodeOrToken[] nodesOrTokens)
            => nodesOrTokens.SelectMany(nodeOrToken => GetTrivia(nodeOrToken)).ToArray();
    }
}
