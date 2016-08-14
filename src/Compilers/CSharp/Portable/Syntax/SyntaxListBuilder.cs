// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Syntax;
using CoreInternalSyntax = Microsoft.CodeAnalysis.Syntax.InternalSyntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    internal class SyntaxListBuilder : AbstractSyntaxListBuilder
    {
        public SyntaxListBuilder(int size) : base(size)
        {
        }

        public bool Any(SyntaxKind kind) => Any((int)kind);

        internal GreenNode ToListNode()
        {
            switch (this.Count)
            {
                case 0:
                    return null;
                case 1:
                    return (Syntax.InternalSyntax.CSharpSyntaxNode)Nodes[0].Value;
                case 2:
                    return CoreInternalSyntax.CommonSyntaxList.List((Syntax.InternalSyntax.CSharpSyntaxNode)Nodes[0].Value, (Syntax.InternalSyntax.CSharpSyntaxNode)Nodes[1].Value);
                case 3:
                    return CoreInternalSyntax.CommonSyntaxList.List((Syntax.InternalSyntax.CSharpSyntaxNode)Nodes[0].Value, (Syntax.InternalSyntax.CSharpSyntaxNode)Nodes[1].Value, (Syntax.InternalSyntax.CSharpSyntaxNode)Nodes[2].Value);
                default:
                    var tmp = new ArrayElement<GreenNode>[this.Count];
                    for (int i = 0; i < this.Count; i++)
                    {
                        tmp[i].Value = Nodes[i].Value;
                    }

                    return CoreInternalSyntax.CommonSyntaxList.List(tmp);
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