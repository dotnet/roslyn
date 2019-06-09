// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static class SyntaxTokenListExtensions
    {
        public static IEnumerable<SyntaxToken> SkipKinds(this SyntaxTokenList tokenList, params SyntaxKind[] kinds)
        {
            return tokenList.SkipWhile(t => t.IsKind(kinds));
        }
    }
}
