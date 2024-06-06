// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
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

    public static NodeEnumerator<TIntrospector> GetNodeEnumerator<TIntrospector>(TIntervalTree tree, int start, int end, in TIntrospector introspector)
        where TIntrospector : struct, IIntervalIntrospector<T>
        => new(tree, start, end, introspector);

    public struct NodeEnumerator<TIntrospector> : IEnumerator<TNode>
        where TIntrospector : struct, IIntervalIntrospector<T>
    {
        private readonly TIntervalTree _tree;
        private readonly TIntervalTreeWitness _witness;
        private readonly TIntrospector _introspector;
        private readonly int _start;
        private readonly int _end;

        private readonly PooledObject<Stack<TNode>> _pooledStack;
        private readonly Stack<TNode>? _stack;

        private bool _started;
        private TNode? _currentNode;
        private bool _currentNodeHasValue;

        public NodeEnumerator(TIntervalTree tree, int start, int end, in TIntrospector introspector)
        {
            _tree = tree;
            _start = start;
            _end = end;
            _introspector = introspector;

            _currentNodeHasValue = _witness.TryGetRoot(_tree, out _currentNode);

            // Avoid any pooling work if we don't even have a root.
            if (_currentNodeHasValue)
            {
                _pooledStack = s_nodeStackPool.GetPooledObject();
                _stack = _pooledStack.Object;
            }
        }

        readonly object IEnumerator.Current => this.Current!;

        public readonly TNode Current => _currentNode!;

        public bool MoveNext()
        {
            // Trivial empty case
            if (_stack is null)
                return false;

            // The first time through, we just want to start processing with the root node.  Every other time through,
            // after we've yielded the current element, we  want to walk down the right side of it.
            if (_started)
                _currentNodeHasValue = ShouldExamineRight(_tree, _start, _end, _currentNode!, _introspector, out _currentNode);

            // After we're called once, we're in the started point.
            _started = true;

            while (_currentNodeHasValue || _stack.Count > 0)
            {
                // Traverse all the way down the left side of the tree, pushing nodes onto the stack as we go.
                while (_currentNodeHasValue)
                {
                    _stack.Push(_currentNode!);
                    _currentNodeHasValue = ShouldExamineLeft(_tree, _start, _currentNode!, _introspector, out _currentNode);
                }

                Contract.ThrowIfTrue(_currentNodeHasValue);
                Contract.ThrowIfTrue(_stack.Count == 0);
                _currentNode = _stack.Pop();
                return true;
            }

            return false;
        }

        public readonly void Dispose()
            => _pooledStack.Dispose();

        public readonly void Reset()
            => throw new System.NotImplementedException();
    }

    /// <summary>
    /// An introspector that always throws.  Used when we need to call an api that takes this, but we know will never
    /// call into it due to other arguments we pass along.
    /// </summary>
    private readonly struct AlwaysThrowIntrospector : IIntervalIntrospector<T>
    {
        public TextSpan GetSpan(T value) => throw new System.NotImplementedException();
    }

    public struct Enumerator(TIntervalTree tree) : IEnumerator<T>
    {
        private readonly TIntervalTree _tree = tree;
        private readonly TIntervalTreeWitness _witness;

        /// <summary>
        /// Because we're passing the full span of all ints, we know that we'll never call into the introspector.  Since
        /// all intervals will always be in that span.
        /// </summary>
        private NodeEnumerator<AlwaysThrowIntrospector> _nodeEnumerator =
            GetNodeEnumerator(tree, start: int.MinValue, end: int.MaxValue, default(AlwaysThrowIntrospector));

        readonly object IEnumerator.Current => this.Current!;

        public readonly T Current => _witness.GetValue(_tree, _nodeEnumerator.Current);

        public bool MoveNext() => _nodeEnumerator.MoveNext();

        public readonly void Reset() => _nodeEnumerator.Reset();

        public readonly void Dispose() => _nodeEnumerator.Dispose();
    }

    public static Enumerator GetEnumerator(TIntervalTree tree)
        => new(tree);

    public static int FillWithIntervalsThatMatch<TIntrospector>(
        TIntervalTree tree, int start, int length,
        TestInterval<T, TIntrospector> testInterval,
        ref TemporaryArray<T> builder,
        in TIntrospector introspector,
        bool stopAfterFirst)
        where TIntrospector : struct, IIntervalIntrospector<T>
    {
        var witness = default(TIntervalTreeWitness);

        var matchCount = 0;
        var end = start + length;

        foreach (var currentNode in GetNodeEnumerator(tree, start, end, in introspector))
        {
            var currentNodeValue = witness.GetValue(tree, currentNode);
            if (testInterval(currentNodeValue, start, length, in introspector))
            {
                matchCount++;
                builder.Add(currentNodeValue);

                if (stopAfterFirst)
                    return 1;
            }
        }

        return matchCount;
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
