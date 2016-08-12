// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    internal class SyntaxListBuilder : AbstractSyntaxListBuilder
    {
        private ArrayElement<GreenNode>[] Nodes;
        public int Count { get; private set; }

        public SyntaxListBuilder(int size)
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

        public bool Any(SyntaxKind kind)
        {
            for (int i = 0; i < Count; i++)
            {
                if (Nodes[i].Value.RawKind == (int)kind)
                {
                    return true;
                }
            }

            return false;
        }

        internal Syntax.InternalSyntax.CSharpSyntaxNode ToListNode()
        {
            switch (this.Count)
            {
                case 0:
                    return null;
                case 1:
                    return (Syntax.InternalSyntax.CSharpSyntaxNode)Nodes[0].Value;
                case 2:
                    return Syntax.InternalSyntax.SyntaxList.List((Syntax.InternalSyntax.CSharpSyntaxNode)Nodes[0].Value, (Syntax.InternalSyntax.CSharpSyntaxNode)Nodes[1].Value);
                case 3:
                    return Syntax.InternalSyntax.SyntaxList.List((Syntax.InternalSyntax.CSharpSyntaxNode)Nodes[0].Value, (Syntax.InternalSyntax.CSharpSyntaxNode)Nodes[1].Value, (Syntax.InternalSyntax.CSharpSyntaxNode)Nodes[2].Value);
                default:
                    var tmp = new ArrayElement<Syntax.InternalSyntax.CSharpSyntaxNode>[this.Count];
                    for (int i = 0; i < this.Count; i++)
                    {
                        tmp[i].Value = (Syntax.InternalSyntax.CSharpSyntaxNode)Nodes[i].Value;
                    }

                    return Syntax.InternalSyntax.SyntaxList.List(tmp);
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
    }
}
