// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Syntax.InternalSyntax
{
    internal abstract class AbstractSyntaxListBuilder<TNode> where TNode : GreenNode
    {
        protected ArrayElement<TNode>[] Nodes;
        public int Count { get; private set; }

        protected AbstractSyntaxListBuilder(int size)
        {
            Nodes = new ArrayElement<TNode>[size];
        }

        public void Clear()
        {
            this.Count = 0;
        }

        public TNode this[int index]
        {
            get
            {
                return Nodes[index];
            }

            set
            {
                Nodes[index].Value = value;
            }
        }

        public void Add(TNode item)
        {
            if (item == null) return;

            if (item.IsList)
            {
                int slotCount = item.SlotCount;

                // Necessary, but not sufficient (e.g. for nested lists).
                EnsureAdditionalCapacity(slotCount);

                for (int i = 0; i < slotCount; i++)
                {
                    this.Add((TNode)item.GetSlot(i));
                }
            }
            else
            {
                EnsureAdditionalCapacity(1);

                Nodes[Count++].Value = item;
            }
        }

        public void AddRange(TNode[] items)
        {
            this.AddRange(items, 0, items.Length);
        }

        public void AddRange(TNode[] items, int offset, int length)
        {
            // Necessary, but not sufficient (e.g. for nested lists).
            EnsureAdditionalCapacity(length - offset);

            int oldCount = this.Count;

            for (int i = offset; i < length; i++)
            {
                Add(items[i]);
            }

            Validate(oldCount, this.Count);
        }

        [Conditional("DEBUG")]
        protected void Validate(int start, int end)
        {
            for (int i = start; i < end; i++)
            {
                Debug.Assert(Nodes[i].Value != null);
            }
        }

        //public void AddRange(SyntaxList<TNode> list)
        //{
        //    this.AddRange(list, 0, list.Count);
        //}

        //public void AddRange(SyntaxList<TNode> list, int offset, int length)
        //{
        //    // Necessary, but not sufficient (e.g. for nested lists).
        //    EnsureAdditionalCapacity(length - offset);

        //    int oldCount = this.Count;

        //    for (int i = offset; i < length; i++)
        //    {
        //        Add(list[i]);
        //    }

        //    Validate(oldCount, this.Count);
        //}

        //public void AddRange<TNode>(SyntaxList<TNode> list) where TNode : TNode
        //{
        //    this.AddRange(list, 0, list.Count);
        //}

        //public void AddRange<TNode>(SyntaxList<TNode> list, int offset, int length) where TNode : TNode
        //{
        //    this.AddRange(new SyntaxList<TNode>(list.Node), offset, length);
        //}

        internal void RemoveLast()
        {
            Count--;
            Nodes[Count].Value = null;
        }

        protected void EnsureAdditionalCapacity(int additionalCount)
        {
            int currentSize = Nodes.Length;
            int requiredSize = this.Count + additionalCount;

            if (requiredSize <= currentSize) return;

            int newSize =
                requiredSize < 8 ? 8 :
                requiredSize >= (int.MaxValue / 2) ? int.MaxValue :
                Math.Max(requiredSize, currentSize * 2); // NB: Size will *at least* double.
            Debug.Assert(newSize >= requiredSize);

            Array.Resize(ref Nodes, newSize);
        }

        public bool Any(int kind)
        {
            for (int i = 0; i < Count; i++)
            {
                if (Nodes[i].Value.RawKind == kind)
                {
                    return true;
                }
            }

            return false;
        }

        public TNode[] ToArray()
        {
            var array = new TNode[this.Count];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = Nodes[i];
            }

            return array;
        }

        internal abstract TNode ToListNode();
    }
}