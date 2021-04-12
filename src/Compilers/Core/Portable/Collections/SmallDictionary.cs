﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Dictionary designed to hold small number of items.
    /// Compared to the regular Dictionary, average overhead per-item is roughly the same, but 
    /// unlike regular dictionary, this one is based on an AVL tree and as such does not require 
    /// rehashing when items are added.
    /// It does require rebalancing, but that is allocation-free.
    ///
    /// Major caveats:
    ///  1) There is no Remove method. (can be added, but we do not seem to use Remove that much)
    ///  2) foreach [keys|values|pairs] may allocate a small array.
    ///  3) Performance is no longer O(1). At a certain count it becomes slower than regular Dictionary.
    ///     In comparison to regular Dictionary on my machine:
    ///        On trivial number of elements (5 or so) it is more than 2x faster.
    ///        The break even count is about 120 elements for read and 55 for write operations (with unknown initial size).
    ///        At UShort.MaxValue elements, this dictionary is 6x slower to read and 4x slower to write
    ///
    /// Generally, this dictionary is a win if number of elements is small, not known beforehand or both.
    ///
    /// If the size of the dictionary is known at creation and it is likely to contain more than 10 elements, 
    /// then regular Dictionary is a better choice.
    /// </summary>
    internal sealed class SmallDictionary<K, V> : IEnumerable<KeyValuePair<K, V>>
        where K : notnull
    {
        private AvlNode? _root;
        public readonly IEqualityComparer<K> Comparer;

        // https://github.com/dotnet/roslyn/issues/40344
        public static readonly SmallDictionary<K, V> Empty = new SmallDictionary<K, V>(null!);

        public SmallDictionary() : this(EqualityComparer<K>.Default) { }

        public SmallDictionary(IEqualityComparer<K> comparer)
        {
            Comparer = comparer;
        }

        public SmallDictionary(SmallDictionary<K, V> other, IEqualityComparer<K> comparer)
            : this(comparer)
        {
            // TODO: if comparers are same (often they are), then just need to clone the tree.
            foreach (var kv in other)
            {
                this.Add(kv.Key, kv.Value);
            }
        }

        private bool CompareKeys(K k1, K k2)
        {
            return Comparer.Equals(k1, k2);
        }

        private int GetHashCode(K k)
        {
            return Comparer.GetHashCode(k);
        }

        public bool TryGetValue(K key, [MaybeNullWhen(returnValue: false)] out V value)
        {
            if (_root != null)
            {
                return TryGetValue(GetHashCode(key), key, out value!);
            }

            value = default!;
            return false;
        }

        public void Add(K key, V value)
        {
            Insert(GetHashCode(key), key, value, add: true);
        }

        public V this[K key]
        {
            get
            {
                V value;
                if (!TryGetValue(key, out value!))
                {
                    throw new KeyNotFoundException($"Could not find key {key}");
                }

                return value;
            }

            set
            {
                this.Insert(GetHashCode(key), key, value, add: false);
            }
        }

        public bool ContainsKey(K key)
        {
            V value;
            return TryGetValue(key, out value!);
        }

        [Conditional("DEBUG")]
        internal void AssertBalanced()
        {
#if DEBUG
            AvlNode.AssertBalanced(_root);
#endif
        }

        private abstract class Node
        {
            public readonly K Key;
            public V Value;

            protected Node(K key, V value)
            {
                this.Key = key;
                this.Value = value;
            }

            public virtual Node? Next => null;
        }

        private sealed class NodeLinked : Node
        {
            public NodeLinked(K key, V value, Node next)
                : base(key, value)
            {
                this.Next = next;
            }

            public override Node Next { get; }
        }

        private sealed class AvlNodeHead : AvlNode
        {
            public Node next;

            public AvlNodeHead(int hashCode, K key, V value, Node next)
                : base(hashCode, key, value)
            {
                this.next = next;
            }

            public override Node Next => next;
        }

        // separate class to ensure that HashCode field 
        // is located before other AvlNode fields
        // Balance is also here for better packing of AvlNode on 64bit
        private abstract class HashedNode : Node
        {
            public readonly int HashCode;
            public sbyte Balance;

            protected HashedNode(int hashCode, K key, V value)
                : base(key, value)
            {
                this.HashCode = hashCode;
            }
        }

        private class AvlNode : HashedNode
        {
            public AvlNode? Left;
            public AvlNode? Right;

            public AvlNode(int hashCode, K key, V value)
                : base(hashCode, key, value)
            { }

#if DEBUG
            public static int AssertBalanced(AvlNode? V)
            {
                if (V == null) return 0;

                int a = AssertBalanced(V.Left);
                int b = AssertBalanced(V.Right);

                if (a - b != V.Balance ||
                    Math.Abs(a - b) >= 2)
                {
                    throw new InvalidOperationException();
                }

                return 1 + Math.Max(a, b);
            }
#endif
        }

        private bool TryGetValue(int hashCode, K key, [MaybeNullWhen(returnValue: false)] out V value)
        {
            RoslynDebug.Assert(_root is object);
            AvlNode? b = _root;

            do
            {
                if (b.HashCode > hashCode)
                {
                    b = b.Left;
                }
                else if (b.HashCode < hashCode)
                {
                    b = b.Right;
                }
                else
                {
                    goto hasBucket;
                }
            } while (b != null);

            value = default!;
            return false;

hasBucket:
            if (CompareKeys(b.Key, key))
            {
                value = b.Value;
                return true;
            }

            return GetFromList(b.Next, key, out value!);
        }

        private bool GetFromList(Node? next, K key, [MaybeNullWhen(returnValue: false)] out V value)
        {
            while (next != null)
            {
                if (CompareKeys(key, next.Key))
                {
                    value = next.Value;
                    return true;
                }

                next = next.Next;
            }

            value = default!;
            return false;
        }

        private void Insert(int hashCode, K key, V value, bool add)
        {
            AvlNode? currentNode = _root;

            if (currentNode == null)
            {
                _root = new AvlNode(hashCode, key, value);
                return;
            }

            AvlNode? currentNodeParent = null;
            AvlNode unbalanced = currentNode;
            AvlNode? unbalancedParent = null;

            // ====== insert new node
            // also make a note of the last unbalanced node and its parent (for rotation if needed)
            // nodes on the search path from rotation candidate downwards will change balances because of the node added
            // unbalanced node itself will become balanced or will be rotated
            // either way nodes above unbalanced do not change their balance
            for (; ; )
            {
                // schedule hk read 
                var hc = currentNode.HashCode;

                if (currentNode.Balance != 0)
                {
                    unbalancedParent = currentNodeParent;
                    unbalanced = currentNode;
                }

                if (hc > hashCode)
                {
                    if (currentNode.Left == null)
                    {
                        var previousNode = currentNode;
                        currentNode = new AvlNode(hashCode, key, value);
                        previousNode.Left = currentNode;
                        break;
                    }

                    currentNodeParent = currentNode;
                    currentNode = currentNode.Left;
                }
                else if (hc < hashCode)
                {
                    if (currentNode.Right == null)
                    {
                        var previousNode = currentNode;
                        currentNode = new AvlNode(hashCode, key, value);
                        previousNode.Right = currentNode;
                        break;
                    }

                    currentNodeParent = currentNode;
                    currentNode = currentNode.Right;
                }
                else // (p.HashCode == hashCode)
                {
                    this.HandleInsert(currentNode, currentNodeParent, key, value, add);
                    return;
                }
            }

            Debug.Assert(unbalanced != currentNode);

            // ====== update balances on the path from unbalanced downwards
            var n = unbalanced;
            do
            {
                Debug.Assert(n.HashCode != hashCode);

                if (n.HashCode < hashCode)
                {
                    n.Balance--;
                    n = n.Right!;
                }
                else
                {
                    n.Balance++;
                    n = n.Left!;
                }
            }
            while (n != currentNode);

            // ====== rotate unbalanced node if needed
            AvlNode rotated;
            var balance = unbalanced.Balance;
            if (balance == -2)
            {
                rotated = unbalanced.Right!.Balance < 0 ?
                    LeftSimple(unbalanced) :
                    LeftComplex(unbalanced);
            }
            else if (balance == 2)
            {
                rotated = unbalanced.Left!.Balance > 0 ?
                    RightSimple(unbalanced) :
                    RightComplex(unbalanced);
            }
            else
            {
                return;
            }

            // ===== make parent to point to rotated
            if (unbalancedParent == null)
            {
                _root = rotated;
            }
            else if (unbalanced == unbalancedParent.Left)
            {
                unbalancedParent.Left = rotated;
            }
            else
            {
                unbalancedParent.Right = rotated;
            }
        }

        private static AvlNode LeftSimple(AvlNode unbalanced)
        {
            RoslynDebug.Assert(unbalanced.Right is object);
            var right = unbalanced.Right;
            unbalanced.Right = right.Left;
            right.Left = unbalanced;

            unbalanced.Balance = 0;
            right.Balance = 0;
            return right;
        }

        private static AvlNode RightSimple(AvlNode unbalanced)
        {
            RoslynDebug.Assert(unbalanced.Left is object);
            var left = unbalanced.Left;
            unbalanced.Left = left.Right;
            left.Right = unbalanced;

            unbalanced.Balance = 0;
            left.Balance = 0;
            return left;
        }

        private static AvlNode LeftComplex(AvlNode unbalanced)
        {
            RoslynDebug.Assert(unbalanced.Right is object);
            RoslynDebug.Assert(unbalanced.Right.Left is object);
            var right = unbalanced.Right;
            var rightLeft = right.Left;
            right.Left = rightLeft.Right;
            rightLeft.Right = right;
            unbalanced.Right = rightLeft.Left;
            rightLeft.Left = unbalanced;

            var rightLeftBalance = rightLeft.Balance;
            rightLeft.Balance = 0;

            if (rightLeftBalance < 0)
            {
                right.Balance = 0;
                unbalanced.Balance = 1;
            }
            else
            {
                right.Balance = (sbyte)-rightLeftBalance;
                unbalanced.Balance = 0;
            }

            return rightLeft;
        }

        private static AvlNode RightComplex(AvlNode unbalanced)
        {
            RoslynDebug.Assert(unbalanced.Left != null);
            RoslynDebug.Assert(unbalanced.Left.Right != null);
            var left = unbalanced.Left;
            var leftRight = left.Right;
            left.Right = leftRight.Left;
            leftRight.Left = left;
            unbalanced.Left = leftRight.Right;
            leftRight.Right = unbalanced;

            var leftRightBalance = leftRight.Balance;
            leftRight.Balance = 0;

            if (leftRightBalance < 0)
            {
                left.Balance = 1;
                unbalanced.Balance = 0;
            }
            else
            {
                left.Balance = 0;
                unbalanced.Balance = (sbyte)-leftRightBalance;
            }

            return leftRight;
        }


        private void HandleInsert(AvlNode node, AvlNode? parent, K key, V value, bool add)
        {
            Node? currentNode = node;
            do
            {
                if (CompareKeys(currentNode.Key, key))
                {
                    if (add)
                    {
                        throw new InvalidOperationException();
                    }

                    currentNode.Value = value;
                    return;
                }

                currentNode = currentNode.Next;
            } while (currentNode != null);

            AddNode(node, parent, key, value);
        }

        private void AddNode(AvlNode node, AvlNode? parent, K key, V value)
        {
            AvlNodeHead? head = node as AvlNodeHead;
            if (head != null)
            {
                var newNext = new NodeLinked(key, value, head.next);
                head.next = newNext;
                return;
            }

            var newHead = new AvlNodeHead(node.HashCode, key, value, node);
            newHead.Balance = node.Balance;
            newHead.Left = node.Left;
            newHead.Right = node.Right;

            if (parent == null)
            {
                _root = newHead;
                return;
            }

            if (node == parent.Left)
            {
                parent.Left = newHead;
            }
            else
            {
                parent.Right = newHead;
            }
        }

        public KeyCollection Keys => new KeyCollection(this);

        internal struct KeyCollection : IEnumerable<K>
        {
            private readonly SmallDictionary<K, V> _dict;

            public KeyCollection(SmallDictionary<K, V> dict)
            {
                _dict = dict;
            }

            public struct Enumerator
            {
                private readonly Stack<AvlNode>? _stack;
                private Node? _next;
                private Node? _current;

                public Enumerator(SmallDictionary<K, V> dict)
                    : this()
                {
                    var root = dict._root;
                    if (root != null)
                    {
                        // left == right only if both are nulls
                        if (root.Left == root.Right)
                        {
                            _next = root;
                        }
                        else
                        {
                            _stack = new Stack<AvlNode>(dict.HeightApprox());
                            _stack.Push(root);
                        }
                    }
                }

                public K Current => _current!.Key;

                public bool MoveNext()
                {
                    if (_next != null)
                    {
                        _current = _next;
                        _next = _next.Next;
                        return true;
                    }

                    if (_stack == null || _stack.Count == 0)
                    {
                        return false;
                    }

                    var curr = _stack.Pop();
                    _current = curr;
                    _next = curr.Next;

                    PushIfNotNull(curr.Left);
                    PushIfNotNull(curr.Right);

                    return true;
                }

                private void PushIfNotNull(AvlNode? child)
                {
                    if (child != null)
                    {
                        _stack!.Push(child);
                    }
                }
            }

            public Enumerator GetEnumerator()
            {
                return new Enumerator(_dict);
            }

            public class EnumerableImpl : IEnumerator<K>
            {
                private Enumerator _e;

                public EnumerableImpl(Enumerator e)
                {
                    _e = e;
                }

                K IEnumerator<K>.Current => _e.Current;

                void IDisposable.Dispose()
                {
                }

                object IEnumerator.Current => _e.Current;

                bool IEnumerator.MoveNext()
                {
                    return _e.MoveNext();
                }

                void IEnumerator.Reset()
                {
                    throw new NotSupportedException();
                }
            }

            IEnumerator<K> IEnumerable<K>.GetEnumerator()
            {
                return new EnumerableImpl(GetEnumerator());
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                throw new NotImplementedException();
            }
        }

        public ValueCollection Values => new ValueCollection(this);

        internal struct ValueCollection : IEnumerable<V>
        {
            private readonly SmallDictionary<K, V> _dict;

            public ValueCollection(SmallDictionary<K, V> dict)
            {
                _dict = dict;
            }

            public struct Enumerator
            {
                private readonly Stack<AvlNode>? _stack;
                private Node? _next;
                private Node? _current;

                public Enumerator(SmallDictionary<K, V> dict)
                    : this()
                {
                    var root = dict._root;
                    if (root == null)
                    {
                        return;
                    }

                    // left == right only if both are nulls
                    if (root.Left == root.Right)
                    {
                        _next = root;
                    }
                    else
                    {
                        _stack = new Stack<AvlNode>(dict.HeightApprox());
                        _stack.Push(root);
                    }
                }

                public V Current => _current!.Value;

                public bool MoveNext()
                {
                    if (_next != null)
                    {
                        _current = _next;
                        _next = _next.Next;
                        return true;
                    }

                    if (_stack == null || _stack.Count == 0)
                    {
                        return false;
                    }

                    var curr = _stack.Pop();
                    _current = curr;
                    _next = curr.Next;

                    PushIfNotNull(curr.Left);
                    PushIfNotNull(curr.Right);

                    return true;
                }

                private void PushIfNotNull(AvlNode? child)
                {
                    if (child != null)
                    {
                        _stack!.Push(child);
                    }
                }
            }

            public Enumerator GetEnumerator()
            {
                return new Enumerator(_dict);
            }

            public class EnumerableImpl : IEnumerator<V>
            {
                private Enumerator _e;

                public EnumerableImpl(Enumerator e)
                {
                    _e = e;
                }

                V IEnumerator<V>.Current => _e.Current;

                void IDisposable.Dispose()
                {
                }

                object? IEnumerator.Current => _e.Current;

                bool IEnumerator.MoveNext()
                {
                    return _e.MoveNext();
                }

                void IEnumerator.Reset()
                {
                    throw new NotImplementedException();
                }
            }

            IEnumerator<V> IEnumerable<V>.GetEnumerator()
            {
                return new EnumerableImpl(GetEnumerator());
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                throw new NotImplementedException();
            }
        }

        public struct Enumerator
        {
            private readonly Stack<AvlNode>? _stack;
            private Node? _next;
            private Node? _current;

            public Enumerator(SmallDictionary<K, V> dict)
                : this()
            {
                var root = dict._root;
                if (root == null)
                {
                    return;
                }

                // left == right only if both are nulls
                if (root.Left == root.Right)
                {
                    _next = root;
                }
                else
                {
                    _stack = new Stack<AvlNode>(dict.HeightApprox());
                    _stack.Push(root);
                }
            }

            public KeyValuePair<K, V> Current => new KeyValuePair<K, V>(_current!.Key, _current!.Value);

            public bool MoveNext()
            {
                if (_next != null)
                {
                    _current = _next;
                    _next = _next.Next;
                    return true;
                }

                if (_stack == null || _stack.Count == 0)
                {
                    return false;
                }

                var curr = _stack.Pop();
                _current = curr;
                _next = curr.Next;

                PushIfNotNull(curr.Left);
                PushIfNotNull(curr.Right);

                return true;
            }

            private void PushIfNotNull(AvlNode? child)
            {
                if (child != null)
                {
                    _stack!.Push(child);
                }
            }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        public class EnumerableImpl : IEnumerator<KeyValuePair<K, V>>
        {
            private Enumerator _e;

            public EnumerableImpl(Enumerator e)
            {
                _e = e;
            }

            KeyValuePair<K, V> IEnumerator<KeyValuePair<K, V>>.Current => _e.Current;

            void IDisposable.Dispose()
            {
            }

            object IEnumerator.Current => _e.Current;

            bool IEnumerator.MoveNext()
            {
                return _e.MoveNext();
            }

            void IEnumerator.Reset()
            {
                throw new NotImplementedException();
            }
        }

        IEnumerator<KeyValuePair<K, V>> IEnumerable<KeyValuePair<K, V>>.GetEnumerator()
        {
            return new EnumerableImpl(GetEnumerator());
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        private int HeightApprox()
        {
            // height is less than 1.5 * depth(leftmost node)
            var h = 0;
            var cur = _root;
            while (cur != null)
            {
                h++;
                cur = cur.Left;
            }

            h = h + h / 2;
            return h;
        }
    }
}
