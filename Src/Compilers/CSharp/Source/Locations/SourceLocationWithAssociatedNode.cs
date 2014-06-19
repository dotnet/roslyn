using System;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Roslyn.Compilers.CSharp
{
    /// <summary>
    /// A program location in source code for which there is an associated SyntaxNode.
    /// </summary>
    [Serializable]
    internal sealed class SourceLocationWithAssociatedNode : SourceLocation
    {
        // A SourceLocation can also be associated with a weak reference to a SyntaxNode. This is
        // not exposed via the public API, and it present so that diagnostics can be associated with
        // syntax nodes more reliably, even if the span of the location is different.
        //
        // The "associateInParent" flag is a key part of this. If this is true, then the diagnostic
        // is associated with the usage of this syntax node within it's parent -- it is retrieved
        // when GetSemanticInfoInParent is called on the node, or when GetSemanticInfo is called on
        // the parent.
        //
        // A weak reference is usage so that the Diagnostics do not keep the SyntaxNode alive. Usage
        // of this within the binding API already keeps the syntax tree alive; other usages of
        // diagnostics should not keep the syntax tree alive.
        private readonly WeakReference<SyntaxNode> associatedNode;
        private readonly bool associateInParent;

        // This constructor can be used to have the span and associated node be arbitrarily different.
        public SourceLocationWithAssociatedNode(SyntaxTree syntaxTree, TextSpan span, SyntaxNode associatedNode, bool associateInParent)
            : base(syntaxTree, span)
        {
            Debug.Assert(associatedNode != null); //if it's null, construct a SourceLocation instead
            this.associatedNode = new WeakReference<SyntaxNode>(associatedNode);
            this.associateInParent = associateInParent;
        }

        public SourceLocationWithAssociatedNode(SyntaxTree syntaxTree, SyntaxToken token)
            : this(syntaxTree, token.Span, token.Parent, associateInParent: false)
        {
        }

        public SourceLocationWithAssociatedNode(SyntaxTree syntaxTree, SyntaxNodeOrToken nodeOrToken, bool associateInParent = false)
            : this(syntaxTree, nodeOrToken.Span, nodeOrToken.IsNode ? nodeOrToken.AsNode() : nodeOrToken.AsToken().Parent, associateInParent)
        {
        }

        public SourceLocationWithAssociatedNode(SyntaxTree syntaxTree, SyntaxTrivia trivia)
            : this(syntaxTree, trivia.Span, trivia.Token.Parent, associateInParent: false)
        {
        }

        // Get the syntax node this diagnostic was associated with. Or return null
        // if this diagnostic wasn't associated with a syntax node, or that syntax node
        // was garbage collected.
        internal SyntaxNode AssociatedSyntaxNode
        {
            get
            {
                return (this.associatedNode != null) ? this.associatedNode.GetTarget() : null;
            }
        }

        // Return true if this diagnostic is associated with the way this node is used
        // from its parent.
        internal bool AssociatedInParent
        {
            get
            {
                return this.associateInParent;
            }
        }
    }
}