// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Syntax
{
    internal class SyntaxListBuilder
    {
        private ArrayElement<GreenNode?>[] _nodes;
        public int Count { get; private set; }

        public SyntaxListBuilder(int size)
        {
            _nodes = new ArrayElement<GreenNode?>[size];
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

            if (Count >= _nodes.Length)
            {
                this.Grow(Count == 0 ? 8 : _nodes.Length * 2);
            }

            _nodes[Count++].Value = item;
        }

        public void AddRange(SyntaxNode[] items)
        {
            this.AddRange(items, 0, items.Length);
        }

        public void AddRange(SyntaxNode[] items, int offset, int length)
        {
            if (Count + length > _nodes.Length)
            {
                this.Grow(Count + length);
            }

            for (int i = offset, j = Count; i < offset + length; ++i, ++j)
            {
                _nodes[j].Value = items[i].Green;
            }

            int start = Count;
            Count += length;
            Validate(start, Count);
        }

        [Conditional("DEBUG")]
        private void Validate(int start, int end)
        {
            for (int i = start; i < end; i++)
            {
                if (_nodes[i].Value == null)
                {
                    throw new ArgumentException("Cannot add a null node.");
                }
            }
        }

        public void AddRange(SyntaxList<SyntaxNode> list)
        {
            this.AddRange(list, 0, list.Count);
        }

        public void AddRange(SyntaxList<SyntaxNode> list, int offset, int count)
        {
            if (this.Count + count > _nodes.Length)
            {
                this.Grow(Count + count);
            }

            var dst = this.Count;
            for (int i = offset, limit = offset + count; i < limit; i++)
            {
                _nodes[dst].Value = list.ItemInternal(i)!.Green;
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
            if (this.Count + count > _nodes.Length)
            {
                this.Grow(Count + count);
            }

            var dst = this.Count;
            for (int i = offset, limit = offset + count; i < limit; i++)
            {
                _nodes[dst].Value = list[i].UnderlyingNode;
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
            Debug.Assert(list.Node is object);
            this.AddRange(new SyntaxList<SyntaxNode>(list.Node.CreateRed()), offset, length);
        }

        private void Grow(int size)
        {
            var tmp = new ArrayElement<GreenNode?>[size];
            Array.Copy(_nodes, tmp, _nodes.Length);
            _nodes = tmp;
        }

        public bool Any(int kind)
        {
            for (int i = 0; i < Count; i++)
            {
                if (_nodes[i].Value!.RawKind == kind)
                {
                    return true;
                }
            }

            return false;
        }

        internal GreenNode? ToListNode()
        {
            switch (this.Count)
            {
                case 0:
                    return null;
                case 1:
                    return _nodes[0].Value;
                case 2:
                    return InternalSyntax.SyntaxList.List(_nodes[0].Value!, _nodes[1].Value!);
                case 3:
                    return InternalSyntax.SyntaxList.List(_nodes[0].Value!, _nodes[1].Value!, _nodes[2].Value!);
                default:
                    var tmp = new ArrayElement<GreenNode>[this.Count];
                    for (int i = 0; i < this.Count; i++)
                    {
                        tmp[i].Value = _nodes[i].Value!;
                    }

                    return InternalSyntax.SyntaxList.List(tmp);
            }
        }

        public static implicit operator SyntaxList<SyntaxNode>(SyntaxListBuilder builder)
        {
            if (builder == null)
            {
                return default(SyntaxList<SyntaxNode>);
            }

            return builder.ToList();
        }

        internal void RemoveLast()
        {
            this.Count -= 1;
            this._nodes[Count] = default;
        }
    }
}
