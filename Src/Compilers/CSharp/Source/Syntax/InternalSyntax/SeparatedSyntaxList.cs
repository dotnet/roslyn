// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal struct SeparatedSyntaxList<TNode> where TNode : CSharpSyntaxNode
    {
        private readonly SyntaxList<CSharpSyntaxNode> list;

        internal SeparatedSyntaxList(SyntaxList<CSharpSyntaxNode> list)
        {
            Validate(list);
            this.list = list;
        }

        [Conditional("DEBUG")]
        private static void Validate(SyntaxList<CSharpSyntaxNode> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                CSharpSyntaxNode item = list[i];
                if ((i & 1) == 0)
                {
                    Debug.Assert(!(item is SyntaxToken), "even elements of a separated list must be nodes");
                }
                else
                {
                    Debug.Assert(item is SyntaxToken, "odd elements of a separated list must be tokens");
                }
            }
        }

        internal CSharpSyntaxNode Node
        {
            get
            {
                return list.Node;
            }
        }

        public int Count
        {
            get
            {
                return (this.list.Count + 1) >> 1;
            }
        }

        public int SeparatorCount
        {
            get
            {
                return this.list.Count >> 1;
            }
        }

        public TNode this[int index]
        {
            get
            {
                return (TNode)this.list[index << 1];
            }
        }

        /// <summary>
        /// Gets the separator at the given index in this list.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        public SyntaxToken GetSeparator(int index)
        {
            return (SyntaxToken)this.list[(index << 1) + 1];
        }

        public SyntaxList<CSharpSyntaxNode> GetWithSeparators()
        {
            return this.list;
        }

        public static bool operator ==(SeparatedSyntaxList<TNode> left, SeparatedSyntaxList<TNode> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SeparatedSyntaxList<TNode> left, SeparatedSyntaxList<TNode> right)
        {
            return !left.Equals(right);
        }

        public bool Equals(SeparatedSyntaxList<TNode> other)
        {
            return this.list == other.list;
        }

        public override bool Equals(object obj)
        {
            return (obj is SeparatedSyntaxList<TNode>) && Equals((SeparatedSyntaxList<TNode>)obj);
        }

        public override int GetHashCode()
        {
            return this.list.GetHashCode();
        }

#if DEBUG
        [Obsolete("For debugging only", true)]
        private TNode[] Nodes
        {
            get
            {
                int count = this.Count;
                TNode[] array = new TNode[count];
                for (int i = 0; i < count; i++)
                {
                    array[i] = this[i];
                }
                return array;
            }
        }
#endif
    }
}