// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Collections;

internal static partial class IntervalTreeHelpers<T, TIntervalTree, TNode, TIntervalTreeWitness>
    where TIntervalTree : IIntervalTree<T>
    where TIntervalTreeWitness : struct, IIntervalTreeWitness<T, TIntervalTree, TNode>
{
    /// <summary>
    /// Struct based enumerator, so we can iterate an interval tree without allocating.
    /// </summary>
    private struct NodeEnumerator<TIntrospector> : IEnumerator<TNode>
        where TIntrospector : struct, IIntervalIntrospector<T>
    {
        private readonly TIntervalTree _tree;
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

            _currentNodeHasValue = default(TIntervalTreeWitness).TryGetRoot(_tree, out _currentNode);

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

            if (_currentNodeHasValue || _stack.Count > 0)
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
}
