// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    internal class SyntaxTokenListBuilder
    {
        private Syntax.InternalSyntax.CSharpSyntaxNode[] nodes;
        private int count;

        public SyntaxTokenListBuilder(int size)
        {
            this.nodes = new Syntax.InternalSyntax.CSharpSyntaxNode[size];
            this.count = 0;
        }

        public static SyntaxTokenListBuilder Create()
        {
            return new SyntaxTokenListBuilder(8);
        }

        public int Count
        {
            get
            {
                return this.count;
            }
        }

        public void Add(SyntaxToken item)
        {
            this.Add((Syntax.InternalSyntax.SyntaxToken)item.Node);
        }

        internal void Add(Syntax.InternalSyntax.SyntaxToken item)
        {
            CheckSpace(1);
            nodes[count++] = item;
        }

        public void Add(SyntaxTokenList list)
        {
            this.Add(list, 0, list.Count);
        }

        public void Add(SyntaxTokenList list, int offset, int length)
        {
            CheckSpace(length);
            list.CopyTo(offset, nodes, count, length);
            count += length;
        }

        public void Add(SyntaxToken[] list)
        {
            this.Add(list, 0, list.Length);
        }

        public void Add(SyntaxToken[] list, int offset, int length)
        {
            CheckSpace(length);
            for (int i = 0; i < length; i++)
            {
                this.nodes[count + i] = (InternalSyntax.SyntaxToken)list[offset + i].Node;
            }
            count += length;
        }

        private void CheckSpace(int delta)
        {
            var requiredSize = this.count + delta;
            if (requiredSize > this.nodes.Length)
            {
                this.Grow(requiredSize);
            }
        }

        private void Grow(int newSize)
        {
            var tmp = new Syntax.InternalSyntax.CSharpSyntaxNode[newSize];
            Array.Copy(nodes, tmp, nodes.Length);
            this.nodes = tmp;
        }

        public SyntaxTokenList ToList()
        {
            if (this.count > 0)
            {
                switch (this.count)
                {
                    case 1:
                        return new SyntaxTokenList(null, nodes[0], 0, 0);
                    case 2:
                        return new SyntaxTokenList(null, Syntax.InternalSyntax.SyntaxList.List(nodes[0], nodes[1]), 0, 0);
                    case 3:
                        return new SyntaxTokenList(null, Syntax.InternalSyntax.SyntaxList.List(nodes[0], nodes[1], nodes[2]), 0, 0);
                    default:
                        return new SyntaxTokenList(null, Syntax.InternalSyntax.SyntaxList.List(nodes, this.count), 0, 0);
                }
            }
            else
            {
                return default(SyntaxTokenList);
            }
        }

        public static implicit operator SyntaxTokenList(SyntaxTokenListBuilder builder)
        {
            return builder.ToList();
        }
    }
}