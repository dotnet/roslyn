// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp.Extensions;

internal static class SyntaxTokenListExtensions
{
    public static IEnumerable<SyntaxToken> SkipKinds(this SyntaxTokenList tokenList, params SyntaxKind[] kinds)
        => tokenList.SkipWhile(t => kinds.Contains(t.Kind()));
}
