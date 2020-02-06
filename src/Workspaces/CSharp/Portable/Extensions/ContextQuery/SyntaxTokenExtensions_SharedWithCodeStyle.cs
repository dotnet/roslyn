// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery
{
    internal static partial class SyntaxTokenExtensions
    {
        public static bool IsLastTokenOfNode<T>(this SyntaxToken token)
            where T : SyntaxNode
        {
            var node = token.GetAncestor<T>();
            return node != null && token == node.GetLastToken(includeZeroWidth: true);
        }
    }
}
