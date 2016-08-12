// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    internal class SyntaxListBuilder : AbstractSyntaxListBuilder
    {
        public SyntaxListBuilder(int size) : base(size)
        {
        }

        public void AddRange<TNode>(SyntaxList<TNode> list) where TNode : SyntaxNode
        {
            this.AddRange(list, 0, list.Count);
        }

        public void AddRange<TNode>(SyntaxList<TNode> list, int offset, int count) where TNode : SyntaxNode
        {
            this.AddRange(new SyntaxList<SyntaxNode>(list.Node), offset, count);
        }

        public void AddRange(SyntaxTokenList list)
        {
            this.AddRange(list, 0, list.Count);
        }

        public void AddRange(SyntaxTokenList list, int offset, int length)
        {
            this.AddRange(new SyntaxList<SyntaxNode>(list.Node.CreateRed()), offset, length);
        }

        public bool Any(SyntaxKind kind) => Any((int)kind);

        internal Syntax.InternalSyntax.CSharpSyntaxNode ToListNode()
        {
            switch (this.Count)
            {
                case 0:
                    return null;
                case 1:
                    return (Syntax.InternalSyntax.CSharpSyntaxNode)Nodes[0].Value;
                case 2:
                    return Syntax.InternalSyntax.SyntaxList.List((Syntax.InternalSyntax.CSharpSyntaxNode)Nodes[0].Value, (Syntax.InternalSyntax.CSharpSyntaxNode)Nodes[1].Value);
                case 3:
                    return Syntax.InternalSyntax.SyntaxList.List((Syntax.InternalSyntax.CSharpSyntaxNode)Nodes[0].Value, (Syntax.InternalSyntax.CSharpSyntaxNode)Nodes[1].Value, (Syntax.InternalSyntax.CSharpSyntaxNode)Nodes[2].Value);
                default:
                    var tmp = new ArrayElement<Syntax.InternalSyntax.CSharpSyntaxNode>[this.Count];
                    for (int i = 0; i < this.Count; i++)
                    {
                        tmp[i].Value = (Syntax.InternalSyntax.CSharpSyntaxNode)Nodes[i].Value;
                    }

                    return Syntax.InternalSyntax.SyntaxList.List(tmp);
            }
        }

        public static implicit operator SyntaxList<SyntaxNode>(SyntaxListBuilder builder)
        {
            if (builder == null)
            {
                return default(SyntaxList<SyntaxNode>);
            }

            return builder.ToList();
        }
    }
}
