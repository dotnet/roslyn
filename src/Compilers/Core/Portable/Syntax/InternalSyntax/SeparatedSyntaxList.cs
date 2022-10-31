// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Syntax.InternalSyntax
{
    internal readonly struct SeparatedSyntaxList<TNode> : IEquatable<SeparatedSyntaxList<TNode>> where TNode : GreenNode
    {
        private readonly SyntaxList<GreenNode> _list;

        internal SeparatedSyntaxList(SyntaxList<GreenNode> list)
        {
            Validate(list);
            _list = list;
        }

        [Conditional("DEBUG")]
        private static void Validate(SyntaxList<GreenNode> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var item = list.GetRequiredItem(i);
                if ((i & 1) == 0)
                {
                    Debug.Assert(!item.IsToken, "even elements of a separated list must be nodes");
                }
                else
                {
                    Debug.Assert(item.IsToken, "odd elements of a separated list must be tokens");
                }
            }
        }

        internal GreenNode? Node => _list.Node;

        public int Count
        {
            get
            {
                return (_list.Count + 1) >> 1;
            }
        }

        public int SeparatorCount
        {
            get
            {
                return _list.Count >> 1;
            }
        }

        public TNode? this[int index]
        {
            get
            {
                return (TNode?)_list[index << 1];
            }
        }

        /// <summary>
        /// Gets the separator at the given index in this list.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        public GreenNode? GetSeparator(int index)
        {
            return _list[(index << 1) + 1];
        }

        public SyntaxList<GreenNode> GetWithSeparators()
        {
            return _list;
        }

        public static bool operator ==(in SeparatedSyntaxList<TNode> left, in SeparatedSyntaxList<TNode> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(in SeparatedSyntaxList<TNode> left, in SeparatedSyntaxList<TNode> right)
        {
            return !left.Equals(right);
        }

        public bool Equals(SeparatedSyntaxList<TNode> other)
        {
            return _list == other._list;
        }

        public override bool Equals(object? obj)
        {
            return (obj is SeparatedSyntaxList<TNode>) && Equals((SeparatedSyntaxList<TNode>)obj);
        }

        public override int GetHashCode()
        {
            return _list.GetHashCode();
        }

        public static implicit operator SeparatedSyntaxList<GreenNode>(SeparatedSyntaxList<TNode> list)
        {
            return new SeparatedSyntaxList<GreenNode>(list.GetWithSeparators());
        }

#if DEBUG
        [Obsolete("For debugging only", true)]
#pragma warning disable IDE0051 // Remove unused private members
        private TNode[] Nodes
#pragma warning restore IDE0051 // Remove unused private members
        {
            get
            {
                int count = this.Count;
                TNode[] array = new TNode[count];
                for (int i = 0; i < count; i++)
                {
                    array[i] = this[i]!;
                }
                return array;
            }
        }
#endif
    }
}
