// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Collections;

/// <summary>
/// Witness interface that allows transparent access to information about a specific <see cref="IIntervalTree{T}"/>
/// implementation without needing to know the specifics of that implementation.  This allows <see
/// cref="IntervalTreeHelpers{T, TIntervalTree, TNode, TIntervalTreeWitness}"/> to operate transparently over any
/// implementation.  IntervalTreeHelpers constrains its TIntervalTreeWitness type to be a struct to ensure this can be
/// entirely reified and erased by the runtime.
/// </summary>
internal interface IIntervalTreeWitness<T, TIntervalTree, TNode>
    where TIntervalTree : IIntervalTree<T>
{
    public bool TryGetRoot(TIntervalTree tree, [NotNullWhen(true)] out TNode? root);
    public bool TryGetLeftNode(TIntervalTree tree, TNode node, [NotNullWhen(true)] out TNode? leftNode);
    public bool TryGetRightNode(TIntervalTree tree, TNode node, [NotNullWhen(true)] out TNode? rightNode);

    public T GetValue(TIntervalTree tree, TNode node);
    public TNode GetMaxEndNode(TIntervalTree tree, TNode node);
}

/// <summary>
/// Utility helpers used to allow code sharing for the different implementations of <see cref="IIntervalTree{T}"/>s.
/// </summary>
internal static class IntervalTreeHelpers<T, TIntervalTree, TNode, TIntervalTreeWitness>
    where TIntervalTree : IIntervalTree<T>
    where TIntervalTreeWitness : struct, IIntervalTreeWitness<T, TIntervalTree, TNode>
{
    private static readonly ObjectPool<Stack<TNode>> s_nodeStackPool = new(() => new(), 128, trimOnFree: false);

    public static IEnumerator<T> GetEnumerator(TIntervalTree tree)
    {
        var witness = default(TIntervalTreeWitness);
        if (!witness.TryGetRoot(tree, out var root))
            return SpecializedCollections.EmptyEnumerator<T>();

        return GetEnumeratorWorker(tree, witness, root);

        static IEnumerator<T> GetEnumeratorWorker(
            TIntervalTree tree, TIntervalTreeWitness witness, TNode root)
        {
            using var _ = s_nodeStackPool.GetPooledObject(out var stack);
            var currentNode = root;
            var currentNodeHasValue = true;

            while (currentNodeHasValue || stack.Count > 0)
            {
                // Traverse all the way down the left side of the tree, pushing nodes onto the stack as we go.
                while (currentNodeHasValue)
                {
                    stack.Push(currentNode!);
                    currentNodeHasValue = witness.TryGetLeftNode(tree, currentNode!, out currentNode);
                }

                Contract.ThrowIfTrue(currentNodeHasValue);
                Contract.ThrowIfTrue(stack.Count == 0);
                currentNode = stack.Pop();

                // We only get to a node once we've walked the left side of it.  So we can now return the parent node at
                // that point.
                yield return witness.GetValue(tree, currentNode);

                // now get the right side and set things up so we can walk into it.
                currentNodeHasValue = witness.TryGetRightNode(tree, currentNode, out currentNode);
            }
        }
    }

    public static int FillWithIntervalsThatMatch<TIntrospector>(
        TIntervalTree tree, int start, int length,
        TestInterval<T, TIntrospector> testInterval,
        ref TemporaryArray<T> builder,
        in TIntrospector introspector,
        bool stopAfterFirst)
        where TIntrospector : struct, IIntervalIntrospector<T>
    {
        var witness = default(TIntervalTreeWitness);
        if (!witness.TryGetRoot(tree, out var root))
            return 0;

        using var _ = s_nodeStackPool.GetPooledObject(out var stack);
        var currentNode = root;
        var currentNodeHasValue = true;

        var matches = 0;
        var end = start + length;

        while (currentNodeHasValue || stack.Count > 0)
        {
            // Traverse all the way down the left side of the tree, pushing nodes onto the stack as we go.
            while (currentNodeHasValue)
            {
                stack.Push(currentNode!);
                currentNodeHasValue = witness.TryGetLeftNode(tree, currentNode!, out currentNode);
            }

            Contract.ThrowIfTrue(currentNodeHasValue);
            Contract.ThrowIfTrue(stack.Count == 0);
            currentNode = stack.Pop();

            // We only get to a node once we've walked the left side of it.  So we can now process the parent node at
            // that point.

            var currentNodeValue = witness.GetValue(tree, currentNode);
            if (testInterval(currentNodeValue, start, length, in introspector))
            {
                matches++;
                builder.Add(currentNodeValue);

                if (stopAfterFirst)
                    return 1;
            }

            // now get the right side and set things up so we can walk into it.
            currentNodeHasValue = witness.TryGetRightNode(tree, currentNode, out currentNode);
        }

        return matches;
    }

    public static bool Any<TIntrospector>(TIntervalTree tree, int start, int length, TestInterval<T, TIntrospector> testInterval, in TIntrospector introspector)
        where TIntrospector : struct, IIntervalIntrospector<T>
    {
        // Inlined version of FillWithIntervalsThatMatch, optimized to do less work and stop once it finds a match.

        var witness = default(TIntervalTreeWitness);
        if (!witness.TryGetRoot(tree, out var root))
            return false;

        using var _ = s_nodeStackPool.GetPooledObject(out var candidates);

        var end = start + length;

        candidates.Push(root);

        while (candidates.TryPop(out var currentNode))
        {
            // Check the nodes as we go down.  That way we can stop immediately when we find something that matches,
            // instead of having to do an entire in-order walk, which might end up hitting a lot of nodes we don't care
            // about and placing a lot into the stack.
            if (testInterval(witness.GetValue(tree, currentNode), start, length, in introspector))
                return true;

            if (ShouldExamineRight(tree, start, end, currentNode, in introspector, out var right))
                candidates.Push(right);

            if (ShouldExamineLeft(tree, start, currentNode, in introspector, out var left))
                candidates.Push(left);
        }

        return false;
    }

    private static bool ShouldExamineRight<TIntrospector>(
        TIntervalTree tree,
        int start,
        int end,
        TNode currentNode,
        in TIntrospector introspector,
        [NotNullWhen(true)] out TNode? right) where TIntrospector : struct, IIntervalIntrospector<T>
    {
        var witness = default(TIntervalTreeWitness);

        // right children's starts will never be to the left of the parent's start so we should consider right
        // subtree only if root's start overlaps with interval's End, 
        if (introspector.GetSpan(witness.GetValue(tree, currentNode)).Start <= end)
        {
            if (witness.TryGetRightNode(tree, currentNode, out var rightNode) &&
                GetEnd(witness.GetValue(tree, witness.GetMaxEndNode(tree, rightNode)), in introspector) >= start)
            {
                right = rightNode;
                return true;
            }
        }

        right = default;
        return false;
    }

    private static bool ShouldExamineLeft<TIntrospector>(
        TIntervalTree tree,
        int start,
        TNode currentNode,
        in TIntrospector introspector,
        [NotNullWhen(true)] out TNode? left) where TIntrospector : struct, IIntervalIntrospector<T>
    {
        var witness = default(TIntervalTreeWitness);
        // only if left's maxVal overlaps with interval's start, we should consider 
        // left subtree
        if (witness.TryGetLeftNode(tree, currentNode, out left) &&
            GetEnd(witness.GetValue(tree, witness.GetMaxEndNode(tree, left)), in introspector) >= start)
        {
            return true;
        }

        return false;
    }

    private static int GetEnd<TIntrospector>(T value, in TIntrospector introspector)
        where TIntrospector : struct, IIntervalIntrospector<T>
        => introspector.GetSpan(value).End;
}
