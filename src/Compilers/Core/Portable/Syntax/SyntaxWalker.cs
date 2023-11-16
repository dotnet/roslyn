// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis
{
#pragma warning disable RS0030 // Do not use banned APIs: SyntaxWalker
    /// <summary>
    /// Walks the syntax tree, allowing subclasses to operate on all nodes, token and trivia.  The
    /// walker will perform a depth first walk of the tree.
    /// </summary>
    public abstract class SyntaxWalker
    {
        /// <summary>
        /// Syntax the <see cref="SyntaxWalker"/> should descend into.
        /// </summary>
        protected SyntaxWalkerDepth Depth { get; }

        /// <summary>
        /// Creates a new walker instance.
        /// </summary>
        /// <param name="depth">Syntax the <see cref="SyntaxWalker"/> should descend into.</param>
        protected SyntaxWalker(SyntaxWalkerDepth depth = SyntaxWalkerDepth.Node)
        {
            this.Depth = depth;
        }

        /// <summary>
        /// Called when the walker visits a node.  This method may be overridden if subclasses want
        /// to handle the node.  Overrides should call back into this base method if they want the
        /// children of this node to be visited.
        /// </summary>
        /// <param name="node">The current node that the walker is visiting.</param>
        public virtual void Visit(SyntaxNode node)
        {
            foreach (var child in node.ChildNodesAndTokens())
            {
                if (child.IsNode)
                {
                    if (this.Depth >= SyntaxWalkerDepth.Node)
                    {
                        Visit(child.AsNode()!);
                    }
                }
                else if (child.IsToken)
                {
                    if (this.Depth >= SyntaxWalkerDepth.Token)
                    {
                        VisitToken(child.AsToken());
                    }
                }
            }
        }

        /// <summary>
        /// Called when the walker visits a token.  This method may be overridden if subclasses want
        /// to handle the token.  Overrides should call back into this base method if they want the 
        /// trivia of this token to be visited.
        /// </summary>
        /// <param name="token">The current token that the walker is visiting.</param>
        protected virtual void VisitToken(SyntaxToken token)
        {
            if (this.Depth >= SyntaxWalkerDepth.Trivia)
            {
                this.VisitLeadingTrivia(token);
                this.VisitTrailingTrivia(token);
            }
        }

        private void VisitLeadingTrivia(in SyntaxToken token)
        {
            if (token.HasLeadingTrivia)
            {
                foreach (var trivia in token.LeadingTrivia)
                {
                    VisitTrivia(trivia);
                }
            }
        }

        private void VisitTrailingTrivia(in SyntaxToken token)
        {
            if (token.HasTrailingTrivia)
            {
                foreach (var trivia in token.TrailingTrivia)
                {
                    VisitTrivia(trivia);
                }
            }
        }

        /// <summary>
        /// Called when the walker visits a trivia syntax.  This method may be overridden if
        /// subclasses want to handle the token.  Overrides should call back into this base method if
        /// they want the children of this trivia syntax to be visited.
        /// </summary>
        /// <param name="trivia">The current trivia syntax that the walker is visiting.</param>
        protected virtual void VisitTrivia(SyntaxTrivia trivia)
        {
            if (this.Depth >= SyntaxWalkerDepth.StructuredTrivia && trivia.HasStructure)
            {
                this.Visit(trivia.GetStructure()!);
            }
        }
    }
#pragma warning restore
}
