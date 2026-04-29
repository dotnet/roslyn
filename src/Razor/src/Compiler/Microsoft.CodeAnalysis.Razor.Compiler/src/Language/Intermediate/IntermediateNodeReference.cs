// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
public readonly struct IntermediateNodeReference
{
    public IntermediateNode Node { get; }
    public IntermediateNode Parent { get; }

    public IntermediateNodeReference(IntermediateNode node, IntermediateNode parent)
    {
        ArgHelper.ThrowIfNull(parent);
        ArgHelper.ThrowIfNull(node);

        Parent = parent;
        Node = node;
    }

    public void Deconstruct(out IntermediateNode node, out IntermediateNode parent)
    {
        node = Node;
        parent = Parent;
    }

    private int GetNodeIndexForMutation()
    {
        if (Parent == null)
        {
            throw new InvalidOperationException(Resources.IntermediateNodeReference_NotInitialized);
        }

        if (Parent.Children.IsReadOnly)
        {
            throw new InvalidOperationException(Resources.FormatIntermediateNodeReference_CollectionIsReadOnly(Parent));
        }

        var index = Parent.Children.IndexOf(Node);
        if (index < 0)
        {
            throw new InvalidOperationException(Resources.FormatIntermediateNodeReference_NodeNotFound(Node, Parent));
        }

        return index;
    }

    public IntermediateNodeReference InsertAfter(IntermediateNode node)
    {
        ArgHelper.ThrowIfNull(node);

        var index = GetNodeIndexForMutation();

        Parent.Children.Insert(index + 1, node);
        return new IntermediateNodeReference(node, Parent);
    }

    public void InsertAfter(IEnumerable<IntermediateNode> nodes)
    {
        ArgHelper.ThrowIfNull(nodes);

        var index = GetNodeIndexForMutation();

        foreach (var node in nodes)
        {
            Parent.Children.Insert(++index, node);
        }
    }

    public IntermediateNodeReference InsertBefore(IntermediateNode node)
    {
        ArgHelper.ThrowIfNull(node);

        var index = GetNodeIndexForMutation();

        Parent.Children.Insert(index, node);
        return new IntermediateNodeReference(node, Parent);
    }

    public void InsertBefore(IEnumerable<IntermediateNode> nodes)
    {
        ArgHelper.ThrowIfNull(nodes);

        var index = GetNodeIndexForMutation();

        foreach (var node in nodes)
        {
            Parent.Children.Insert(index++, node);
        }
    }

    public void Remove()
    {
        var index = GetNodeIndexForMutation();

        Parent.Children.RemoveAt(index);
    }

    public IntermediateNodeReference Replace(IntermediateNode node)
    {
        ArgHelper.ThrowIfNull(node);

        var index = GetNodeIndexForMutation();

        Parent.Children[index] = node;
        return new IntermediateNodeReference(node, Parent);
    }

    private string GetDebuggerDisplay()
        => $"ref: {Parent.GetDebuggerDisplay()} - {Node.GetDebuggerDisplay()}";
}
