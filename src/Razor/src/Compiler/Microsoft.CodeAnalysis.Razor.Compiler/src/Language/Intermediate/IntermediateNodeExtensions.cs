// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public static class IntermediateNodeExtensions
{
    public static ImmutableArray<RazorDiagnostic> GetAllDiagnostics(this IntermediateNode node)
    {
        ArgHelper.ThrowIfNull(node);

        var diagnostics = new PooledHashSet<RazorDiagnostic>();
        try
        {
            CollectDiagnostics(node, ref diagnostics);

            return diagnostics.OrderByAsArray(static d => d.Span.AbsoluteIndex);
        }
        finally
        {
            diagnostics.ClearAndFree();
        }

        static void CollectDiagnostics(IntermediateNode node, ref PooledHashSet<RazorDiagnostic> diagnostics)
        {
            if (node.HasDiagnostics)
            {
                diagnostics.UnionWith(node.Diagnostics);
            }

            foreach (var childNode in node.Children)
            {
                CollectDiagnostics(childNode, ref diagnostics);
            }
        }
    }

    public static ImmutableArray<TNode> FindDescendantNodes<TNode>(this IntermediateNode node)
        where TNode : IntermediateNode
    {
        using var results = new PooledArrayBuilder<TNode>();
        node.CollectDescendantNodes(ref results.AsRef());

        return results.ToImmutableAndClear();
    }

    internal static void CollectDescendantNodes<TNode>(this IntermediateNode root, ref PooledArrayBuilder<TNode> results)
        where TNode : IntermediateNode
    {
        using var stack = new PooledArrayBuilder<IntermediateNode>();
        ref var stackRef = ref stack.AsRef();

        PushChildren(root, ref stackRef);

        while (stack.Count > 0)
        {
            var node = stack.Pop();

            if (node is TNode target)
            {
                results.Add(target);
            }

            PushChildren(node, ref stackRef);
        }

        static void PushChildren(IntermediateNode node, ref PooledArrayBuilder<IntermediateNode> stack)
        {
            // Push children in reverse order so we process them in original order.
            var children = node.Children;
            for (var i = children.Count - 1; i >= 0; i--)
            {
                stack.Push(children[i]);
            }
        }
    }
}
