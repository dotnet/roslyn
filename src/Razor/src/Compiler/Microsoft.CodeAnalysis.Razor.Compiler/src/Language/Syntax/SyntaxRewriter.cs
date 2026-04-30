// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

internal abstract partial class SyntaxRewriter : SyntaxVisitor<SyntaxNode>
{
    private int _recursionDepth;

    [return: NotNullIfNotNull(nameof(node))]
    public override SyntaxNode? Visit(SyntaxNode? node)
    {
        if (node != null)
        {
            Debug.Assert(!node.IsToken);
            Debug.Assert(!node.IsList);

            _recursionDepth++;
            StackGuard.EnsureSufficientExecutionStack(_recursionDepth);

            var result = ((RazorSyntaxNode)node).Accept(this);

            _recursionDepth--;
            return result!;
        }
        else
        {
            return null;
        }
    }

    public virtual SyntaxToken VisitToken(SyntaxToken token)
    {
        return token;
    }

    public virtual SyntaxList<TNode> VisitList<TNode>(SyntaxList<TNode> list)
        where TNode : RazorSyntaxNode
    {
        var count = list.Count;
        if (count == 0)
        {
            return list;
        }

        using var builder = new PooledArrayBuilder<TNode>(capacity: count);

        var isUpdating = false;

        for (var i = 0; i < count; i++)
        {
            var item = list[i];

            var visited = VisitListElement(item);

            if (item != visited && !isUpdating)
            {
                // The list is being updated, so we need to initialize the builder
                // add the items we've seen so far.
                builder.AddRange(list, startIndex: 0, count: i);

                isUpdating = true;
            }

            if (isUpdating && visited != null)
            {
                builder.Add(visited);
            }
        }

        return isUpdating
            ? builder.ToList()
            : list;
    }

    public virtual TNode? VisitListElement<TNode>(TNode? node)
        where TNode : RazorSyntaxNode
    {
        return (TNode?)Visit(node);
    }

    public virtual SyntaxTokenList VisitList(SyntaxTokenList list)
    {
        var count = list.Count;
        if (count == 0)
        {
            return list;
        }

        using var builder =  new PooledArrayBuilder<SyntaxToken>(count);

        var isUpdating = false;

        for (var i = 0; i < count; i++)
        {
            var item = list[i];

            var visited = VisitToken(item);

            if (item != visited && !isUpdating)
            {
                // The list is being updated, so we need to initialize the builder
                // add the items we've seen so far.
                builder.AddRange(list, startIndex: 0, count: i);

                isUpdating = true;
            }

            if (isUpdating && visited.Kind != SyntaxKind.None)
            {
                builder.Add(visited);
            }
        }

        return isUpdating
            ? builder.ToList()
            : list;
    }
}
