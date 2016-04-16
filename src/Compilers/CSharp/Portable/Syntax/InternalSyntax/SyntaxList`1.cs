// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal partial struct SyntaxList<TNode> : IEquatable<SyntaxList<TNode>> where TNode : CSharpSyntaxNode
    {
        private readonly CSharpSyntaxNode _node;

        internal SyntaxList(CSharpSyntaxNode node)
        {
            _node = node;
        }

        internal CSharpSyntaxNode Node
        {
            get
            {
                return _node;
            }
        }

        public int Count
        {
            get
            {
                return _node == null ? 0 : (_node.IsList ? _node.SlotCount : 1);
            }
        }

        public TNode this[int index]
        {
            get
            {
                if (_node == null)
                {
                    return null;
                }
                else if (_node.IsList)
                {
                    Debug.Assert(index >= 0);
                    Debug.Assert(index <= _node.SlotCount);

                    return (TNode)_node.GetSlot(index);
                }
                else if (index == 0)
                {
                    return (TNode)_node;
                }
                else
                {
                    throw ExceptionUtilities.Unreachable;
                }
            }
        }

        public bool Any()
        {
            return _node != null;
        }

        public bool Any(SyntaxKind kind)
        {
            foreach (var element in this)
            {
                if (element.Kind == kind)
                {
                    return true;
                }
            }

            return false;
        }

        // for debugging
        private TNode[] Nodes
        {
            get
            {
                var arr = new TNode[this.Count];
                for (int i = 0; i < this.Count; i++)
                {
                    arr[i] = this[i];
                }
                return arr;
            }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        internal void CopyTo(int offset, ArrayElement<CSharpSyntaxNode>[] array, int arrayOffset, int count)
        {
            for (int i = 0; i < count; i++)
            {
                array[arrayOffset + i].Value = this[i + offset];
            }
        }

        public static bool operator ==(SyntaxList<TNode> left, SyntaxList<TNode> right)
        {
            return left._node == right._node;
        }

        public static bool operator !=(SyntaxList<TNode> left, SyntaxList<TNode> right)
        {
            return left._node != right._node;
        }

        public bool Equals(SyntaxList<TNode> other)
        {
            return _node == other._node;
        }

        public override bool Equals(object obj)
        {
            return (obj is SyntaxList<TNode>) && Equals((SyntaxList<TNode>)obj);
        }

        public override int GetHashCode()
        {
            return _node != null ? _node.GetHashCode() : 0;
        }

        public SeparatedSyntaxList<TOther> AsSeparatedList<TOther>() where TOther : CSharpSyntaxNode
        {
            return new SeparatedSyntaxList<TOther>(this);
        }

        public static implicit operator SyntaxList<TNode>(TNode node)
        {
            return new SyntaxList<TNode>(node);
        }

        public static implicit operator SyntaxList<TNode>(SyntaxList<CSharpSyntaxNode> nodes)
        {
            return new SyntaxList<TNode>(nodes._node);
        }

        public static implicit operator SyntaxList<CSharpSyntaxNode>(SyntaxList<TNode> nodes)
        {
            return new SyntaxList<CSharpSyntaxNode>(nodes.Node);
        }
    }
}
