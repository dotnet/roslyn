// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Represents a non-recursive visitor which descends an entire <see cref="CSharpSyntaxNode"/> graph
    /// </summary>
    public abstract partial class NonRecursiveFullTreeVisitor : CSharpSyntaxVisitor<NonRecursiveFullTreeVisitor.Chunk>
    {
        public class Chunk
        {
            internal Chunk(SyntaxNodeOrToken[] children)
            {
                this.ChildNodes = children;
            }

            public SyntaxNodeOrToken[] ChildNodes { get; private set; }
        }

        public new void Visit(SyntaxNode node)
        {
            this.Visit((SyntaxNodeOrToken)node);
        }

        public void Visit(SyntaxToken token)
        {
            this.Visit((SyntaxNodeOrToken)token);
        }

        private void Visit(SyntaxNodeOrToken node)
        {
            if (node == null)
            {
                return;
            }

            Stack<SyntaxNodeOrToken> nodesToRewriteStack = new Stack<SyntaxNodeOrToken>();
            nodesToRewriteStack.Push(node);

            while (nodesToRewriteStack.Count != 0)
            {
                SyntaxNodeOrToken syntaxNodeOrToken = nodesToRewriteStack.Pop();

                if (syntaxNodeOrToken.IsToken)
                {
                    this.VisitToken(syntaxNodeOrToken.AsToken());
                }
                else
                {
                    var subNode = (CSharpSyntaxNode)syntaxNodeOrToken.AsNode();
                    if (subNode == null)
                    {
                        continue;
                    }

                    foreach (SyntaxNodeOrToken childNode in VisitNode(subNode).ChildNodes.Reverse())
                    {
                        nodesToRewriteStack.Push(childNode);
                    }
                }
            }
        }

        protected virtual Chunk CreateChunk(SyntaxNodeOrToken nodeOrToken, params SyntaxNodeOrToken[] children)
        {
            return new Chunk(children);
        }

        protected Chunk CreateChunk(SyntaxNodeOrToken nodeOrToken, IEnumerable<SyntaxNodeOrToken> children)
        {
            return this.CreateChunk(nodeOrToken, children.ToArray());
        }

        protected virtual Chunk VisitNode(CSharpSyntaxNode node)
        {
            return node.Accept(this);
        }

        public virtual Chunk VisitToken(SyntaxToken token)
        {
            return this.CreateChunk(token);
        }
    }
}
