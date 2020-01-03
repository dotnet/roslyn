// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Represents a non-terminal node in the syntax tree.
    /// </summary>
    //The fact that this type implements IMessageSerializable
    //enables it to be used as an argument to a diagnostic. This allows diagnostics
    //to defer the realization of strings. Often diagnostics generated while binding
    //in service of a SemanticModel API are never realized. So this
    //deferral can result in meaningful savings of strings.
    public abstract partial class CSharpSyntaxNode : SyntaxNode, IFormattable
    {
        internal CSharpSyntaxNode(GreenNode green, SyntaxNode parent, int position)
            : base(green, parent, position)
        {
        }

        /// <summary>
        /// Used by structured trivia which has "parent == null", and therefore must know its
        /// SyntaxTree explicitly when created.
        /// </summary>
        internal CSharpSyntaxNode(GreenNode green, int position, SyntaxTree syntaxTree)
            : base(green, position, syntaxTree)
        {
        }

        /// <summary>
        /// Returns a non-null <see cref="SyntaxTree"/> that owns this node.
        /// If this node was created with an explicit non-null <see cref="SyntaxTree"/>, returns that tree.
        /// Otherwise, if this node has a non-null parent, then returns the parent's <see cref="SyntaxTree"/>.
        /// Otherwise, returns a newly created <see cref="SyntaxTree"/> rooted at this node, preserving this node's reference identity.
        /// </summary>
        internal new SyntaxTree SyntaxTree
        {
            get
            {
                var result = this._syntaxTree ?? ComputeSyntaxTree(this);
                Debug.Assert(result != null);
                return result;
            }
        }

        private static SyntaxTree ComputeSyntaxTree(CSharpSyntaxNode node)
        {
            ArrayBuilder<CSharpSyntaxNode> nodes = null;
            SyntaxTree tree = null;

            // Find the nearest parent with a non-null syntax tree
            while (true)
            {
                tree = node._syntaxTree;
                if (tree != null)
                {
                    break;
                }

                var parent = node.Parent;
                if (parent == null)
                {
                    // set the tree on the root node atomically
                    Interlocked.CompareExchange(ref node._syntaxTree, CSharpSyntaxTree.CreateWithoutClone(node), null);
                    tree = node._syntaxTree;
                    break;
                }

                tree = parent._syntaxTree;
                if (tree != null)
                {
                    node._syntaxTree = tree;
                    break;
                }

                (nodes ?? (nodes = ArrayBuilder<CSharpSyntaxNode>.GetInstance())).Add(node);
                node = parent;
            }

            // Propagate the syntax tree downwards if necessary
            if (nodes != null)
            {
                Debug.Assert(tree != null);

                foreach (var n in nodes)
                {
                    var existingTree = n._syntaxTree;
                    if (existingTree != null)
                    {
                        Debug.Assert(existingTree == tree, "how could this node belong to a different tree?");

                        // yield the race
                        break;
                    }
                    n._syntaxTree = tree;
                }

                nodes.Free();
            }

            return tree;
        }

        public abstract TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

        public abstract void Accept(CSharpSyntaxVisitor visitor);

        /// <summary>
        /// The node that contains this node in its Children collection.
        /// </summary>
        internal new CSharpSyntaxNode Parent
        {
            get
            {
                return (CSharpSyntaxNode)base.Parent;
            }
        }

        internal new CSharpSyntaxNode ParentOrStructuredTriviaParent
        {
            get
            {
                return (CSharpSyntaxNode)base.ParentOrStructuredTriviaParent;
            }
        }

        // TODO: may be eventually not needed
        internal Syntax.InternalSyntax.CSharpSyntaxNode CsGreen
        {
            get { return (Syntax.InternalSyntax.CSharpSyntaxNode)this.Green; }
        }

        /// <summary>
        /// Returns the <see cref="SyntaxKind"/> of the node.
        /// </summary>
        public SyntaxKind Kind()
        {
            return (SyntaxKind)this.Green.RawKind;
        }

        /// <summary>
        /// The language name that this node is syntax of.
        /// </summary>
        public override string Language
        {
            get { return LanguageNames.CSharp; }
        }

        /// <summary>
        /// The list of trivia that appears before this node in the source code.
        /// </summary>
        public new SyntaxTriviaList GetLeadingTrivia()
        {
            var firstToken = this.GetFirstToken(includeZeroWidth: true);
            return firstToken.LeadingTrivia;
        }

        /// <summary>
        /// The list of trivia that appears after this node in the source code.
        /// </summary>
        public new SyntaxTriviaList GetTrailingTrivia()
        {
            var lastToken = this.GetLastToken(includeZeroWidth: true);
            return lastToken.TrailingTrivia;
        }

        #region serialization

        /// <summary>
        /// Deserialize a syntax node from the byte stream.
        /// </summary>
        public static SyntaxNode DeserializeFrom(Stream stream, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (!stream.CanRead)
            {
                throw new InvalidOperationException(CodeAnalysisResources.TheStreamCannotBeReadFrom);
            }

            using (var reader = ObjectReader.TryGetReader(stream, cancellationToken: cancellationToken))
            {
                if (reader == null)
                {
                    throw new ArgumentException(CodeAnalysisResources.Stream_contains_invalid_data, nameof(stream));
                }

                var root = (Syntax.InternalSyntax.CSharpSyntaxNode)reader.ReadValue();
                return root.CreateRed();
            }
        }

        #endregion

        /// <summary>
        /// Gets a <see cref="Location"/> for this node.
        /// </summary>
        public new Location GetLocation()
        {
            return new SourceLocation(this);
        }

        /// <summary>
        /// Gets a SyntaxReference for this syntax node. SyntaxReferences can be used to
        /// regain access to a syntax node without keeping the entire tree and source text in
        /// memory.
        /// </summary>
        internal new SyntaxReference GetReference()
        {
            return this.SyntaxTree.GetReference(this);
        }

        /// <summary>
        /// Gets a list of all the diagnostics in the sub tree that has this node as its root.
        /// This method does not filter diagnostics based on #pragmas and compiler options
        /// like nowarn, warnaserror etc.
        /// </summary>
        public new IEnumerable<Diagnostic> GetDiagnostics()
        {
            return this.SyntaxTree.GetDiagnostics(this);
        }

        #region Directives

        internal IList<DirectiveTriviaSyntax> GetDirectives(Func<DirectiveTriviaSyntax, bool> filter = null)
        {
            return ((SyntaxNodeOrToken)this).GetDirectives<DirectiveTriviaSyntax>(filter);
        }

        /// <summary>
        /// Gets the first directive of the tree rooted by this node.
        /// </summary>
        public DirectiveTriviaSyntax GetFirstDirective(Func<DirectiveTriviaSyntax, bool> predicate = null)
        {
            foreach (var child in this.ChildNodesAndTokens())
            {
                if (child.ContainsDirectives)
                {
                    if (child.IsNode)
                    {
                        var d = child.AsNode().GetFirstDirective(predicate);
                        if (d != null)
                        {
                            return d;
                        }
                    }
                    else
                    {
                        var token = child.AsToken();

                        // directives can only occur in leading trivia
                        foreach (var tr in token.LeadingTrivia)
                        {
                            if (tr.IsDirective)
                            {
                                var d = (DirectiveTriviaSyntax)tr.GetStructure();
                                if (predicate == null || predicate(d))
                                {
                                    return d;
                                }
                            }
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the last directive of the tree rooted by this node.
        /// </summary>
        public DirectiveTriviaSyntax GetLastDirective(Func<DirectiveTriviaSyntax, bool> predicate = null)
        {
            foreach (var child in this.ChildNodesAndTokens().Reverse())
            {
                if (child.ContainsDirectives)
                {
                    if (child.IsNode)
                    {
                        var d = child.AsNode().GetLastDirective(predicate);
                        if (d != null)
                        {
                            return d;
                        }
                    }
                    else
                    {
                        var token = child.AsToken();

                        // directives can only occur in leading trivia
                        foreach (var tr in token.LeadingTrivia.Reverse())
                        {
                            if (tr.IsDirective)
                            {
                                var d = (DirectiveTriviaSyntax)tr.GetStructure();
                                if (predicate == null || predicate(d))
                                {
                                    return d;
                                }
                            }
                        }
                    }
                }
            }

            return null;
        }

        #endregion

        #region Token Lookup

        /// <summary>
        /// Gets the first token of the tree rooted by this node.
        /// </summary>
        /// <param name="includeZeroWidth">True if zero width tokens should be included, false by
        /// default.</param>
        /// <param name="includeSkipped">True if skipped tokens should be included, false by default.</param>
        /// <param name="includeDirectives">True if directives should be included, false by default.</param>
        /// <param name="includeDocumentationComments">True if documentation comments should be
        /// included, false by default.</param>
        /// <returns></returns>
        public new SyntaxToken GetFirstToken(bool includeZeroWidth = false, bool includeSkipped = false, bool includeDirectives = false, bool includeDocumentationComments = false)
        {
            return base.GetFirstToken(includeZeroWidth, includeSkipped, includeDirectives, includeDocumentationComments);
        }

        /// <summary>
        /// Gets the first token of the tree rooted by this node.
        /// </summary>
        /// <param name="predicate">Only tokens for which this predicate returns true are included.  Pass null to include
        /// all tokens.</param>
        /// <param name="stepInto">Steps into trivia if this is not null.  Only trivia for which this delegate returns
        /// true are included.</param> 
        /// <returns></returns>
        internal SyntaxToken GetFirstToken(Func<SyntaxToken, bool> predicate, Func<SyntaxTrivia, bool> stepInto = null)
        {
            return SyntaxNavigator.Instance.GetFirstToken(this, predicate, stepInto);
        }

        /// <summary>
        /// Gets the last non-zero-width token of the tree rooted by this node.
        /// </summary>
        /// <param name="includeZeroWidth">True if zero width tokens should be included, false by
        /// default.</param>
        /// <param name="includeSkipped">True if skipped tokens should be included, false by default.</param>
        /// <param name="includeDirectives">True if directives should be included, false by default.</param>
        /// <param name="includeDocumentationComments">True if documentation comments should be
        /// included, false by default.</param>
        /// <returns></returns>
        public new SyntaxToken GetLastToken(bool includeZeroWidth = false, bool includeSkipped = false, bool includeDirectives = false, bool includeDocumentationComments = false)
        {
            return base.GetLastToken(includeZeroWidth, includeSkipped, includeDirectives, includeDocumentationComments);
        }

        /// <summary>
        /// Finds a token according to the following rules:
        /// 1) If position matches the End of the node/s FullSpan and the node is CompilationUnit,
        ///    then EoF is returned. 
        /// 
        ///  2) If node.FullSpan.Contains(position) then the token that contains given position is
        ///     returned.
        /// 
        ///  3) Otherwise an ArgumentOutOfRangeException is thrown
        /// </summary>
        public new SyntaxToken FindToken(int position, bool findInsideTrivia = false)
        {
            return base.FindToken(position, findInsideTrivia);
        }

        /// <summary>
        /// Finds a token according to the following rules:
        /// 1) If position matches the End of the node/s FullSpan and the node is CompilationUnit,
        ///    then EoF is returned. 
        /// 
        ///  2) If node.FullSpan.Contains(position) then the token that contains given position is
        ///     returned.
        /// 
        ///  3) Otherwise an ArgumentOutOfRangeException is thrown
        /// </summary>
        internal SyntaxToken FindTokenIncludingCrefAndNameAttributes(int position)
        {
            SyntaxToken nonTriviaToken = this.FindToken(position, findInsideTrivia: false);

            SyntaxTrivia trivia = GetTriviaFromSyntaxToken(position, nonTriviaToken);

            if (!SyntaxFacts.IsDocumentationCommentTrivia(trivia.Kind()))
            {
                return nonTriviaToken;
            }

            Debug.Assert(trivia.HasStructure);
            SyntaxToken triviaToken = ((CSharpSyntaxNode)trivia.GetStructure()).FindTokenInternal(position);

            // CONSIDER: We might want to use the trivia token anywhere within a doc comment.
            // Otherwise, we'll fall back on the enclosing scope outside of name and cref
            // attribute values.
            CSharpSyntaxNode curr = (CSharpSyntaxNode)triviaToken.Parent;
            while (curr != null)
            {
                // Don't return a trivia token unless we're in the scope of a cref or name attribute.
                if (curr.Kind() == SyntaxKind.XmlCrefAttribute || curr.Kind() == SyntaxKind.XmlNameAttribute)
                {
                    return LookupPosition.IsInXmlAttributeValue(position, (XmlAttributeSyntax)curr)
                        ? triviaToken
                        : nonTriviaToken;
                }

                curr = curr.Parent;
            }

            return nonTriviaToken;
        }

        #endregion

        #region Trivia Lookup

        /// <summary>
        /// Finds a descendant trivia of this node at the specified position, where the position is
        /// within the span of the node.
        /// </summary>
        /// <param name="position">The character position of the trivia relative to the beginning of
        /// the file.</param>
        /// <param name="stepInto">Specifies a function that determines per trivia node, whether to
        /// descend into structured trivia of that node.</param>
        /// <returns></returns>
        public new SyntaxTrivia FindTrivia(int position, Func<SyntaxTrivia, bool> stepInto)
        {
            return base.FindTrivia(position, stepInto);
        }

        /// <summary>
        /// Finds a descendant trivia of this node whose span includes the supplied position.
        /// </summary>
        /// <param name="position">The character position of the trivia relative to the beginning of
        /// the file.</param>
        /// <param name="findInsideTrivia">Whether to search inside structured trivia.</param>
        public new SyntaxTrivia FindTrivia(int position, bool findInsideTrivia = false)
        {
            return base.FindTrivia(position, findInsideTrivia);
        }

        #endregion

        #region SyntaxNode members

        internal override SyntaxDiffer GetDiffer(SyntaxNode newNode, bool computeNewText) =>
            new CSharpSyntaxDiffer(this, newNode, computeNewText);

        /// <summary>
        /// Determine if this node is structurally equivalent to another.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        protected override bool EquivalentToCore(SyntaxNode other)
        {
            throw ExceptionUtilities.Unreachable;
        }

        protected override SyntaxTree SyntaxTreeCore
        {
            get
            {
                return this.SyntaxTree;
            }
        }

        protected internal override SyntaxNode ReplaceCore<TNode>(
            IEnumerable<TNode> nodes = null,
            Func<TNode, TNode, SyntaxNode> computeReplacementNode = null,
            IEnumerable<SyntaxToken> tokens = null,
            Func<SyntaxToken, SyntaxToken, SyntaxToken> computeReplacementToken = null,
            IEnumerable<SyntaxTrivia> trivia = null,
            Func<SyntaxTrivia, SyntaxTrivia, SyntaxTrivia> computeReplacementTrivia = null)
        {
            return SyntaxReplacer.Replace(this, nodes, computeReplacementNode, tokens, computeReplacementToken, trivia, computeReplacementTrivia).AsRootOfNewTreeWithOptionsFrom(this.SyntaxTree);
        }

        protected internal override SyntaxNode ReplaceNodeInListCore(SyntaxNode originalNode, IEnumerable<SyntaxNode> replacementNodes)
        {
            return SyntaxReplacer.ReplaceNodeInList(this, originalNode, replacementNodes).AsRootOfNewTreeWithOptionsFrom(this.SyntaxTree);
        }

        protected internal override SyntaxNode InsertNodesInListCore(SyntaxNode nodeInList, IEnumerable<SyntaxNode> nodesToInsert, bool insertBefore)
        {
            return SyntaxReplacer.InsertNodeInList(this, nodeInList, nodesToInsert, insertBefore).AsRootOfNewTreeWithOptionsFrom(this.SyntaxTree);
        }

        protected internal override SyntaxNode ReplaceTokenInListCore(SyntaxToken originalToken, IEnumerable<SyntaxToken> newTokens)
        {
            return SyntaxReplacer.ReplaceTokenInList(this, originalToken, newTokens).AsRootOfNewTreeWithOptionsFrom(this.SyntaxTree);
        }

        protected internal override SyntaxNode InsertTokensInListCore(SyntaxToken originalToken, IEnumerable<SyntaxToken> newTokens, bool insertBefore)
        {
            return SyntaxReplacer.InsertTokenInList(this, originalToken, newTokens, insertBefore).AsRootOfNewTreeWithOptionsFrom(this.SyntaxTree);
        }

        protected internal override SyntaxNode ReplaceTriviaInListCore(SyntaxTrivia originalTrivia, IEnumerable<SyntaxTrivia> newTrivia)
        {
            return SyntaxReplacer.ReplaceTriviaInList(this, originalTrivia, newTrivia).AsRootOfNewTreeWithOptionsFrom(this.SyntaxTree);
        }

        protected internal override SyntaxNode InsertTriviaInListCore(SyntaxTrivia originalTrivia, IEnumerable<SyntaxTrivia> newTrivia, bool insertBefore)
        {
            return SyntaxReplacer.InsertTriviaInList(this, originalTrivia, newTrivia, insertBefore).AsRootOfNewTreeWithOptionsFrom(this.SyntaxTree);
        }

        protected internal override SyntaxNode RemoveNodesCore(IEnumerable<SyntaxNode> nodes, SyntaxRemoveOptions options)
        {
            return SyntaxNodeRemover.RemoveNodes(this, nodes.Cast<CSharpSyntaxNode>(), options).AsRootOfNewTreeWithOptionsFrom(this.SyntaxTree);
        }

        protected internal override SyntaxNode NormalizeWhitespaceCore(string indentation, string eol, bool elasticTrivia)
        {
            return SyntaxNormalizer.Normalize(this, indentation, eol, elasticTrivia).AsRootOfNewTreeWithOptionsFrom(this.SyntaxTree);
        }

        protected override bool IsEquivalentToCore(SyntaxNode node, bool topLevel = false)
        {
            return SyntaxFactory.AreEquivalent(this, (CSharpSyntaxNode)node, topLevel);
        }

        internal override bool ShouldCreateWeakList()
        {
            if (this.Kind() == SyntaxKind.Block)
            {
                var parent = this.Parent;
                if (parent is MemberDeclarationSyntax || parent is AccessorDeclarationSyntax)
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

        string IFormattable.ToString(string format, IFormatProvider formatProvider)
        {
            return ToString();
        }
    }
}
