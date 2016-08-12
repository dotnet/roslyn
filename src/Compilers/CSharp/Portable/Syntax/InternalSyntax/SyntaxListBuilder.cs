// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Syntax.InternalSyntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal class SyntaxListBuilder : AbstractSyntaxListBuilder<CSharpSyntaxNode>
    {
        public SyntaxListBuilder(int size) : base(size)
        {
        }

        public void AddRange(CSharpSyntaxNode[] items)
        {
            this.AddRange(items, 0, items.Length);
        }

        public void AddRange(SyntaxList<CSharpSyntaxNode> list)
        {
            this.AddRange(list, 0, list.Count);
        }

        public void AddRange(SyntaxList<CSharpSyntaxNode> list, int offset, int length)
        {
            // Necessary, but not sufficient (e.g. for nested lists).
            EnsureAdditionalCapacity(length - offset);

            int oldCount = this.Count;

            for (int i = offset; i < length; i++)
            {
                Add(list[i]);
            }

            Validate(oldCount, this.Count);
        }

        public void AddRange<TNode>(SyntaxList<TNode> list) where TNode : CSharpSyntaxNode
        {
            this.AddRange(list, 0, list.Count);
        }

        public void AddRange<TNode>(SyntaxList<TNode> list, int offset, int length) where TNode : CSharpSyntaxNode
        {
            this.AddRange(new SyntaxList<CSharpSyntaxNode>(list.Node), offset, length);
        }

        public bool Any(SyntaxKind kind) => Any((int)kind);

        internal CSharpSyntaxNode ToListNode()
        {
            switch (this.Count)
            {
                case 0:
                    return null;
                case 1:
                    return Nodes[0];
                case 2:
                    return SyntaxList.List(Nodes[0], Nodes[1]);
                case 3:
                    return SyntaxList.List(Nodes[0], Nodes[1], Nodes[2]);
                default:
                    var tmp = new ArrayElement<CSharpSyntaxNode>[this.Count];
                    Array.Copy(Nodes, tmp, this.Count);
                    return SyntaxList.List(tmp);
            }
        }

        public static implicit operator SyntaxList<CSharpSyntaxNode>(SyntaxListBuilder builder)
        {
            if (builder == null)
            {
                return default(SyntaxList<CSharpSyntaxNode>);
            }

            return builder.ToList();
        }
    }
}
