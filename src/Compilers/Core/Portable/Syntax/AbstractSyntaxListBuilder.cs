// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Syntax
{
    internal abstract class AbstractSyntaxListBuilder
    {
        protected ArrayElement<GreenNode>[] Nodes;
        public int Count { get; private set; }

        protected AbstractSyntaxListBuilder(int size)
        {
            Nodes = new ArrayElement<GreenNode>[size];
            this.Count = 0;
        }

        public void Clear()
        {
            this.Count = 0;
        }

        public void Add(SyntaxNode item)
        {
            AddInternal(item.Green);
        }

        internal void AddInternal(GreenNode item)
        {
            if (item == null)
            {
                throw new ArgumentNullException();
            }

            if (Nodes == null || Count >= Nodes.Length)
            {
                this.Grow(Count == 0 ? 8 : Nodes.Length * 2);
            }

            Nodes[Count++].Value = item;
        }

        public void AddRange(SyntaxNode[] items)
        {
            this.AddRange(items, 0, items.Length);
        }

        public void AddRange(SyntaxNode[] items, int offset, int length)
        {
            if (Nodes == null || Count + length > Nodes.Length)
            {
                this.Grow(Count + length);
            }

            for (int i = offset, j = Count; i < offset + length; ++i, ++j)
            {
                Nodes[j].Value = items[i].Green;
            }

            int start = Count;
            Count += length;
            Validate(start, Count);
        }

        private void Validate(int start, int end)
        {
            for (int i = start; i < end; i++)
            {
                if (Nodes[i].Value == null)
                {
                    throw new ArgumentException("Cannot add a null CSharpSyntaxNode.");
                }
            }
        }

        public void AddRange(SyntaxList<SyntaxNode> list)
        {
            this.AddRange(list, 0, list.Count);
        }

        public void AddRange(SyntaxList<SyntaxNode> list, int offset, int count)
        {
            if (Nodes == null || this.Count + count > Nodes.Length)
            {
                this.Grow(Count + count);
            }

            var dst = this.Count;
            for (int i = offset, limit = offset + count; i < limit; i++)
            {
                Nodes[dst].Value = list.ItemInternal(i).Green;
                dst++;
            }

            int start = Count;
            Count += count;
            Validate(start, Count);
        }

        public void AddRange<TNode>(SyntaxList<TNode> list) where TNode : SyntaxNode
        {
            this.AddRange(list, 0, list.Count);
        }

        public void AddRange<TNode>(SyntaxList<TNode> list, int offset, int count) where TNode : SyntaxNode
        {
            this.AddRange(new SyntaxList<SyntaxNode>(list.Node), offset, count);
        }

        public void AddRange(SyntaxNodeOrTokenList list)
        {
            this.AddRange(list, 0, list.Count);
        }

        public void AddRange(SyntaxNodeOrTokenList list, int offset, int count)
        {
            if (Nodes == null || this.Count + count > Nodes.Length)
            {
                this.Grow(Count + count);
            }

            var dst = this.Count;
            for (int i = offset, limit = offset + count; i < limit; i++)
            {
                Nodes[dst].Value = list[i].UnderlyingNode;
                dst++;
            }

            int start = Count;
            Count += count;
            Validate(start, Count);
        }

        public void AddRange(SyntaxTokenList list)
        {
            this.AddRange(list, 0, list.Count);
        }

        public void AddRange(SyntaxTokenList list, int offset, int length)
        {
            this.AddRange(new SyntaxList<SyntaxNode>(list.Node.CreateRed()), offset, length);
        }

        private void Grow(int size)
        {
            var tmp = new ArrayElement<GreenNode>[size];
            Array.Copy(Nodes, tmp, Nodes.Length);
            Nodes = tmp;
        }

        public bool Any(int rawKind)
        {
            for (int i = 0; i < Count; i++)
            {
                if (Nodes[i].Value.RawKind == rawKind)
                {
                    return true;
                }
            }

            return false;
        }

        internal void RemoveLast()
        {
            this.Count -= 1;
            this.Nodes[this.Count] = default(ArrayElement<GreenNode>);
        }

        internal abstract GreenNode ToListNode();
    }
}