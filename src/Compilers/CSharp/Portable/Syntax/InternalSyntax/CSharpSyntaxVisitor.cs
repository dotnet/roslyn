// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal abstract partial class CSharpSyntaxVisitor<TResult>
    {
        public virtual TResult Visit(CSharpSyntaxNode node)
        {
            if (node == null)
            {
                return default(TResult);
            }

            return node.Accept(this);
        }

        public virtual TResult VisitToken(SyntaxToken token)
        {
            return this.DefaultVisit(token);
        }

        public virtual TResult VisitTrivia(SyntaxTrivia trivia)
        {
            return this.DefaultVisit(trivia);
        }

        protected virtual TResult DefaultVisit(CSharpSyntaxNode node)
        {
            return default(TResult);
        }
    }

    internal abstract partial class CSharpSyntaxVisitor
    {
        public virtual void Visit(CSharpSyntaxNode node)
        {
            if (node == null)
            {
                return;
            }

            node.Accept(this);
        }

        public virtual void VisitToken(SyntaxToken token)
        {
            this.DefaultVisit(token);
        }

        public virtual void VisitTrivia(SyntaxTrivia trivia)
        {
            this.DefaultVisit(trivia);
        }

        public virtual void DefaultVisit(CSharpSyntaxNode node)
        {
        }
    }
}
