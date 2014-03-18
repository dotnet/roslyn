// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    internal class SyntaxTriviaListBuilder
    {
        private SyntaxTrivia[] nodes;
        private int count;
        private SyntaxTrivia[] previous;

        public SyntaxTriviaListBuilder(int size)
        {
            this.nodes = new SyntaxTrivia[size];
        }

        public static SyntaxTriviaListBuilder Create()
        {
            return new SyntaxTriviaListBuilder(4);
        }

        public int Count
        {
            get { return count; }
        }

        public void Clear()
        {
            this.count = 0;
        }

        public SyntaxTrivia this[int index]
        {
            get
            {
                if (index < 0 || index > this.count)
                {
                    throw new IndexOutOfRangeException();
                }

                return this.nodes[index];
            }
        }

        public SyntaxTriviaListBuilder Add(SyntaxTrivia item)
        {
            if (nodes == null || count >= nodes.Length)
            {
                this.Grow(count == 0 ? 8 : nodes.Length * 2);
            }

            nodes[count++] = item;
            return this;
        }

        public void Add(SyntaxTrivia[] items)
        {
            this.Add(items, 0, items.Length);
        }

        public void Add(SyntaxTrivia[] items, int offset, int length)
        {
            if (nodes == null || count + length > nodes.Length)
            {
                this.Grow(count + length);
            }

            Array.Copy(items, offset, nodes, count, length);
            count += length;
        }

        public void Add(SyntaxTriviaList list)
        {
            this.Add(list, 0, list.Count);
        }

        public void Add(SyntaxTriviaList list, int offset, int length)
        {
            if (nodes == null || count + length > nodes.Length)
            {
                this.Grow(count + length);
            }

            list.CopyTo(offset, nodes, count, length);
            count += length;
        }

        private void Grow(int size)
        {
            var tmp = new SyntaxTrivia[size];
            if (previous != null)
            {
                Array.Copy(previous, tmp, this.count);
                this.previous = null;
            }
            else
            {
                Array.Copy(nodes, tmp, nodes.Length);
            }

            this.nodes = tmp;
        }

        public static implicit operator SyntaxTriviaList(SyntaxTriviaListBuilder builder)
        {
            return builder.ToList();
        }

        public SyntaxTriviaList ToList()
        {
            if (this.count > 0)
            {
                if (this.previous != null)
                {
                    this.Grow(this.count);
                }

                switch (this.count)
                {
                    case 1:
                        return new SyntaxTriviaList(default(SyntaxToken), nodes[0].UnderlyingNode, position: 0, index: 0);
                    case 2:
                        return new SyntaxTriviaList(default(SyntaxToken), Syntax.InternalSyntax.SyntaxList.List(
                            (Syntax.InternalSyntax.CSharpSyntaxNode)nodes[0].UnderlyingNode,
                            (Syntax.InternalSyntax.CSharpSyntaxNode)nodes[1].UnderlyingNode), position: 0, index: 0);
                    case 3:
                        return new SyntaxTriviaList(default(SyntaxToken),
                            Syntax.InternalSyntax.SyntaxList.List(
                                (Syntax.InternalSyntax.CSharpSyntaxNode)nodes[0].UnderlyingNode,
                                (Syntax.InternalSyntax.CSharpSyntaxNode)nodes[1].UnderlyingNode,
                                (Syntax.InternalSyntax.CSharpSyntaxNode)nodes[2].UnderlyingNode),
                            position: 0, index: 0);
                    default:
                        {
                            var tmp = new ArrayElement<Syntax.InternalSyntax.CSharpSyntaxNode>[count];
                            for (int i = 0; i < this.count; i++)
                            {
                                tmp[i].Value = (Syntax.InternalSyntax.CSharpSyntaxNode)this.nodes[i].UnderlyingNode;
                            }

                            return new SyntaxTriviaList(default(SyntaxToken), Syntax.InternalSyntax.SyntaxList.List(tmp), position: 0, index: 0);
                        }
                }
            }
            else
            {
                return default(SyntaxTriviaList);
            }
        }
    }
}