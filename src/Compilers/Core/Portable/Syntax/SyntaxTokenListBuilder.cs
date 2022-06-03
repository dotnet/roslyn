// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Syntax
{
    internal class SyntaxTokenListBuilder
    {
        private GreenNode?[] _nodes;
        private int _count;

        public SyntaxTokenListBuilder(int size)
        {
            _nodes = new GreenNode?[size];
            _count = 0;
        }

        public static SyntaxTokenListBuilder Create()
        {
            return new SyntaxTokenListBuilder(8);
        }

        public int Count
        {
            get
            {
                return _count;
            }
        }

        public void Add(SyntaxToken item)
        {
            Debug.Assert(item.Node is object);
            this.Add(item.Node);
        }

        internal void Add(GreenNode item)
        {
            CheckSpace(1);
            _nodes[_count++] = item;
        }

        public void Add(SyntaxTokenList list)
        {
            this.Add(list, 0, list.Count);
        }

        public void Add(SyntaxTokenList list, int offset, int length)
        {
            CheckSpace(length);
            list.CopyTo(offset, _nodes, _count, length);
            _count += length;
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
                _nodes[_count + i] = list[offset + i].Node;
            }
            _count += length;
        }

        private void CheckSpace(int delta)
        {
            var requiredSize = _count + delta;
            if (requiredSize > _nodes.Length)
            {
                this.Grow(requiredSize);
            }
        }

        private void Grow(int newSize)
        {
            var tmp = new GreenNode[newSize];
            Array.Copy(_nodes, tmp, _nodes.Length);
            _nodes = tmp;
        }

        public SyntaxTokenList ToList()
        {
            if (_count > 0)
            {
                switch (_count)
                {
                    case 1:
                        return new SyntaxTokenList(null, _nodes[0], 0, 0);
                    case 2:
                        Debug.Assert(_nodes[0] is object);
                        Debug.Assert(_nodes[1] is object);
                        return new SyntaxTokenList(null, InternalSyntax.SyntaxList.List(_nodes[0]!, _nodes[1]!), 0, 0);
                    case 3:
                        Debug.Assert(_nodes[0] is object);
                        Debug.Assert(_nodes[1] is object);
                        Debug.Assert(_nodes[2] is object);
                        return new SyntaxTokenList(null, InternalSyntax.SyntaxList.List(_nodes[0]!, _nodes[1]!, _nodes[2]!), 0, 0);
                    default:
                        return new SyntaxTokenList(null, InternalSyntax.SyntaxList.List(_nodes, _count), 0, 0);
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
