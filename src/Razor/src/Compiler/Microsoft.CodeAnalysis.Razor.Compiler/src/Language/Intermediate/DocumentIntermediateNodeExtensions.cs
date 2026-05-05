// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public static class DocumentIntermediateNodeExtensions
{
    public static ClassDeclarationIntermediateNode? FindPrimaryClass(this DocumentIntermediateNode node)
    {
        ArgHelper.ThrowIfNull(node);

        return FindNode<ClassDeclarationIntermediateNode>(node, static n => n.IsPrimaryClass);
    }

    public static MethodDeclarationIntermediateNode? FindPrimaryMethod(this DocumentIntermediateNode node)
    {
        ArgHelper.ThrowIfNull(node);

        return FindNode<MethodDeclarationIntermediateNode>(node, static n => n.IsPrimaryMethod);
    }

    public static NamespaceDeclarationIntermediateNode? FindPrimaryNamespace(this DocumentIntermediateNode node)
    {
        ArgHelper.ThrowIfNull(node);

        return FindNode<NamespaceDeclarationIntermediateNode>(node, static n => n.IsPrimaryNamespace);
    }

    private static T? FindNode<T>(IntermediateNode node, Func<T, bool> predicate)
        where T : IntermediateNode
    {
        using var stack = new PooledArrayBuilder<IntermediateNode>();
        stack.Push(node);

        while (stack.Count > 0)
        {
            node = stack.Pop();

            if (node is T target && predicate(target))
            {
                return target;
            }

            // Push in reverse order so we process in original order.
            var children = node.Children;
            for (var i = children.Count - 1; i >= 0; i--)
            {
                stack.Push(children[i]);
            }
        }

        return null;
    }

    public static ImmutableArray<IntermediateNodeReference<DirectiveIntermediateNode>> FindDirectiveReferences(
        this DocumentIntermediateNode node, DirectiveDescriptor directive)
    {
        ArgHelper.ThrowIfNull(node);
        ArgHelper.ThrowIfNull(directive);

        using var results = new PooledArrayBuilder<IntermediateNodeReference<DirectiveIntermediateNode>>();
        node.CollectDirectiveReferences(directive, ref results.AsRef());

        return results.ToImmutableAndClear();
    }

    internal static void CollectDirectiveReferences(
        this DocumentIntermediateNode document,
        DirectiveDescriptor directive,
        ref PooledArrayBuilder<IntermediateNodeReference<DirectiveIntermediateNode>> references)
    {
        using var stack = new PooledArrayBuilder<(IntermediateNode node, IntermediateNode parent)>();

        stack.Push((document, null!));

        while (stack.Count > 0)
        {
            var (node, parent) = stack.Pop();

            if (node is DirectiveIntermediateNode directiveNode &&
                directiveNode.Directive == directive)
            {
                references.Add(new(directiveNode, parent));
            }

            var children = node.Children;

            // Push children on the stack in reverse order so they are processed in the original order.
            for (var i = children.Count - 1; i >= 0; i--)
            {
                stack.Push((children[i], node));
            }
        }
    }

    public static ImmutableArray<IntermediateNodeReference<TNode>> FindDescendantReferences<TNode>(this DocumentIntermediateNode document)
        where TNode : IntermediateNode
    {
        ArgHelper.ThrowIfNull(document);

        using var results = new PooledArrayBuilder<IntermediateNodeReference<TNode>>();
        document.CollectDescendantReferences(ref results.AsRef());

        return results.ToImmutableAndClear();
    }

    internal static void CollectDescendantReferences<TNode>(
        this DocumentIntermediateNode document,
        ref PooledArrayBuilder<IntermediateNodeReference<TNode>> references)
        where TNode : IntermediateNode
    {
        // Use a post-order traversal because references are used to replace nodes, and thus
        // change the parent nodes.
        //
        // This ensures that we always operate on the leaf nodes first.

        using var stack = new PooledArrayBuilder<(IntermediateNode node, IntermediateNode parent, bool visited)>();

        stack.Push((document, null!, false));

        while (stack.Count > 0)
        {
            // Pop the top of the stack and see if this node has been visited.
            var (node, parent, visited) = stack.Pop();

            if (visited)
            {
                // We've already visited the children, so process this node.
                if (node is TNode typedNode && parent != null)
                {
                    references.Add(new(typedNode, parent));
                }
            }
            else
            {
                // Push back on the stack and mark as visited.
                stack.Push((node, parent, true));

                var children = node.Children;

                // Push the children in reverse order so they are processed in the original order.
                for (var i = children.Count - 1; i >= 0; i--)
                {
                    stack.Push((children[i], node, false));
                }
            }
        }
    }
}
