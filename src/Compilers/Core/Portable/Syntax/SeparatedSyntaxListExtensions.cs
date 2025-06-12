// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis
{
    internal static class SeparatedSyntaxListExtensions
    {
        internal static int Count<TNode>(this SeparatedSyntaxList<TNode> list, Func<TNode, bool> predicate)
            where TNode : SyntaxNode
        {
            int n = list.Count;
            int count = 0;
            for (int i = 0; i < n; i++)
            {
                if (predicate(list[i]))
                {
                    count++;
                }
            }

            return count;
        }
    }
}
