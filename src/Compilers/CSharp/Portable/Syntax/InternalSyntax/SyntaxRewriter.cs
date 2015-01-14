// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal partial class CSharpSyntaxRewriter : CSharpSyntaxVisitor<CSharpSyntaxNode>
    {
        protected readonly bool VisitIntoStructuredTrivia;

        public CSharpSyntaxRewriter(bool visitIntoStructuredTrivia = false)
        {
            this.VisitIntoStructuredTrivia = visitIntoStructuredTrivia;
        }

        public override CSharpSyntaxNode VisitToken(SyntaxToken token)
        {
            var leading = this.VisitList(token.LeadingTrivia);
            var trailing = this.VisitList(token.TrailingTrivia);

            if (leading != token.LeadingTrivia || trailing != token.TrailingTrivia)
            {
                if (leading != token.LeadingTrivia)
                {
                    token = token.WithLeadingTrivia(leading.Node);
                }

                if (trailing != token.TrailingTrivia)
                {
                    token = token.WithTrailingTrivia(trailing.Node);
                }
            }

            return token;
        }

        public SyntaxList<TNode> VisitList<TNode>(SyntaxList<TNode> list) where TNode : CSharpSyntaxNode
        {
            SyntaxListBuilder alternate = null;
            for (int i = 0, n = list.Count; i < n; i++)
            {
                var item = list[i];
                var visited = (TNode)this.Visit(item);
                if (item != visited && alternate == null)
                {
                    alternate = new SyntaxListBuilder(n);
                    alternate.AddRange(list, 0, i);
                }

                if (alternate != null)
                {
                    Debug.Assert(visited != null && visited.Kind != SyntaxKind.None, "Cannot remove node using Syntax.InternalSyntax.SyntaxRewriter.");
                    alternate.Add(visited);
                }
            }

            if (alternate != null)
            {
                return alternate.ToList();
            }

            return list;
        }

        public SeparatedSyntaxList<TNode> VisitList<TNode>(SeparatedSyntaxList<TNode> list) where TNode : CSharpSyntaxNode
        {
            var withSeps = list.GetWithSeparators();
            var result = this.VisitList(withSeps);
            if (result != withSeps)
            {
                return result.AsSeparatedList<TNode>();
            }

            return list;
        }
    }
}
