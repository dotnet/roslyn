// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    // Dictionary designed to hold small number of items.
    // Compared to the regular Dictionary, average overhead per-item is roughly the same, but 
    // unlike regular dictionary, this one is based on an AVL tree and as such does not require 
    // rehashing when items are added.
    // It does require rebalancing, but that is allocation-free.
    //
    // Major caveats:
    //  1) There is no Remove method. (can be added, but we do not seem to use Remove that much)
    //  2) foreach [keys|values|pairs] may allocate a small array.
    //  3) Performance is no longer O(1). At a certain count it becomes slower than regular Dictionary.
    //     In comparison to regular Dictionary on my machine:
    //        On trivial number of elements (5 or so) it is more than 2x faster.
    //        The break even count is about 120 elements for read and 55 for write operations (with unknown initial size).
    //        At UShort.MaxValue elements, this dictionary is 6x slower to read and 4x slower to write
    //
    // Generally, this dictionary is a win if number of elements is small, not known beforehand or both.
    //
    // If the size of the dictionary is known at creation and it is likely to contain more than 10 elements, 
    // then regular Dictionary is a better choice.
    //
    internal sealed class SmallDictionary<K, V> : IEnumerable<KeyValuePair<K, V>>
    {
        private AvlNode root = null;
        private readonly IEqualityComparer<K> comparer;

        public static readonly SmallDictionary<K, V> Empty = new SmallDictionary<K, V>(null);

        public SmallDictionary() : this(EqualityComparer<K>.Default) { }

        public SmallDictionary(IEqualityComparer<K> comparer)
        {
            this.comparer = comparer;
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
            return this.comparer.Equals(k1, k2);
        }

        private int GetHashCode(K k)
        {
            return this.comparer.GetHashCode(k);
        }

        public bool TryGetValue(K key, out V value)
        {
            if (root != null)
            {
                int hash = GetHashCode(key);
                return TryGetValue(hash, key, out value);
            }

            value = default(V);
            return false;
        }

        public void Add(K key, V value)
        {
            int hash = GetHashCode(key);
            Insert(hash, key, value, add: true);
        }

        public V this[K key]
        {
            get
            {
                V value;
                if (!TryGetValue(key, out value))
                {
                    throw new InvalidOperationException("key not found");
                }
                return value;
            }
            set
            {
                int hash = GetHashCode(key);
                this.Insert(hash, key, value, add: false);
            }
        }

        public bool ContainsKey(K key)
        {
            V value;
            return TryGetValue(key, out value);
        }

        [Conditional("DEBUG")]
        internal void AssertBalanced()
        {
#if DEBUG
            AvlNode.AssertBalanced(root);
#endif
        }

        private class Node
        {
            public readonly K key;
            public V Value;

            public Node(K key, V value)
            {
                this.key = key;
                this.Value = value;
            }

            public virtual Node Next
            {
                get
                {
                    return null;
                }
            }
        }

        private class NodeLinked : Node
        {
            public readonly Node next;

            public NodeLinked(K key, V value, Node next)
                : base(key, value)
            {
                this.next = next;
            }

            public override Node Next
            {
                get
                {
                    return next;
                }
            }
        }

        private class AvlNodeHead : AvlNode
        {
            public Node next;

            public AvlNodeHead(int hashCode, K key, V value, Node next)
                : base(hashCode, key, value)
            {
                this.next = next;
            }

            public override Node Next
            {
                get
                {
                    return next;
                }
            }
        }

        // separate class to ensure that HashCode field 
        // is layed out in ram  before other AvlNode fields
        private class HashedNode : Node
        {
            public readonly int HashCode;

            public HashedNode(int hashCode, K key, V value)
                : base(key, value)
            {
                this.HashCode = hashCode;
            }
        }

        private class AvlNode : HashedNode
        {
            public AvlNode Left;
            public AvlNode Right;
            public sbyte Balance;

            public AvlNode(int hashCode, K key, V value)
                : base(hashCode, key, value)
            { }

#if DEBUG
            public static int AssertBalanced(AvlNode V)
            {
                if (V == null) return 0;

                int a = AssertBalanced(V.Left);
                int b = AssertBalanced(V.Right);

                if (((a - b) != V.Balance) || (Math.Abs(a - b) >= 2))
                {
                    throw new InvalidOperationException();
                }

                return 1 + Math.Max(a, b);
            }
#endif
        }

        private bool TryGetValue(int hashCode, K key, out V value)
        {
            AvlNode b = root;

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

            value = default(V);
            return false;

            hasBucket:
            if (CompareKeys(b.key, key))
            {
                value = b.Value;
                return true;
            }

            return GetFromList(b.Next, key, out value);
        }

        private bool GetFromList(Node next, K key, out V value)
        {
            while (next != null)
            {
                if (CompareKeys(key, next.key))
                {
                    value = next.Value;
                    return true;
                }

                next = next.Next;
            }

            value = default(V);
            return false;
        }

        private void Insert(int hashCode, K key, V value, bool add)
        {
            AvlNode currentNode = this.root;

            if (currentNode == null)
            {
                this.root = new AvlNode(hashCode, key, value);
                return;
            }

            AvlNode currentNodeParent = null;
            AvlNode unbalanced = currentNode;
            AvlNode unbalancedParent = null;

            // ====== insert new node
            // also make a note of the last unbalanced node and its parent (for rotation if needed)
            // nodes on the search path from rotation candidate downwards will change balances because of the node added
            // unbalanced node itself will become balanced or will be rotated
            // either way nodes above unbalanced do not change their balance
            for (; ;)
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
                        currentNode.Left = currentNode = new AvlNode(hashCode, key, value);
                        break;
                    }
                    currentNodeParent = currentNode;
                    currentNode = currentNode.Left;
                }
                else if (hc < hashCode)
                {
                    if (currentNode.Right == null)
                    {
                        currentNode.Right = currentNode = new AvlNode(hashCode, key, value);
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
                    n = n.Right;
                }
                else
                {
                    n.Balance++;
                    n = n.Left;
                }
            }
            while (n != currentNode);

            // ====== rotate unbalanced node if needed
            AvlNode rotated = null;
            var balance = unbalanced.Balance;
            if (balance == -2)
            {
                rotated = unbalanced.Right.Balance < 0 ?
                    LeftSimple(unbalanced) :
                    LeftComplex(unbalanced);
            }
            else if (balance == 2)
            {
                rotated = unbalanced.Left.Balance > 0 ?
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
                this.root = rotated;
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

        private AvlNode LeftSimple(AvlNode unbalanced)
        {
            var right = unbalanced.Right;
            unbalanced.Right = right.Left;
            right.Left = unbalanced;

            unbalanced.Balance = 0;
            right.Balance = 0;
            return right;
        }

        private AvlNode RightSimple(AvlNode unbalanced)
        {
            var left = unbalanced.Left;
            unbalanced.Left = left.Right;
            left.Right = unbalanced;

            unbalanced.Balance = 0;
            left.Balance = 0;
            return left;
        }

        private AvlNode LeftComplex(AvlNode unbalanced)
        {
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

        private AvlNode RightComplex(AvlNode unbalanced)
        {
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


        private void HandleInsert(AvlNode node, AvlNode parent, K key, V value, bool add)
        {
            Node currentNode = node;
            do
            {
                if (CompareKeys(currentNode.key, key))
                {
                    if (add)
                    {
                        throw new InvalidOperationException();
                    }
                    else
                    {
                        currentNode.Value = value;
                        return;
                    }
                }

                currentNode = currentNode.Next;
            } while (currentNode != null);

            AddNode(node, parent, key, value);
        }

        private void AddNode(AvlNode node, AvlNode parent, K key, V value)
        {
            AvlNodeHead head = node as AvlNodeHead;
            if (head != null)
            {
                var newNext = new NodeLinked(key, value, head.next);
                head.next = newNext;
            }
            else
            {
                var newHead = new AvlNodeHead(node.HashCode, key, value, node);
                newHead.Balance = node.Balance;
                newHead.Left = node.Left;
                newHead.Right = node.Right;

                if (parent == null)
                {
                    root = newHead;
                }
                else
                {
                    if (node == parent.Left)
                    {
                        parent.Left = newHead;
                    }
                    else
                    {
                        parent.Right = newHead;
                    }
                }
            }
        }

        public KeyCollection Keys
        {
            get
            {
                return new KeyCollection(this);
            }
        }

        internal struct KeyCollection : IEnumerable<K>
        {
            private readonly SmallDictionary<K, V> dict;

            public KeyCollection(SmallDictionary<K, V> dict)
            {
                this.dict = dict;
            }

            public struct Enumerator
            {
                private readonly Stack<AvlNode> stack;
                private Node next;
                private Node current;

                public Enumerator(SmallDictionary<K, V> dict)
                    : this()
                {
                    var root = dict.root;
                    if (root != null)
                    {
                        // left == right only if both are nulls
                        if (root.Left == root.Right)
                        {
                            next = dict.root;
                        }
                        else
                        {
                            stack = new Stack<AvlNode>(dict.HeightApprox());
                            stack.Push(dict.root);
                        }
                    }
                }

                public K Current
                {
                    get
                    {
                        return current.key;
                    }
                }

                public bool MoveNext()
                {
                    if (next != null)
                    {
                        current = next;
                        next = next.Next;
                        return true;
                    }

                    if (stack != null && stack.Count != 0)
                    {
                        var curr = stack.Pop();
                        current = curr;
                        next = curr.Next;

                        PushIfNotNull(curr.Left);
                        PushIfNotNull(curr.Right);

                        return true;
                    }

                    return false;
                }

                private void PushIfNotNull(AvlNode child)
                {
                    if (child != null)
                    {
                        stack.Push(child);
                    }
                }
            }

            public Enumerator GetEnumerator()
            {
                return new Enumerator(this.dict);
            }

            public class EnumerableImpl : IEnumerator<K>
            {
                private Enumerator e;

                public EnumerableImpl(Enumerator e)
                {
                    this.e = e;
                }

                K IEnumerator<K>.Current
                {
                    get { return e.Current; }
                }

                void IDisposable.Dispose()
                {
                }

                object System.Collections.IEnumerator.Current
                {
                    get { return e.Current; }
                }

                bool System.Collections.IEnumerator.MoveNext()
                {
                    return e.MoveNext();
                }

                void System.Collections.IEnumerator.Reset()
                {
                    throw new NotSupportedException();
                }
            }

            IEnumerator<K> IEnumerable<K>.GetEnumerator()
            {
                return new EnumerableImpl(GetEnumerator());
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                throw new NotImplementedException();
            }
        }

        public ValueCollection Values
        {
            get
            {
                return new ValueCollection(this);
            }
        }

        internal struct ValueCollection : IEnumerable<V>
        {
            private readonly SmallDictionary<K, V> dict;

            public ValueCollection(SmallDictionary<K, V> dict)
            {
                this.dict = dict;
            }

            public struct Enumerator
            {
                private readonly Stack<AvlNode> stack;
                private Node next;
                private Node current;

                public Enumerator(SmallDictionary<K, V> dict)
                    : this()
                {
                    var root = dict.root;
                    if (root != null)
                    {
                        // left == right only if both are nulls
                        if (root.Left == root.Right)
                        {
                            next = dict.root;
                        }
                        else
                        {
                            stack = new Stack<AvlNode>(dict.HeightApprox());
                            stack.Push(dict.root);
                        }
                    }
                }

                public V Current
                {
                    get
                    {
                        return current.Value;
                    }
                }

                public bool MoveNext()
                {
                    if (next != null)
                    {
                        current = next;
                        next = next.Next;
                        return true;
                    }

                    if (stack != null && stack.Count != 0)
                    {
                        var curr = stack.Pop();
                        current = curr;
                        next = curr.Next;

                        PushIfNotNull(curr.Left);
                        PushIfNotNull(curr.Right);

                        return true;
                    }

                    return false;
                }

                private void PushIfNotNull(AvlNode child)
                {
                    if (child != null)
                    {
                        stack.Push(child);
                    }
                }
            }

            public Enumerator GetEnumerator()
            {
                return new Enumerator(this.dict);
            }

            public class EnumerableImpl : IEnumerator<V>
            {
                private Enumerator e;

                public EnumerableImpl(Enumerator e)
                {
                    this.e = e;
                }

                V IEnumerator<V>.Current
                {
                    get { return e.Current; }
                }

                void IDisposable.Dispose()
                {
                }

                object System.Collections.IEnumerator.Current
                {
                    get { return e.Current; }
                }

                bool System.Collections.IEnumerator.MoveNext()
                {
                    return e.MoveNext();
                }

                void System.Collections.IEnumerator.Reset()
                {
                    throw new NotImplementedException();
                }
            }

            IEnumerator<V> IEnumerable<V>.GetEnumerator()
            {
                return new EnumerableImpl(GetEnumerator());
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                throw new NotImplementedException();
            }
        }


        public struct Enumerator
        {
            private readonly Stack<AvlNode> stack;
            private Node next;
            private Node current;

            public Enumerator(SmallDictionary<K, V> dict)
                : this()
            {
                var root = dict.root;
                if (root != null)
                {
                    // left == right only if both are nulls
                    if (root.Left == root.Right)
                    {
                        next = dict.root;
                    }
                    else
                    {
                        stack = new Stack<AvlNode>(dict.HeightApprox());
                        stack.Push(dict.root);
                    }
                }
            }

            public KeyValuePair<K, V> Current
            {
                get
                {
                    return new KeyValuePair<K, V>(current.key, current.Value);
                }
            }

            public bool MoveNext()
            {
                if (next != null)
                {
                    current = next;
                    next = next.Next;
                    return true;
                }

                if (stack != null && stack.Count != 0)
                {
                    var curr = stack.Pop();
                    current = curr;
                    next = curr.Next;

                    PushIfNotNull(curr.Left);
                    PushIfNotNull(curr.Right);

                    return true;
                }

                return false;
            }

            private void PushIfNotNull(AvlNode child)
            {
                if (child != null)
                {
                    stack.Push(child);
                }
            }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        public class EnumerableImpl : IEnumerator<KeyValuePair<K, V>>
        {
            private Enumerator e;

            public EnumerableImpl(Enumerator e)
            {
                this.e = e;
            }

            KeyValuePair<K, V> IEnumerator<KeyValuePair<K, V>>.Current
            {
                get { return e.Current; }
            }

            void IDisposable.Dispose()
            {
            }

            object System.Collections.IEnumerator.Current
            {
                get { return e.Current; }
            }

            bool System.Collections.IEnumerator.MoveNext()
            {
                return e.MoveNext();
            }

            void System.Collections.IEnumerator.Reset()
            {
                throw new NotImplementedException();
            }
        }

        IEnumerator<KeyValuePair<K, V>> IEnumerable<KeyValuePair<K, V>>.GetEnumerator()
        {
            return new EnumerableImpl(GetEnumerator());
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        private int HeightApprox()
        {
            // height is less than 1.5 * depth(leftmost node)
            var h = 0;
            var cur = this.root;
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
