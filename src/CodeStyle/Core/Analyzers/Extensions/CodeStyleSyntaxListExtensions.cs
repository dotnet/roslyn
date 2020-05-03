// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis
{
    internal static class CodeStyleSyntaxListExtensions
    {
        public static SyntaxList<TDerived> CastDown<TDerived>(this SyntaxList<SyntaxNode> list)
            where TDerived : SyntaxNode
        {
            return list;
        }
    }
}
