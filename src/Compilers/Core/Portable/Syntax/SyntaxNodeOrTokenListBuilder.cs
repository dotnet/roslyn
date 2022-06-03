// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Syntax
{
    internal class SyntaxNodeOrTokenListBuilder
    {
        private GreenNode?[] _nodes;
        private int _count;

        public SyntaxNodeOrTokenListBuilder(int size)
        {
            _nodes = new GreenNode?[size];
            _count = 0;
        }

        public static SyntaxNodeOrTokenListBuilder Create()
        {
            return new SyntaxNodeOrTokenListBuilder(8);
        }

        public int Count
        {
            get { return _count; }
        }

        public void Clear()
        {
            _count = 0;
        }

        public SyntaxNodeOrToken this[int index]
        {
            get
            {
                var innerNode = _nodes[index];
                RoslynDebug.Assert(innerNode is object);
                if (innerNode.IsToken == true)
                {
                    // getting internal token so we do not know the position
                    return new SyntaxNodeOrToken(null, innerNode, 0, 0);
                }
                else
                {
                    return innerNode.CreateRed();
                }
            }

            set
            {
                _nodes[index] = value.UnderlyingNode;
            }
        }

        internal void Add(GreenNode item)
        {
            if (_count >= _nodes.Length)
            {
                this.Grow(_count == 0 ? 8 : _nodes.Length * 2);
            }

            _nodes[_count++] = item;
        }

        public void Add(SyntaxNode item)
        {
            this.Add(item.Green);
        }

        public void Add(in SyntaxToken item)
        {
            RoslynDebug.Assert(item.Node is object);
            this.Add(item.Node);
        }

        public void Add(in SyntaxNodeOrToken item)
        {
            RoslynDebug.Assert(item.UnderlyingNode is object);
            this.Add(item.UnderlyingNode);
        }

        public void Add(SyntaxNodeOrTokenList list)
        {
            this.Add(list, 0, list.Count);
        }

        public void Add(SyntaxNodeOrTokenList list, int offset, int length)
        {
            if (_count + length > _nodes.Length)
            {
                this.Grow(_count + length);
            }

            list.CopyTo(offset, _nodes, _count, length);
            _count += length;
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
            _count--;
            _nodes[_count] = null;
        }

        private void Grow(int size)
        {
            var tmp = new GreenNode[size];
            Array.Copy(_nodes, tmp, _nodes.Length);
            _nodes = tmp;
        }

        public SyntaxNodeOrTokenList ToList()
        {
            if (_count > 0)
            {
                switch (_count)
                {
                    case 1:
                        if (_nodes[0]!.IsToken)
                        {
                            return new SyntaxNodeOrTokenList(
                                InternalSyntax.SyntaxList.List(new[] { _nodes[0]! }).CreateRed(),
                                index: 0);
                        }
                        else
                        {
                            return new SyntaxNodeOrTokenList(_nodes[0]!.CreateRed(), index: 0);
                        }
                    case 2:
                        return new SyntaxNodeOrTokenList(
                            InternalSyntax.SyntaxList.List(_nodes[0]!, _nodes[1]!).CreateRed(),
                            index: 0);
                    case 3:
                        return new SyntaxNodeOrTokenList(
                            InternalSyntax.SyntaxList.List(_nodes[0]!, _nodes[1]!, _nodes[2]!).CreateRed(),
                            index: 0);
                    default:
                        var tmp = new ArrayElement<GreenNode>[_count];
                        for (int i = 0; i < _count; i++)
                        {
                            tmp[i].Value = _nodes[i]!;
                        }

                        return new SyntaxNodeOrTokenList(InternalSyntax.SyntaxList.List(tmp).CreateRed(), index: 0);
                }
            }
            else
            {
                return default(SyntaxNodeOrTokenList);
            }
        }
    }
}
