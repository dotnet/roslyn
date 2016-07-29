// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Represents a non-recursive visitor which descends an entire <see cref="CSharpSyntaxNode"/> graph
    /// </summary>
    public class CSharpNonRecursiveSyntaxWalker : CSharpSyntaxVisitor
    {
        private readonly Stack<SyntaxNodeOrToken> _stack = new Stack<SyntaxNodeOrToken>();

        public override void Visit(SyntaxNode node)
        {
            _stack.Push(node);
            while (_stack.Count != 0)
            {
                SyntaxNodeOrToken n = _stack.Pop();
                if (n.IsToken)
                {
                    this.VisitToken(n.AsToken());
                }
                else
                {
                    ((CSharpSyntaxNode)n.AsNode()).Accept(this);
                    var children = n.ChildNodesAndTokens();
                    for (int i = children.Count - 1; i >= 0; i--)
                    {
                        _stack.Push(children[i]);
                    }
                }
            }
        }

        public virtual void VisitToken(SyntaxToken token)
        {
        }
    }
}
