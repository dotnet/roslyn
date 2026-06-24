// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public abstract partial class IntermediateNodeWalker : IntermediateNodeVisitor
{
    private Stack _ancestorStack;

    protected ReadOnlySpan<IntermediateNode> Ancestors => _ancestorStack.Span;

    protected IntermediateNode? Parent
        => _ancestorStack.Span is [var parent, ..] ? parent : null;

    public override void VisitDefault(IntermediateNode node)
    {
        var children = node.Children;
        if (children.Count == 0)
        {
            return;
        }

        _ancestorStack.Push(node);

        try
        {
            // Visiting a child may mutate it's parent's children, so we don't use foreach here.
            for (var i = 0; i < children.Count; i++)
            {
                Visit(children[i]);
            }
        }
        finally
        {
            _ancestorStack.Pop();
        }
    }
}
