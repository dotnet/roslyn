// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax;

using Microsoft.CodeAnalysis.Syntax.InternalSyntax;

internal static class SyntaxListPoolExtensions
{
    public static SyntaxList<SyntaxToken> ToTokenListAndFree(this SyntaxListPool pool, SyntaxListBuilder builder)
    {
        var listNode = builder.ToListNode();
        pool.Free(builder);
        return new SyntaxList<SyntaxToken>(listNode);
    }
}
