// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

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
internal static partial class IntervalTreeHelpers<T, TIntervalTree, TNode, TIntervalTreeWitness>
    where TIntervalTree : IIntervalTree<T>
    where TIntervalTreeWitness : struct, IIntervalTreeWitness<T, TIntervalTree, TNode>
{
    private static readonly ObjectPool<Stack<TNode>> s_nodeStackPool = new(() => new(), 128, trimOnFree: false);

    public static Enumerator GetEnumerator(TIntervalTree tree)
        => new(tree);

    public static int FillWithIntervalsThatMatch<TIntrospector, TIntervalTester>(
        TIntervalTree tree, int start, int length,
        ref TemporaryArray<T> builder,
        in TIntrospector introspector,
        in TIntervalTester intervalTester,
        bool stopAfterFirst)
        where TIntrospector : struct, IIntervalIntrospector<T>
        where TIntervalTester : struct, IIntervalTester<T, TIntrospector>
    {
        var witness = default(TIntervalTreeWitness);

        var matchCount = 0;
        var end = start + length;

        using var enumerator = new NodeEnumerator<TIntrospector>(tree, start, end, introspector);
        while (enumerator.MoveNext())
        {
            var currentNodeValue = witness.GetValue(tree, enumerator.Current);
            if (intervalTester.Test(currentNodeValue, start, length, in introspector))
            {
                matchCount++;
                builder.Add(currentNodeValue);

                if (stopAfterFirst)
                    return 1;
            }
        }

        return matchCount;
    }

    public static bool Any<TIntrospector, TIntervalTester>(
        TIntervalTree tree, int start, int length, in TIntrospector introspector, in TIntervalTester intervalTester)
        where TIntrospector : struct, IIntervalIntrospector<T>
        where TIntervalTester : struct, IIntervalTester<T, TIntrospector>
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
            if (intervalTester.Test(witness.GetValue(tree, currentNode), start, length, in introspector))
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

        if (start == int.MinValue && end == int.MaxValue)
            return witness.TryGetRightNode(tree, currentNode, out right);

        // right children's starts will never be to the left of the parent's start so we should consider right
        // subtree only if root's start overlaps with interval's End, 
        if (introspector.GetSpan(witness.GetValue(tree, currentNode)).Start <= end)
        {
            if (witness.TryGetRightNode(tree, currentNode, out var rightNode) &&
                introspector.GetSpan(witness.GetValue(tree, witness.GetMaxEndNode(tree, rightNode))).End >= start)
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

        if (start == int.MinValue)
            return witness.TryGetLeftNode(tree, currentNode, out left);

        // only if left's maxVal overlaps with interval's start, we should consider 
        // left subtree
        if (witness.TryGetLeftNode(tree, currentNode, out left) &&
            introspector.GetSpan(witness.GetValue(tree, witness.GetMaxEndNode(tree, left))).End >= start)
        {
            return true;
        }

        return false;
    }
}
