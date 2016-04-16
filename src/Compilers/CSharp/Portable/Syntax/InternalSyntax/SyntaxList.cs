// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal abstract partial class SyntaxList : CSharpSyntaxNode
    {
        internal SyntaxList()
            : base(SyntaxKind.List)
        {
        }

        internal SyntaxList(ObjectReader reader)
            : base(reader)
        {
        }

        internal static CSharpSyntaxNode List(CSharpSyntaxNode child)
        {
            return child;
        }

        internal static WithTwoChildren List(CSharpSyntaxNode child0, CSharpSyntaxNode child1)
        {
            Debug.Assert(child0 != null);
            Debug.Assert(child1 != null);

            int hash;
            GreenNode cached = SyntaxNodeCache.TryGetNode((short)SyntaxKind.List, child0, child1, out hash);
            if (cached != null)
                return (WithTwoChildren)cached;

            var result = new WithTwoChildren(child0, child1);
            if (hash >= 0)
            {
                SyntaxNodeCache.AddNode(result, hash);
            }

            return result;
        }

        internal static WithThreeChildren List(CSharpSyntaxNode child0, CSharpSyntaxNode child1, CSharpSyntaxNode child2)
        {
            Debug.Assert(child0 != null);
            Debug.Assert(child1 != null);
            Debug.Assert(child2 != null);

            int hash;
            GreenNode cached = SyntaxNodeCache.TryGetNode((short)SyntaxKind.List, child0, child1, child2, out hash);
            if (cached != null)
                return (WithThreeChildren)cached;

            var result = new WithThreeChildren(child0, child1, child2);
            if (hash >= 0)
            {
                SyntaxNodeCache.AddNode(result, hash);
            }

            return result;
        }

        internal static CSharpSyntaxNode List(CSharpSyntaxNode[] nodes)
        {
            return List(nodes, nodes.Length);
        }

        internal static CSharpSyntaxNode List(CSharpSyntaxNode[] nodes, int count)
        {
            var array = new ArrayElement<CSharpSyntaxNode>[count];
            for (int i = 0; i < count; i++)
            {
                Debug.Assert(nodes[i] != null);
                array[i].Value = nodes[i];
            }

            return List(array);
        }

        internal static SyntaxList List(ArrayElement<CSharpSyntaxNode>[] children)
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

        internal static CSharpSyntaxNode List(SyntaxListBuilder builder)
        {
            if (builder != null)
            {
                return builder.ToListNode();
            }

            return null;
        }

        internal abstract void CopyTo(ArrayElement<CSharpSyntaxNode>[] array, int offset);

        internal static CSharpSyntaxNode Concat(CSharpSyntaxNode left, CSharpSyntaxNode right)
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
                    var tmp = new ArrayElement<CSharpSyntaxNode>[left.SlotCount + right.SlotCount];
                    leftList.CopyTo(tmp, 0);
                    rightList.CopyTo(tmp, left.SlotCount);
                    return List(tmp);
                }
                else
                {
                    var tmp = new ArrayElement<CSharpSyntaxNode>[left.SlotCount + 1];
                    leftList.CopyTo(tmp, 0);
                    tmp[left.SlotCount].Value = right;
                    return List(tmp);
                }
            }
            else if (rightList != null)
            {
                var tmp = new ArrayElement<CSharpSyntaxNode>[rightList.SlotCount + 1];
                tmp[0].Value = left;
                rightList.CopyTo(tmp, 1);
                return List(tmp);
            }
            else
            {
                return List(left, right);
            }
        }

        internal override GreenNode SetDiagnostics(DiagnosticInfo[] diagnostics)
        {
            throw ExceptionUtilities.Unreachable;
        }

        internal override GreenNode SetAnnotations(SyntaxAnnotation[] annotations)
        {
            throw ExceptionUtilities.Unreachable;
        }
    }
}
