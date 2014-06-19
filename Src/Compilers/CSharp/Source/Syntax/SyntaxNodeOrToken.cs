using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Common;
using Microsoft.CodeAnalysis.Common.Semantics;
using Microsoft.CodeAnalysis.Common.Symbols;
using Microsoft.CodeAnalysis.CSharp.Semantics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Differencing;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
#if REMOVE
    // Note that we do not store the token directly, we just store enough information to reconstruct
    // it. This allows us to reuse nodeOrToken as a token's parent.
    /// <summary>
    /// This structure is a union of either a CSharpSyntaxNode or a SyntaxToken.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public struct SyntaxNodeOrToken : IEquatable<SyntaxNodeOrToken>
    {
        // In a case if we are wrapping a CSharpSyntaxNode this is the CSharpSyntaxNode itself.
        // In a case where we are wrapping a token, this is the token's parent.
        private readonly CSharpSyntaxNode nodeOrParent;

        // Green node for the token. 
        private readonly Syntax.InternalSyntax.SyntaxToken token;

        // Used in both node and token cases.
        // When we have a node, position == nodeOrParent.Position
        private readonly int position;

        // Index of the token among parent's children. 
        // This field only makes sense if this is a token.
        // For regular nodes it is set to 0.
        internal readonly int tokenIndex;

        internal SyntaxNodeOrToken(CSharpSyntaxNode node)
        {
            this.token = null;
            this.tokenIndex = 0;
            this.position = 0;
            this.nodeOrParent = null;

            if (node != null)
            {
                this.position = node.Position;
                this.nodeOrParent = node;
            }
        }

#if DEBUG

#endif

#if DEBUG
        internal SyntaxNodeOrToken(CSharpSyntaxNode parent, Syntax.InternalSyntax.SyntaxToken token, int position, int index, bool fromTokenCtor = false)
#else
        internal SyntaxNodeOrToken(CSharpSyntaxNode parent, Syntax.InternalSyntax.SyntaxToken token, int position, int index)
#endif
        {
            Debug.Assert(parent == null || parent.Kind != SyntaxKind.List);

            this.nodeOrParent = parent;
            this.token = token;
            this.position = position;
            this.tokenIndex = index;

#if DEBUG
            if (!fromTokenCtor && token != null)
            {
                // create a token just for the purpose of argument validation.
                new SyntaxToken(parent, token, position, index);
            }
#endif
        }

        internal string DebuggerDisplay
        {
            get { return GetType().Name + " " + CSharpKind() + " " + ToString(); }
        }

        internal Syntax.InternalSyntax.CSharpSyntaxNode UnderlyingNode
        {
            get
            {
                if (this.IsNode)
                {
                    return this.nodeOrParent.Green;
                }

                return this.token;
            }
        }

        public bool IsNode
        {
            get { return !this.IsToken; }
        }

        public bool IsToken
        {
            get { return this.token != null; }
        }

        public CSharpSyntaxNode AsNode()
        {
            if (!this.IsNode)
            {
                return null;
            }
            return this.nodeOrParent;
        }

        public SyntaxToken AsToken()
        {
            if (!this.IsToken)
            {
                return default(SyntaxToken);
            }

            return new SyntaxToken(this.nodeOrParent, this.token, this.Position, this.tokenIndex);
        }

        public SyntaxKind CSharpKind()
        {
            if (this.IsToken)
            {
                return this.token.Kind;
            }
            if (this.nodeOrParent != null)
            {
                return this.nodeOrParent.Kind;
            }
            return SyntaxKind.None;
        }

        /// <summary>
        /// The language name that this node or token is syntax of.
        /// </summary>
        public string Language
        {
            get { return LanguageNames.CSharp; }
        }

        public bool IsMissing
        {
            get
            {
                if (this.IsToken)
                {
                    return this.token.IsMissing;
                }
                return ((this.nodeOrParent != null) && this.nodeOrParent.IsMissing);
            }
        }

        internal int Offset
        {
            get
            {
                if (this.Parent == null)
                {
                    return this.position;
                }

                return (this.position - this.Parent.Position);
            }
        }

        internal int Position
        {
            get
            {
                return this.position;
            }
        }

        internal int End
        {
            get
            {
                return this.position + this.FullWidth;
            }
        }

        public ChildSyntaxList ChildNodesAndTokens()
        {
            if (this.IsToken)
            {
                return default(ChildSyntaxList);
            }

            return this.nodeOrParent.ChildNodesAndTokens();
        }

        public bool ContainsDiagnostics
        {
            get
            {
                return this.IsToken
                    ? this.token.ContainsDiagnostics
                    : this.nodeOrParent.ContainsDiagnostics;
            }
        }

        public bool ContainsDirectives
        {
            get
            {
                if (this.IsToken)
                {
                    return this.token.ContainsDirectives;
                }

                return this.nodeOrParent.ContainsDirectives;
            }
        }

        /// <summary>
        /// Determine whether any of this node or token's descendant trivia is structured.
        /// </summary>
        internal bool HasStructuredTrivia
        {
            get
            {
                if (this.IsToken)
                {
                    return this.token.HasStructuredTrivia;
                }

                return this.nodeOrParent.HasStructuredTrivia;
            }
        }

        internal Syntax.InternalSyntax.DirectiveStack ApplyDirectives(Syntax.InternalSyntax.DirectiveStack stack)
        {
            if (this.IsToken)
            {
                return this.token.ApplyDirectives(stack);
            }

            if (this.nodeOrParent != null)
            {
                return this.nodeOrParent.Green.ApplyDirectives(stack);
            }

            return stack;
        }

        internal bool HasSkippedText
        {
            get
            {
                return IsNode ? this.nodeOrParent != null && this.nodeOrParent.HasSkippedText : this.token != null && this.token.HasSkippedText;
            }
        }

        public CSharpSyntaxNode Parent
        {
            get
            {
                if (!this.IsToken)
                {
                    return this.nodeOrParent.Parent;
                }
                return this.nodeOrParent;
            }
        }

        /// <summary>
        /// SyntaxTree which contains current SyntaxNodeOrToken.
        /// </summary>
        public CSharpSyntaxTree SyntaxTree
        {
            get
            {
                var nodeOrParent = this.nodeOrParent;
                if (nodeOrParent != null)
                {
                    return nodeOrParent.SyntaxTree;
                }

                return null;
            }
        }

        public TextSpan FullSpan
        {
            get
            {
                if (this.IsNode)
                {
                    return this.nodeOrParent.FullSpan;
                }

                return new TextSpan(Position, this.token.FullWidth);
            }
        }

        public TextSpan Span
        {
            get
            {
                if (this.IsNode)
                {
                    return this.nodeOrParent.Span;
                }

                return this.AsToken().Span;
            }
        }

        public int SpanStart
        {
            get
            {
                if (this.IsNode)
                {
                    return this.nodeOrParent.SpanStart;
                }

                // PERF: Inlined "this.AsToken().SpanStart"
                return this.position + this.token.LeadingWidth;
            }
        }

        internal int Width
        {
            get
            {
                if (this.IsToken)
                {
                    return this.token.Width;
                }
                return this.nodeOrParent.Width;
            }
        }

        internal int FullWidth
        {
            get
            {
                return this.IsToken
                    ? this.token.FullWidth
                    : this.nodeOrParent.FullWidth;
            }
        }

        /// <summary>
        /// Returns the string representation of this node or token, not including its leading and trailing
        /// trivia.
        /// </summary>
        /// <returns>
        /// The string representation of this node or token, not including its leading and trailing
        /// trivia.
        /// </returns>
        /// <remarks>The length of the returned string is always the same as Span.Length</remarks>
        public override string ToString()
        {
            if (this.IsToken)
            {
                return this.token.ToString();
            }

            if (this.nodeOrParent != null)
            {
                return this.nodeOrParent.ToString();
            }

            return null;
        }

        /// <summary>
        /// Returns the full string representation of this node or token including its leading and trailing trivia.
        /// </summary>
        /// <returns>The full string representation of this node or token including its leading and trailing
        /// trivia.</returns>
        /// <remarks>The length of the returned string is always the same as FullSpan.Length</remarks>
        public string ToFullString()
        {
            if (!this.IsToken)
            {
                return this.nodeOrParent.ToFullString();
            }
            return this.token.ToFullString();
        }

        public bool HasLeadingTrivia
        {
            get
            {
                if (!this.IsToken)
                {
                    return this.nodeOrParent.HasLeadingTrivia;
                }

                return this.token.HasLeadingTrivia;
            }
        }

        public SyntaxTriviaList GetLeadingTrivia()
        {
            if (!this.IsToken)
            {
                return this.AsNode().GetLeadingTrivia();
            }

            return this.AsToken().LeadingTrivia;
        }

        public bool HasTrailingTrivia
        {
            get
            {
                if (!this.IsToken)
                {
                    return this.AsNode().HasTrailingTrivia;
                }

                return this.token.HasTrailingTrivia;
            }
        }

        public SyntaxTriviaList GetTrailingTrivia()
        {
            if (!this.IsToken)
            {
                return this.AsNode().GetTrailingTrivia();
            }

            return this.AsToken().TrailingTrivia;
        }

        public static implicit operator SyntaxNodeOrToken(CSharpSyntaxNode node)
        {
            return new SyntaxNodeOrToken(node);
        }

        public static explicit operator CSharpSyntaxNode(SyntaxNodeOrToken nodeOrToken)
        {
            return nodeOrToken.AsNode();
        }

        public static implicit operator SyntaxNodeOrToken(SyntaxToken token)
        {
            return new SyntaxNodeOrToken((CSharpSyntaxNode)token.Parent, (Syntax.InternalSyntax.SyntaxToken)token.Node, token.Position, token.Index);
        }

        public static explicit operator SyntaxToken(SyntaxNodeOrToken nodeOrToken)
        {
            return nodeOrToken.AsToken();
        }

        public bool Equals(SyntaxNodeOrToken other)
        {
            // index replaces offset to ensure equality.  Assert if offset affects equality.
            Debug.Assert(
                (this.nodeOrParent == other.nodeOrParent && this.token == other.token && this.position == other.position && this.tokenIndex == other.tokenIndex) ==
                (this.nodeOrParent == other.nodeOrParent && this.token == other.token && this.tokenIndex == other.tokenIndex)
            );

            return this.nodeOrParent == other.nodeOrParent &&
                   this.token == other.token &&
                   this.tokenIndex == other.tokenIndex;
        }

        public static bool operator ==(SyntaxNodeOrToken left, SyntaxNodeOrToken right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SyntaxNodeOrToken left, SyntaxNodeOrToken right)
        {
            return !left.Equals(right);
        }

        public override bool Equals(object obj)
        {
            return obj is SyntaxNodeOrToken && Equals((SyntaxNodeOrToken)obj);
        }

        public override int GetHashCode()
        {
            return (this.nodeOrParent != null ? this.nodeOrParent.GetHashCode() : 0)
                + (this.token != null ? this.token.GetHashCode() : 0)
                + this.position
                + this.tokenIndex;
        }

        public bool IsEquivalentTo(SyntaxNodeOrToken other)
        {
            if (this.IsNode && other.IsNode)
            {
                return this.nodeOrParent.Green.IsEquivalentTo(other.nodeOrParent.Green);
            }
            return ((this.IsToken && other.IsToken) && this.token.IsEquivalentTo(other.token));
        }

        public static implicit operator SyntaxNodeOrToken(SyntaxNodeOrToken nodeOrToken)
        {
            return new SyntaxNodeOrToken(nodeOrToken.nodeOrParent, nodeOrToken.token, nodeOrToken.Position, nodeOrToken.tokenIndex);
        }

        public static explicit operator SyntaxNodeOrToken(SyntaxNodeOrToken nodeOrToken)
        {
            if (nodeOrToken.IsNode)
            {
                return new SyntaxNodeOrToken((CSharpSyntaxNode)nodeOrToken.UnderlyingNode);
            }

            return new SyntaxNodeOrToken((CSharpSyntaxNode)nodeOrToken.Parent,
                (Syntax.InternalSyntax.SyntaxToken)nodeOrToken.UnderlyingNode,
                nodeOrToken.Position,
                nodeOrToken.Index);
        }

        public static implicit operator SyntaxNodeOrToken(SyntaxNode node)
        {
            return (CSharpSyntaxNode)node;
        }

        /// <summary>
        /// binary search of nodes to find the slot.  Consider unifying this with that
        /// implementation.
        /// </summary>
        public static int GetFirstChildIndexSpanningPosition(CSharpSyntaxNode node, int position)
        {
            return SyntaxNodeOrToken.GetFirstChildIndexSpanningPosition(node, position);
        }

        public SyntaxNodeOrToken GetNextSibling()
        {
            return (SyntaxNodeOrToken)((SyntaxNodeOrToken)this).GetNextSibling();
        }

        public SyntaxNodeOrToken GetPreviousSibling()
        {
            return (SyntaxNodeOrToken)((SyntaxNodeOrToken)this).GetPreviousSibling();
        }

        internal static bool AreIdentical(SyntaxNodeOrToken node1, SyntaxNodeOrToken node2)
        {
            return node1.UnderlyingNode == node2.UnderlyingNode;
        }

        /// <summary>
        /// Determines whether this node or token (or any sub node, token or trivia) has annotations.
        /// </summary>
        public bool ContainsAnnotations
        {
            get
            {
                return this.IsToken
                    ? this.token.ContainsAnnotations
                    : this.nodeOrParent.ContainsAnnotations;
            }
        }

        /// <summary>
        /// Determines whether this node or token has annotations of the specified type.
        /// </summary>
        public bool HasAnnotations(Type annotationType)
        {
            return this.IsToken
                ? this.token.HasAnnotations(annotationType)
                : this.nodeOrParent.HasAnnotations(annotationType);
        }

        /// <summary>
        /// Determines if this node or token has any annotation of the specified type attached.
        /// </summary>
        public bool HasAnnotations<TSyntaxAnnotation>() where TSyntaxAnnotation : SyntaxAnnotation
        {
            return HasAnnotations(typeof(TSyntaxAnnotation));
        }

        /// <summary>
        /// Determines whether this node or token as the specific annotation.
        /// </summary>
        public bool HasAnnotation(SyntaxAnnotation annotation)
        {
            return this.IsToken
                 ? this.token.HasAnnotation(annotation)
                 : this.nodeOrParent.HasAnnotation(annotation);
        }

        /// <summary>
        /// Gets all annotations of the specified type attached to this node or token.
        /// </summary>
        public IEnumerable<SyntaxAnnotation> GetAnnotations(Type annotationType)
        {
            return this.IsToken
                ? this.token.GetAnnotations(annotationType)
                : this.nodeOrParent.GetAnnotations(annotationType);
        }

        /// <summary>
        /// Gets all the annotations of the specified type attached to this node or token (or any sub  node).
        /// </summary>
        public IEnumerable<TSyntaxAnnotation> GetAnnotations<TSyntaxAnnotation>() where TSyntaxAnnotation : SyntaxAnnotation
        {
            return GetAnnotations(typeof(TSyntaxAnnotation)).Cast<TSyntaxAnnotation>();
        }

        /// <summary>
        /// Adds this annotation to a given syntax node or token, creating a new syntax node or token of the same type with the
        /// annotation on it.
        /// </summary>
        public SyntaxNodeOrToken WithAdditionalAnnotations(params SyntaxAnnotation[] annotations)
        {
            if (annotations == null)
            {
                throw new ArgumentNullException("annotations");
            }

            if (this.IsNode)
            {
                return this.AsNode().WithAdditionalAnnotations(annotations);
            }
            else if (this.IsToken)
            {
                return this.AsToken().WithAdditionalAnnotations(annotations);
            }
            else
            {
                return this;
            }
        }

        /// <summary>
        /// Gets a <see cref="Location"/> for this node or token.
        /// </summary>
        public Location GetLocation()
        {
            return new SourceLocation(this);
        }

        /// <summary>
        /// Gets a list of all the diagnostics in either the sub tree that has this node as its root or
        /// associated with this token and its related trivia. 
        /// This method does not filter diagnostics based on #pragmas and compiler options
        /// like nowarn, warnaserror etc.
        /// </summary>
        public IEnumerable<Diagnostic> GetDiagnostics()
        {
            return this.SyntaxTree.GetDiagnostics(this);
        }

        /// <summary>
        /// Determines if the node or token is a descendant of a structured trivia.
        /// </summary>
        public bool IsPartOfStructuredTrivia()
        {
            if (IsNode)
            {
                return AsNode().IsPartOfStructuredTrivia();
            }
            else if (IsToken)
            {
                return AsToken().IsPartOfStructuredTrivia();
            }
            else 
            {
                return false;
            }
        }

        public SyntaxNodeOrToken WithLeadingTrivia(IEnumerable<SyntaxTrivia> trivia)
        {
            return IsNode
                ? AsNode().WithLeadingTrivia(trivia)
                : (SyntaxNodeOrToken)AsToken().WithLeadingTrivia(trivia);
        }

        public SyntaxNodeOrToken WithLeadingTrivia(params SyntaxTrivia[] trivia)
        {
            return WithLeadingTrivia((IEnumerable<SyntaxTrivia>)trivia);
        }

        public SyntaxNodeOrToken WithTrailingTrivia(IEnumerable<SyntaxTrivia> trivia)
        {
            return IsNode
                ? AsNode().WithTrailingTrivia(trivia)
                : (SyntaxNodeOrToken)AsToken().WithTrailingTrivia(trivia);
        }

        public SyntaxNodeOrToken WithTrailingTrivia(params SyntaxTrivia[] trivia)
        {
            return WithTrailingTrivia((IEnumerable<SyntaxTrivia>)trivia);
        }
    }
#endif
}