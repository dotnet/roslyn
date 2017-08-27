// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Syntax.InternalSyntax
{
    internal abstract partial class SyntaxList : GreenNode
    {
        internal SyntaxList()
            : base(GreenNode.ListKind)
        {
        }

        internal SyntaxList(DiagnosticInfo[] diagnostics, SyntaxAnnotation[] annotations)
            : base(GreenNode.ListKind, diagnostics, annotations)
        {
        }

        internal SyntaxList(ObjectReader reader)
            : base(reader)
        {
        }

        internal static GreenNode List(GreenNode child)
        {
            return child;
        }

        internal static WithTwoChildren List(GreenNode child0, GreenNode child1)
        {
            Debug.Assert(child0 != null);
            Debug.Assert(child1 != null);

            int hash;
            GreenNode cached = SyntaxNodeCache.TryGetNode(GreenNode.ListKind, child0, child1, out hash);
            if (cached != null)
                return (WithTwoChildren)cached;

            var result = new WithTwoChildren(child0, child1);
            if (hash >= 0)
            {
                SyntaxNodeCache.AddNode(result, hash);
            }

            return result;
        }

        internal static WithThreeChildren List(GreenNode child0, GreenNode child1, GreenNode child2)
        {
            Debug.Assert(child0 != null);
            Debug.Assert(child1 != null);
            Debug.Assert(child2 != null);

            int hash;
            GreenNode cached = SyntaxNodeCache.TryGetNode(GreenNode.ListKind, child0, child1, child2, out hash);
            if (cached != null)
                return (WithThreeChildren)cached;

            var result = new WithThreeChildren(child0, child1, child2);
            if (hash >= 0)
            {
                SyntaxNodeCache.AddNode(result, hash);
            }

            return result;
        }

        internal static GreenNode List(GreenNode[] nodes)
        {
            return List(nodes, nodes.Length);
        }

        internal static GreenNode List(GreenNode[] nodes, int count)
        {
            var array = new ArrayElement<GreenNode>[count];
            for (int i = 0; i < count; i++)
            {
                Debug.Assert(nodes[i] != null);
                array[i].Value = nodes[i];
            }

            return List(array);
        }

        internal static SyntaxList List(ArrayElement<GreenNode>[] children)
        {
            // "WithLotsOfChildren" list will allocate a separate array to hold
            // precomputed node offsets. It may not be worth it for smallish lists.
            if (children.Length < 10)
            {
                return new WithManyChildren(children);
            }
            else
            {
                return new WithLotsOfChildren(children);
            }
        }

        internal abstract void CopyTo(ArrayElement<GreenNode>[] array, int offset);

        internal static GreenNode Concat(GreenNode left, GreenNode right)
        {
            if (left == null)
            {
                return right;
            }

            if (right == null)
            {
                return left;
            }

            var leftList = left as SyntaxList;
            var rightList = right as SyntaxList;
            if (leftList != null)
            {
                if (rightList != null)
                {
                    var tmp = new ArrayElement<GreenNode>[left.SlotCount + right.SlotCount];
                    leftList.CopyTo(tmp, 0);
                    rightList.CopyTo(tmp, left.SlotCount);
                    return List(tmp);
                }
                else
                {
                    var tmp = new ArrayElement<GreenNode>[left.SlotCount + 1];
                    leftList.CopyTo(tmp, 0);
                    tmp[left.SlotCount].Value = right;
                    return List(tmp);
                }
            }
            else if (rightList != null)
            {
                var tmp = new ArrayElement<GreenNode>[rightList.SlotCount + 1];
                tmp[0].Value = left;
                rightList.CopyTo(tmp, 1);
                return List(tmp);
            }
            else
            {
                return List(left, right);
            }
        }

        public sealed override string Language
        {
            get
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        public sealed override string KindText
        {
            get
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        public sealed override SyntaxNode GetStructure(SyntaxTrivia parentTrivia)
        {
            throw ExceptionUtilities.Unreachable;
        }

        public sealed override SyntaxToken CreateSeparator<TNode>(SyntaxNode element)
        {
            throw ExceptionUtilities.Unreachable;
        }

        public sealed override bool IsTriviaWithEndOfLine()
        {
            return false;
        }
    }
}
