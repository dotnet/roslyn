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
    //        At UShort.MaxValue elements, this dictionary is 6x slower to read and 4x slower 
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
            AvlNode q = this.root;

            if (q == null)
            {
                this.root = new AvlNode(hashCode, key, value);
                return;
            }

            AvlNode qparent = null;
            AvlNode PrimeNode = q;
            AvlNode PrimeNodeParent = null;

            // ====== insert new node
            // also make a note of the prime node and prime node's parent
            // Prime node is important because it is the node that may need rotation 
            // nodes on the search path from PrimeNode downwards will change balances because of the node added
            // we need prime node's parent for rotation since we cannot have reference locals in C#
            for (; ;)
            {
                // schedule hk read 
                var hk = q.HashCode;

                if (q.Balance != 0)
                {
                    PrimeNodeParent = qparent;
                    PrimeNode = q;
                }

                if (hk > hashCode)
                {
                    if (q.Left == null)
                    {
                        q.Left = q = new AvlNode(hashCode, key, value);
                        break;
                    }
                    qparent = q;
                    q = q.Left;
                }
                else if (hk < hashCode)
                {
                    if (q.Right == null)
                    {
                        q.Right = q = new AvlNode(hashCode, key, value);
                        break;
                    }
                    qparent = q;
                    q = q.Right;
                }
                else // (p.HashCode == hashCode)
                {
                    this.HandleInsert(q, qparent, key, value, add);
                    return;
                }
            }

            Debug.Assert(PrimeNode != q);

            // ====== update balances on the path from PrimeNode downwards
            var p = PrimeNode;
            do
            {
                Debug.Assert(p.HashCode != hashCode);

                if (p.HashCode < hashCode)
                {
                    p.Balance--;
                    p = p.Right;
                }
                else
                {
                    p.Balance++;
                    p = p.Left;
                }
            }
            while (p != q);

            // ====== rotate subtree at PrimeNode if needed
            var primeBalance = PrimeNode.Balance;
            //if (Math.Abs(primeBalance) == 2)
            if (((primeBalance + 2) & 3) == 0)
            {
                var rotated = FixWithRotate(PrimeNode, primeBalance);

                if (PrimeNodeParent == null)
                {
                    this.root = rotated;
                }
                else if (PrimeNode == PrimeNodeParent.Left)
                {
                    PrimeNodeParent.Left = rotated;
                }
                else
                {
                    PrimeNodeParent.Right = rotated;
                }
            }
        }

        private void HandleInsert(AvlNode q, AvlNode qparent, K key, V value, bool add)
        {
            Node n = q;
            do
            {
                if (CompareKeys(n.key, key))
                {
                    if (add)
                    {
                        throw new InvalidOperationException();
                    }
                    else
                    {
                        n.Value = value;
                        return;
                    }
                }

                n = n.Next;
            } while (n != null);

            AddNode(q, qparent, key, value);
        }

        private void AddNode(AvlNode q, AvlNode qparent, K key, V value)
        {
            AvlNodeHead head = q as AvlNodeHead;
            if (head != null)
            {
                var newNext = new NodeLinked(key, value, head.next);
                head.next = newNext;
            }
            else
            {
                var newHead = new AvlNodeHead(q.HashCode, key, value, q);
                newHead.Balance = q.Balance;
                newHead.Left = q.Left;
                newHead.Right = q.Right;

                if (qparent == null)
                {
                    root = newHead;
                }
                else
                {
                    if (q == qparent.Left)
                    {
                        qparent.Left = newHead;
                    }
                    else
                    {
                        qparent.Right = newHead;
                    }
                }
            }
        }

        private static AvlNode FixWithRotate(AvlNode node, int direction)
        {
            if (direction > 0)
            {
                return FixWithRotateRight(node);
            }
            else
            {
                return FixWithRotateLeft(node);
            }
        }

        private static AvlNode FixWithRotateLeft(AvlNode N)
        {
            var R = N.Right;
            var RL = R.Left;

            switch (R.Balance)
            {
                case -1:
                    N.Balance = 0;
                    R.Balance = 0;
                    N.Right = RL;
                    R.Left = N;
                    return R;

                case 0:
                    N.Balance = -1;
                    R.Balance = 1;
                    N.Right = RL;
                    R.Left = N;
                    return R;

                case 1:
                    R.Left = RL.Right;
                    N.Right = RL.Left;

                    var nRLBalance = (sbyte)-RL.Balance;
                    if (nRLBalance > 0)
                    {
                        R.Balance = 0;
                        N.Balance = nRLBalance;
                    }
                    else
                    {
                        R.Balance = nRLBalance;
                        N.Balance = 0;
                    }

                    RL.Balance = 0;
                    RL.Right = R;
                    RL.Left = N;

                    return RL;
            }

            return null;
        }

        private static AvlNode FixWithRotateRight(AvlNode N)
        {
            var L = N.Left;
            var LR = L.Right;

            switch (L.Balance)
            {
                case 1:
                    N.Balance = 0;
                    L.Balance = 0;
                    N.Left = LR;
                    L.Right = N;
                    return L;

                case 0:
                    N.Balance = 1;
                    L.Balance = -1;
                    N.Left = LR;
                    L.Right = N;
                    return L;

                case -1:
                    L.Right = LR.Left;
                    N.Left = LR.Right;

                    var nLRBalance = (sbyte)-LR.Balance;
                    if (nLRBalance > 0)
                    {
                        L.Balance = nLRBalance;
                        N.Balance = 0;
                    }
                    else
                    {
                        L.Balance = 0;
                        N.Balance = nLRBalance;
                    }

                    LR.Balance = 0;
                    LR.Left = L;
                    LR.Right = N;

                    return LR;
            }

            return null;
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
