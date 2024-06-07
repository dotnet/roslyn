// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis;

internal static class SeparatedSyntaxListWrapper
{
    public static SeparatedSyntaxListWrapper<TNode> Create<TNode>(ReadOnlySpan<TNode> nodes)
    //where TNode : SyntaxNode
    {
        throw new NotImplementedException();
        //if (nodes.Length == 0)
        //    return default;

        //if (nodes.Length == 1)
        //    return new SeparatedSyntaxListWrapper<TNode>(new SyntaxNodeOrTokenList(nodes[0], index: 0));

        //var builder = new CodeAnalysis.Syntax.SeparatedSyntaxListBuilder<TNode>(nodes.Length);

        //builder.Add(nodes[0]);

        //var separator = nodes[0].Green.CreateSeparator(nodes[0]);

        //for (int i = 1, n = nodes.Length; i < n; i++)
        //{
        //    builder.AddSeparator(separator);
        //    builder.Add(nodes[i]);
        //}

        //return builder.ToList();
    }
}

