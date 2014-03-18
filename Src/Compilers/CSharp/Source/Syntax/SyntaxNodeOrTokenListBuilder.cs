// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    internal class SyntaxNodeOrTokenListBuilder
    {
        private Syntax.InternalSyntax.CSharpSyntaxNode[] nodes;
        private int count;

        public SyntaxNodeOrTokenListBuilder(int size)
        {
            this.nodes = new Syntax.InternalSyntax.CSharpSyntaxNode[size];
            this.count = 0;
        }

        public int Count
        {
            get { return count; }
        }

        public void Clear()
        {
            this.count = 0;
        }

        public SyntaxNodeOrToken this[int index]
        {
            get
            {
                var innerNode = this.nodes[index];
                var tk = innerNode as Syntax.InternalSyntax.SyntaxToken;
                if (tk != null)
                {
                    // getting internal token so we do not know the position
                    return new SyntaxNodeOrToken(null, tk, 0, 0);
                }
                else
                {
                    return innerNode.CreateRed();
                }
            }
            set
            {
                nodes[index] = (Syntax.InternalSyntax.CSharpSyntaxNode)value.UnderlyingNode;
            }
        }

        internal void Add(Syntax.InternalSyntax.CSharpSyntaxNode item)
        {
            if (nodes == null || count >= nodes.Length)
            {
                this.Grow(count == 0 ? 8 : nodes.Length * 2);
            }

            nodes[count++] = item;
        }

        public void Add(SyntaxNodeOrToken item)
        {
            this.Add((Syntax.InternalSyntax.CSharpSyntaxNode)item.UnderlyingNode);
        }

        public void Add(SyntaxNodeOrTokenList list)
        {
            this.Add(list, 0, list.Count);
        }

        public void Add(SyntaxNodeOrTokenList list, int offset, int length)
        {
            if (nodes == null || count + length > nodes.Length)
            {
                this.Grow(count + length);
            }

            list.CopyTo(offset, nodes, count, length);
            count += length;
        }

        public void Add(IEnumerable<SyntaxNodeOrToken> nodeOrTokens)
        {
            foreach (var n in nodeOrTokens)
            {
                this.Add(n);
            }
        }

        internal void RemoveLast()
        {
            count--;
            nodes[count] = null;
        }

        private void Grow(int size)
        {
            var tmp = new Syntax.InternalSyntax.CSharpSyntaxNode[size];
            Array.Copy(nodes, tmp, nodes.Length);
            this.nodes = tmp;
        }

        public SyntaxNodeOrTokenList ToList()
        {
            if (this.count > 0)
            {
                switch (this.count)
                {
                    case 1:
                        if (nodes[0].IsToken)
                        {
                            return new SyntaxNodeOrTokenList(
                                Syntax.InternalSyntax.SyntaxList.List(new[] { nodes[0] }).CreateRed(),
                                index: 0);
                        }
                        else
                        {
                            return new SyntaxNodeOrTokenList(nodes[0].CreateRed(), index: 0);
                        }
                    case 2:
                        return new SyntaxNodeOrTokenList(
                            Syntax.InternalSyntax.SyntaxList.List(nodes[0], nodes[1]).CreateRed(),
                            index: 0);
                    case 3:
                        return new SyntaxNodeOrTokenList(
                            Syntax.InternalSyntax.SyntaxList.List(nodes[0], nodes[1], nodes[2]).CreateRed(),
                            index: 0);
                    default:
                        var tmp = new ArrayElement<Syntax.InternalSyntax.CSharpSyntaxNode>[count];
                        for (int i = 0; i < this.count; i++)
                        {
                            tmp[i].Value = nodes[i];
                        }

                        return new SyntaxNodeOrTokenList(Syntax.InternalSyntax.SyntaxList.List(tmp).CreateRed(), index: 0);
                }
            }
            else
            {
                return default(SyntaxNodeOrTokenList);
            }
        }
    }
}
