// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
#pragma warning disable RS0010
    /// <summary>
    /// Represents a non-terminal node in the syntax tree. This is the language agnostic equivalent of <see
    /// cref="T:Microsoft.CodeAnalysis.CSharp.SyntaxNode"/> and <see cref="T:Microsoft.CodeAnalysis.VisualBasic.SyntaxNode"/>.
    /// </summary>
#pragma warning restore RS0010
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    public abstract partial class SyntaxNode
    {
        private readonly SyntaxNode _parent;
        internal SyntaxTree _syntaxTree;

        internal SyntaxNode(GreenNode green, SyntaxNode parent, int position)
        {
            Debug.Assert(position >= 0, "position cannot be negative");
            Debug.Assert(parent?.Green.IsList != true, "list cannot be a parent");

            Position = position;
            Green = green;
            _parent = parent;
        }

        /// <summary>
        /// Used by structured trivia which has "parent == null", and therefore must know its
        /// SyntaxTree explicitly when created.
        /// </summary>
        internal SyntaxNode(GreenNode green, int position, SyntaxTree syntaxTree)
            : this(green, null, position)
        {
            this._syntaxTree = syntaxTree;
        }

        internal abstract AbstractSyntaxNavigator Navigator { get; }

        private string GetDebuggerDisplay()
        {
            return GetType().Name + " " + KindText + " " + ToString();
        }

        /// <summary>
        /// An integer representing the language specific kind of this node.
        /// </summary>
        public int RawKind => Green.RawKind;

        protected abstract string KindText { get; }

        /// <summary>
        /// The language name that this node is syntax of.
        /// </summary>
        public abstract string Language { get; }

        internal GreenNode Green { get; }

        internal int Position { get; }

        internal int EndPosition => Position + Green.FullWidth;

        /// <summary>
        /// Returns SyntaxTree that owns the node or null if node does not belong to a
        /// SyntaxTree
        /// </summary>
        public SyntaxTree SyntaxTree => this.SyntaxTreeCore;

        internal bool IsList => this.Green.IsList;

        /// <summary>
        /// The absolute span of this node in characters, including its leading and trailing trivia.
        /// </summary>
        public TextSpan FullSpan => new TextSpan(this.Position, this.Green.FullWidth);

        internal int SlotCount => this.Green.SlotCount;

        /// <summary>
        /// The absolute span of this node in characters, not including its leading and trailing trivia.
        /// </summary>
        public TextSpan Span
        {
            get
            {
                // Start with the full span.
                var start = Position;
                var width = this.Green.FullWidth;

                // adjust for preceding trivia (avoid calling this twice, do not call Green.Width)
                var precedingWidth = this.Green.GetLeadingTriviaWidth();
                start += precedingWidth;
                width -= precedingWidth;

                // adjust for following trivia width
                width -= this.Green.GetTrailingTriviaWidth();

                Debug.Assert(width >= 0);
                return new TextSpan(start, width);
            }
        }

        /// <summary>
        /// Same as accessing <see cref="TextSpan.Start"/> on <see cref="Span"/>.
        /// </summary>
        /// <remarks>
        /// Slight performance improvement.
        /// </remarks>
        public int SpanStart => Position + Green.GetLeadingTriviaWidth();

        /// <summary>
        /// The width of the node in characters, not including leading and trailing trivia.
        /// </summary>
        /// <remarks>
        /// The Width property returns the same value as Span.Length, but is somewhat more efficient.
        /// </remarks>
        internal int Width => this.Green.Width;

        /// <summary>
        /// The complete width of the node in characters, including leading and trailing trivia.
        /// </summary>
        /// <remarks>The FullWidth property returns the same value as FullSpan.Length, but is
        /// somewhat more efficient.</remarks>
        internal int FullWidth => this.Green.FullWidth;

        // this is used in cases where we know that a child is a node of particular type.
        internal SyntaxNode GetRed(ref SyntaxNode field, int slot)
        {
            var result = field;

            if (result == null)
            {
                var green = this.Green.GetSlot(slot);
                if (green != null)
                {
                    result = green.CreateRed(this, this.GetChildPosition(slot));
                    result = Interlocked.CompareExchange(ref field, result, null) ?? result;
                }
            }

            return result;
        }

        // special case of above function where slot = 0, does not need GetChildPosition 
        internal SyntaxNode GetRedAtZero(ref SyntaxNode field)
        {
            var result = field;

            if (result == null)
            {
                var green = this.Green.GetSlot(0);
                if (green != null)
                {
                    result = green.CreateRed(this, this.Position);
                    result = Interlocked.CompareExchange(ref field, result, null) ?? result;
                }
            }

            return result;
        }

        protected T GetRed<T>(ref T field, int slot) where T : SyntaxNode
        {
            var result = field;

            if (result == null)
            {
                var green = this.Green.GetSlot(slot);
                if (green != null)
                {
                    result = (T)green.CreateRed(this, this.GetChildPosition(slot));
                    result = Interlocked.CompareExchange(ref field, result, null) ?? result;
                }
            }

            return result;
        }

        // special case of above function where slot = 0, does not need GetChildPosition 
        protected T GetRedAtZero<T>(ref T field) where T : SyntaxNode
        {
            var result = field;

            if (result == null)
            {
                var green = this.Green.GetSlot(0);
                if (green != null)
                {
                    result = (T)green.CreateRed(this, this.Position);
                    result = Interlocked.CompareExchange(ref field, result, null) ?? result;
                }
            }

            return result;
        }

        /// <summary>
        /// This works the same as GetRed, but intended to be used in lists
        /// The only difference is that the public parent of the node is not the list, 
        /// but the list's parent. (element's grand parent).
        /// </summary>
        internal SyntaxNode GetRedElement(ref SyntaxNode element, int slot)
        {
            Debug.Assert(this.IsList);

            var result = element;

            if (result == null)
            {
                var green = this.Green.GetSlot(slot);
                result = green.CreateRed(this.Parent, this.GetChildPosition(slot)); // <- passing list's parent
                if (Interlocked.CompareExchange(ref element, result, null) != null)
                {
                    result = element;
                }
            }

            return result;
        }

        /// <summary>
        /// special cased helper for 2 and 3 children lists where child #1 may map to a token
        /// </summary>
        internal SyntaxNode GetRedElementIfNotToken(ref SyntaxNode element)
        {
            Debug.Assert(this.IsList);

            var result = element;

            if (result == null)
            {
                var green = this.Green.GetSlot(1);
                if (!green.IsToken)
                {
                    result = green.CreateRed(this.Parent, this.GetChildPosition(1)); // <- passing list's parent
                    if (Interlocked.CompareExchange(ref element, result, null) != null)
                    {
                        result = element;
                    }
                }
            }

            return result;
        }

        internal SyntaxNode GetWeakRedElement(ref WeakReference<SyntaxNode> slot, int index)
        {
            SyntaxNode value = null;
            if (slot?.TryGetTarget(out value) == true)
            {
                return value;
            }

            return CreateWeakItem(ref slot, index);
        }

        // handle a miss
        private SyntaxNode CreateWeakItem(ref WeakReference<SyntaxNode> slot, int index)
        {
            var greenChild = this.Green.GetSlot(index);
            var newNode = greenChild.CreateRed(this.Parent, GetChildPosition(index));
            var newWeakReference = new WeakReference<SyntaxNode>(newNode);

            while (true)
            {
                SyntaxNode previousNode = null;
                WeakReference<SyntaxNode> previousWeakReference = slot;
                if (previousWeakReference?.TryGetTarget(out previousNode) == true)
                {
                    return previousNode;
                }

                if (Interlocked.CompareExchange(ref slot, newWeakReference, previousWeakReference) == previousWeakReference)
                {
                    return newNode;
                }
            }
        }


        /// <summary>
        /// Returns the string representation of this node, not including its leading and trailing trivia.
        /// </summary>
        /// <returns>The string representation of this node, not including its leading and trailing trivia.</returns>
        /// <remarks>The length of the returned string is always the same as Span.Length</remarks>
        public abstract override string ToString();

        /// <summary>
        /// Returns full string representation of this node including its leading and trailing trivia.
        /// </summary>
        /// <returns>The full string representation of this node including its leading and trailing trivia.</returns>
        /// <remarks>The length of the returned string is always the same as FullSpan.Length</remarks>
        public abstract string ToFullString();

        /// <summary>
        /// Writes the full text of this node to the specified <see cref="TextWriter"/>.
        /// </summary>
        public abstract void WriteTo(TextWriter writer);

        /// <summary>
        /// Gets the full text of this node as an new <see cref="SourceText"/> instance.
        /// </summary>
        /// <param name="encoding">
        /// Encoding of the file that the text was read from or is going to be saved to.
        /// <c>null</c> if the encoding is unspecified.
        /// If the encoding is not specified the <see cref="SourceText"/> isn't debuggable.
        /// If an encoding-less <see cref="SourceText"/> is written to a file a <see cref="Encoding.UTF8"/> shall be used as a default.
        /// </param>
        /// <param name="checksumAlgorithm">
        /// Hash algorithm to use to calculate checksum of the text that's saved to PDB.
        /// </param>
        /// <exception cref="ArgumentException"><paramref name="checksumAlgorithm"/> is not supported.</exception>
        public SourceText GetText(Encoding encoding = null, SourceHashAlgorithm checksumAlgorithm = SourceHashAlgorithm.Sha1)
        {
            var builder = new StringBuilder();
            this.WriteTo(new StringWriter(builder));
            return new StringBuilderText(builder, encoding, checksumAlgorithm);
        }

        /// <summary>
        /// Determine whether this node is structurally equivalent to another.
        /// </summary>
        public bool IsEquivalentTo(SyntaxNode other)
        {
            return EquivalentToCore(other);
        }

        /// <summary>
        /// Determines whether the node represents a language construct that was actually parsed
        /// from the source code. Missing nodes are generated by the parser in error scenarios to
        /// represent constructs that should have been present in the source code in order to
        /// compile successfully but were actually missing.
        /// </summary>
        public bool IsMissing
        {
            get
            {
                return this.Green.IsMissing;
            }
        }

        /// <summary>
        /// Determines whether this node is a descendant of a structured trivia.
        /// </summary>
        public bool IsPartOfStructuredTrivia()
        {
            for (var node = this; node != null; node = node.Parent)
            {
                if (node.IsStructuredTrivia)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether this node represents a structured trivia.
        /// </summary>
        public bool IsStructuredTrivia
        {
            get
            {
                return this.Green.IsStructuredTrivia;
            }
        }

        /// <summary>
        /// Determines whether a descendant trivia of this node is structured.
        /// </summary>
        public bool HasStructuredTrivia
        {
            get
            {
                return this.Green.ContainsStructuredTrivia && !this.Green.IsStructuredTrivia;
            }
        }

        /// <summary>
        /// Determines whether this node has any descendant skipped text.
        /// </summary>
        public bool ContainsSkippedText
        {
            get
            {
                return this.Green.ContainsSkippedText;
            }
        }

        /// <summary>
        /// Determines whether this node has any descendant preprocessor directives.
        /// </summary>
        public bool ContainsDirectives
        {
            get
            {
                return this.Green.ContainsDirectives;
            }
        }

        /// <summary>
        /// Determines whether this node or any of its descendant nodes, tokens or trivia have any diagnostics on them. 
        /// </summary>
        public bool ContainsDiagnostics
        {
            get
            {
                return this.Green.ContainsDiagnostics;
            }
        }

        /// <summary>
        /// Determines if the specified node is a descendant of this node.
        /// </summary>
        public bool Contains(SyntaxNode node)
        {
            if (node == null || !this.FullSpan.Contains(node.FullSpan))
            {
                return false;
            }

            while (node != null)
            {
                if (node == this)
                {
                    return true;
                }

                if (node.Parent != null)
                {
                    node = node.Parent;
                }
                else if (node.IsStructuredTrivia)
                {
                    node = ((IStructuredTriviaSyntax)node).ParentTrivia.Token.Parent;
                }
                else
                {
                    node = null;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines whether this node has any leading trivia.
        /// </summary>
        public bool HasLeadingTrivia
        {
            get
            {
                return this.GetLeadingTrivia().Count > 0;
            }
        }

        /// <summary>
        /// Determines whether this node has any trailing trivia.
        /// </summary>
        public bool HasTrailingTrivia
        {
            get
            {
                return this.GetTrailingTrivia().Count > 0;
            }
        }

        /// <summary>
        /// Gets a node at given node index without forcing its creation.
        /// If node was not created it would return null.
        /// </summary>
        internal abstract SyntaxNode GetCachedSlot(int index);

        internal int GetChildIndex(int slot)
        {
            int index = 0;
            for (int i = 0; i < slot; i++)
            {
                var item = this.Green.GetSlot(i);
                if (item != null)
                {
                    if (item.IsList)
                    {
                        index += item.SlotCount;
                    }
                    else
                    {
                        index++;
                    }
                }
            }

            return index;
        }

        /// <summary>
        /// This function calculates the offset of a child at given position. It is very common that
        /// some children to the left of the given index already know their positions so we first
        /// check if that is the case. In a worst case the cost is O(n), but it is not generally an
        /// issue because number of children in regular nodes is fixed and small. In a case where
        /// the number of children could be large (lists) this function is overridden with more
        /// efficient implementations.
        /// </summary>
        internal virtual int GetChildPosition(int index)
        {
            int offset = 0;
            var green = this.Green;
            while (index > 0)
            {
                index--;
                var prevSibling = this.GetCachedSlot(index);
                if (prevSibling != null)
                {
                    return prevSibling.EndPosition + offset;
                }
                var greenChild = green.GetSlot(index);
                if (greenChild != null)
                {
                    offset += greenChild.FullWidth;
                }
            }

            return this.Position + offset;
        }

        public Location GetLocation()
        {
            return this.SyntaxTree.GetLocation(this.Span);
        }

        /// <summary>
        /// Gets a list of all the diagnostics in the sub tree that has this node as its root.
        /// This method does not filter diagnostics based on #pragmas and compiler options
        /// like nowarn, warnaserror etc.
        /// </summary>
        public IEnumerable<Diagnostic> GetDiagnostics()
        {
            return this.SyntaxTree.GetDiagnostics(this);
        }

        /// <summary>
        /// Gets a <see cref="SyntaxReference"/> for this syntax node. CommonSyntaxReferences can be used to
        /// regain access to a syntax node without keeping the entire tree and source text in
        /// memory.
        /// </summary>
        public SyntaxReference GetReference()
        {
            return this.SyntaxTree.GetReference(this);
        }

        /// <summary>
        /// When invoked on a node that represents an anonymous function or a query clause [1]
        /// with a <paramref name="body"/> of another anonymous function or a query clause of the same kind [2], 
        /// returns the body of the [1] that positionally corresponds to the specified <paramref name="body"/>.
        /// 
        /// E.g. join clause declares left expression and right expression -- each of these expressions is a lambda body.
        /// JoinClause1.GetCorrespondingLambdaBody(JoinClause2.RightExpression) returns JoinClause1.RightExpression.
        /// </summary>
        internal abstract SyntaxNode TryGetCorrespondingLambdaBody(SyntaxNode body);

        internal abstract SyntaxNode GetLambda();

        #region Node Lookup

        /// <summary>
        /// The node that contains this node in its <see cref="ChildNodes"/> collection.
        /// </summary>
        public SyntaxNode Parent
        {
            get
            {
                return _parent;
            }
        }

        public virtual SyntaxTrivia ParentTrivia
        {
            get
            {
                return default(SyntaxTrivia);
            }
        }

        internal SyntaxNode ParentOrStructuredTriviaParent
        {
            get
            {
                return GetParent(this, ascendOutOfTrivia: true);
            }
        }

        /// <summary>
        /// The list of child nodes and tokens of this node, where each element is a SyntaxNodeOrToken instance.
        /// </summary>
        public ChildSyntaxList ChildNodesAndTokens()
        {
            return new ChildSyntaxList(this);
        }

        public abstract SyntaxNodeOrToken ChildThatContainsPosition(int position);

        /// <summary>
        /// Gets node at given node index. 
        /// This WILL force node creation if node has not yet been created.
        /// </summary>
        internal abstract SyntaxNode GetNodeSlot(int slot);

        /// <summary>
        /// Gets a list of the child nodes in prefix document order.
        /// </summary>
        public IEnumerable<SyntaxNode> ChildNodes()
        {
            foreach (var nodeOrToken in this.ChildNodesAndTokens())
            {
                if (nodeOrToken.IsNode)
                {
                    yield return nodeOrToken.AsNode();
                }
            }
        }

        /// <summary>
        /// Gets a list of ancestor nodes
        /// </summary>
        public IEnumerable<SyntaxNode> Ancestors(bool ascendOutOfTrivia = true)
        {
            return this.Parent?
                .AncestorsAndSelf(ascendOutOfTrivia) ??
                SpecializedCollections.EmptyEnumerable<SyntaxNode>();
        }

        /// <summary>
        /// Gets a list of ancestor nodes (including this node) 
        /// </summary>
        public IEnumerable<SyntaxNode> AncestorsAndSelf(bool ascendOutOfTrivia = true)
        {
            for (var node = this; node != null; node = GetParent(node, ascendOutOfTrivia))
            {
                yield return node;
            }
        }

        private static SyntaxNode GetParent(SyntaxNode node, bool ascendOutOfTrivia)
        {
            var parent = node.Parent;
            if (parent == null && ascendOutOfTrivia)
            {
                var structuredTrivia = node as IStructuredTriviaSyntax;
                if (structuredTrivia != null)
                {
                    parent = structuredTrivia.ParentTrivia.Token.Parent;
                }
            }

            return parent;
        }

        /// <summary>
        /// Gets the first node of type TNode that matches the predicate.
        /// </summary>
        public TNode FirstAncestorOrSelf<TNode>(Func<TNode, bool> predicate = null, bool ascendOutOfTrivia = true)
            where TNode : SyntaxNode
        {
            for (var node = this; node != null; node = GetParent(node, ascendOutOfTrivia))
            {
                var tnode = node as TNode;
                if (tnode != null && (predicate == null || predicate(tnode)))
                {
                    return tnode;
                }
            }

            return default(TNode);
        }

        /// <summary>
        /// Gets a list of descendant nodes in prefix document order.
        /// </summary>
        /// <param name="descendIntoChildren">An optional function that determines if the search descends into the argument node's children.</param>
        /// <param name="descendIntoTrivia">Determines if nodes that are part of structured trivia are included in the list.</param>
        public IEnumerable<SyntaxNode> DescendantNodes(Func<SyntaxNode, bool> descendIntoChildren = null, bool descendIntoTrivia = false)
        {
            return DescendantNodesImpl(this.FullSpan, descendIntoChildren, descendIntoTrivia, includeSelf: false);
        }

        /// <summary>
        /// Gets a list of descendant nodes in prefix document order.
        /// </summary>
        /// <param name="span">The span the node's full span must intersect.</param>
        /// <param name="descendIntoChildren">An optional function that determines if the search descends into the argument node's children.</param>
        /// <param name="descendIntoTrivia">Determines if nodes that are part of structured trivia are included in the list.</param>
        public IEnumerable<SyntaxNode> DescendantNodes(TextSpan span, Func<SyntaxNode, bool> descendIntoChildren = null, bool descendIntoTrivia = false)
        {
            return DescendantNodesImpl(span, descendIntoChildren, descendIntoTrivia, includeSelf: false);
        }

        /// <summary>
        /// Gets a list of descendant nodes (including this node) in prefix document order.
        /// </summary>
        /// <param name="descendIntoChildren">An optional function that determines if the search descends into the argument node's children.</param>
        /// <param name="descendIntoTrivia">Determines if nodes that are part of structured trivia are included in the list.</param>
        public IEnumerable<SyntaxNode> DescendantNodesAndSelf(Func<SyntaxNode, bool> descendIntoChildren = null, bool descendIntoTrivia = false)
        {
            return DescendantNodesImpl(this.FullSpan, descendIntoChildren, descendIntoTrivia, includeSelf: true);
        }

        /// <summary>
        /// Gets a list of descendant nodes (including this node) in prefix document order.
        /// </summary>
        /// <param name="span">The span the node's full span must intersect.</param>
        /// <param name="descendIntoChildren">An optional function that determines if the search descends into the argument node's children.</param>
        /// <param name="descendIntoTrivia">Determines if nodes that are part of structured trivia are included in the list.</param>
        public IEnumerable<SyntaxNode> DescendantNodesAndSelf(TextSpan span, Func<SyntaxNode, bool> descendIntoChildren = null, bool descendIntoTrivia = false)
        {
            return DescendantNodesImpl(span, descendIntoChildren, descendIntoTrivia, includeSelf: true);
        }

        /// <summary>
        /// Gets a list of descendant nodes and tokens in prefix document order.
        /// </summary>
        /// <param name="descendIntoChildren">An optional function that determines if the search descends into the argument node's children.</param>
        /// <param name="descendIntoTrivia">Determines if nodes that are part of structured trivia are included in the list.</param>
        public IEnumerable<SyntaxNodeOrToken> DescendantNodesAndTokens(Func<SyntaxNode, bool> descendIntoChildren = null, bool descendIntoTrivia = false)
        {
            return DescendantNodesAndTokensImpl(this.FullSpan, descendIntoChildren, descendIntoTrivia, includeSelf: false);
        }

        /// <summary>
        /// Gets a list of the descendant nodes and tokens in prefix document order.
        /// </summary>
        /// <param name="span">The span the node's full span must intersect.</param>
        /// <param name="descendIntoChildren">An optional function that determines if the search descends into the argument node's children.</param>
        /// <param name="descendIntoTrivia">Determines if nodes that are part of structured trivia are included in the list.</param>
        public IEnumerable<SyntaxNodeOrToken> DescendantNodesAndTokens(TextSpan span, Func<SyntaxNode, bool> descendIntoChildren = null, bool descendIntoTrivia = false)
        {
            return DescendantNodesAndTokensImpl(span, descendIntoChildren, descendIntoTrivia, includeSelf: false);
        }

        /// <summary>
        /// Gets a list of descendant nodes and tokens (including this node) in prefix document order.
        /// </summary>
        /// <param name="descendIntoChildren">An optional function that determines if the search descends into the argument node's children.</param>
        /// <param name="descendIntoTrivia">Determines if nodes that are part of structured trivia are included in the list.</param>
        public IEnumerable<SyntaxNodeOrToken> DescendantNodesAndTokensAndSelf(Func<SyntaxNode, bool> descendIntoChildren = null, bool descendIntoTrivia = false)
        {
            return DescendantNodesAndTokensImpl(this.FullSpan, descendIntoChildren, descendIntoTrivia, includeSelf: true);
        }

        /// <summary>
        /// Gets a list of the descendant nodes and tokens (including this node) in prefix document order.
        /// </summary>
        /// <param name="span">The span the node's full span must intersect.</param>
        /// <param name="descendIntoChildren">An optional function that determines if the search descends into the argument node's children.</param>
        /// <param name="descendIntoTrivia">Determines if nodes that are part of structured trivia are included in the list.</param>
        public IEnumerable<SyntaxNodeOrToken> DescendantNodesAndTokensAndSelf(TextSpan span, Func<SyntaxNode, bool> descendIntoChildren = null, bool descendIntoTrivia = false)
        {
            return DescendantNodesAndTokensImpl(span, descendIntoChildren, descendIntoTrivia, includeSelf: true);
        }

        /// <summary>
        /// Finds the node with the smallest <see cref="FullSpan"/> that contains <paramref name="span"/>.
        /// <paramref name="getInnermostNodeForTie"/> is used to determine the behavior in case of a tie (i.e. a node having the same span as its parent).
        /// If <paramref name="getInnermostNodeForTie"/> is true, then it returns lowest descending node encompassing the given <paramref name="span"/>.
        /// Otherwise, it returns the outermost node encompassing the given <paramref name="span"/>.
        /// </summary>
        /// <remarks>
        /// TODO: This should probably be reimplemented with <see cref="ChildThatContainsPosition"/>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">This exception is thrown if <see cref="FullSpan"/> doesn't contain the given span.</exception>
        public SyntaxNode FindNode(TextSpan span, bool findInsideTrivia = false, bool getInnermostNodeForTie = false)
        {
            if (!this.FullSpan.Contains(span))
            {
                throw new ArgumentOutOfRangeException(nameof(span));
            }

            var node = FindToken(span.Start, findInsideTrivia)
                .Parent
                .FirstAncestorOrSelf<SyntaxNode>(a => a.FullSpan.Contains(span));

            var cuRoot = node.SyntaxTree?.GetRoot();

            // Tie-breaking.
            if (!getInnermostNodeForTie)
            {
                while (true)
                {
                    var parent = node.Parent;
                    // NOTE: We care about FullSpan equality, but FullWidth is cheaper and equivalent.
                    if (parent == null || parent.FullWidth != node.FullWidth) break;
                    // prefer child over compilation unit
                    if (parent == cuRoot) break;
                    node = parent;
                }
            }

            return node;
        }

        #endregion

        #region Token Lookup
        /// <summary>
        /// Finds a descendant token of this node whose span includes the supplied position. 
        /// </summary>
        /// <param name="position">The character position of the token relative to the beginning of the file.</param>
        /// <param name="findInsideTrivia">
        /// True to return tokens that are part of trivia. If false finds the token whose full span (including trivia)
        /// includes the position.
        /// </param>
        public SyntaxToken FindToken(int position, bool findInsideTrivia = false)
        {
            return FindTokenCore(position, findInsideTrivia);
        }

        /// <summary>
        /// Gets the first token of the tree rooted by this node. Skips zero-width tokens.
        /// </summary>
        /// <returns>The first token or <c>default(SyntaxToken)</c> if it doesn't exist.</returns>
        public SyntaxToken GetFirstToken(bool includeZeroWidth = false, bool includeSkipped = false, bool includeDirectives = false, bool includeDocumentationComments = false)
        {
            return this.Navigator.GetFirstToken(this, includeZeroWidth, includeSkipped, includeDirectives, includeDocumentationComments);
        }

        /// <summary>
        /// Gets the last token of the tree rooted by this node. Skips zero-width tokens.
        /// </summary>
        /// <returns>The last token or <c>default(SyntaxToken)</c> if it doesn't exist.</returns>
        public SyntaxToken GetLastToken(bool includeZeroWidth = false, bool includeSkipped = false, bool includeDirectives = false, bool includeDocumentationComments = false)
        {
            return this.Navigator.GetLastToken(this, includeZeroWidth, includeSkipped, includeDirectives, includeDocumentationComments);
        }

        /// <summary>
        /// Gets a list of the direct child tokens of this node.
        /// </summary>
        public IEnumerable<SyntaxToken> ChildTokens()
        {
            foreach (var nodeOrToken in this.ChildNodesAndTokens())
            {
                if (nodeOrToken.IsToken)
                {
                    yield return nodeOrToken.AsToken();
                }
            }
        }

        /// <summary>
        /// Gets a list of all the tokens in the span of this node.
        /// </summary>
        public IEnumerable<SyntaxToken> DescendantTokens(Func<SyntaxNode, bool> descendIntoChildren = null, bool descendIntoTrivia = false)
        {
            return this.DescendantNodesAndTokens(descendIntoChildren, descendIntoTrivia).Where(sn => sn.IsToken).Select(sn => sn.AsToken());
        }

        /// <summary>
        /// Gets a list of all the tokens in the full span of this node.
        /// </summary>
        public IEnumerable<SyntaxToken> DescendantTokens(TextSpan span, Func<SyntaxNode, bool> descendIntoChildren = null, bool descendIntoTrivia = false)
        {
            return this.DescendantNodesAndTokens(span, descendIntoChildren, descendIntoTrivia).Where(sn => sn.IsToken).Select(sn => sn.AsToken());
        }

        #endregion

        #region Trivia Lookup
        /// <summary>
        /// The list of trivia that appears before this node in the source code and are attached to a token that is a
        /// descendant of this node.
        /// </summary>
        public SyntaxTriviaList GetLeadingTrivia()
        {
            return GetFirstToken(includeZeroWidth: true).LeadingTrivia;
        }

        /// <summary>
        /// The list of trivia that appears after this node in the source code and are attached to a token that is a
        /// descendant of this node.
        /// </summary>
        public SyntaxTriviaList GetTrailingTrivia()
        {
            return GetLastToken(includeZeroWidth: true).TrailingTrivia;
        }

        /// <summary>
        /// Finds a descendant trivia of this node whose span includes the supplied position.
        /// </summary>
        /// <param name="position">The character position of the trivia relative to the beginning of the file.</param>
        /// <param name="findInsideTrivia">
        /// True to return tokens that are part of trivia. If false finds the token whose full span (including trivia)
        /// includes the position.
        /// </param>
        public SyntaxTrivia FindTrivia(int position, bool findInsideTrivia = false)
        {
            return FindTrivia(position, findInsideTrivia ? SyntaxTrivia.Any : null);
        }

        /// <summary>
        /// Finds a descendant trivia of this node at the specified position, where the position is
        /// within the span of the node.
        /// </summary>
        /// <param name="position">The character position of the trivia relative to the beginning of
        /// the file.</param>
        /// <param name="stepInto">Specifies a function that determines per trivia node, whether to
        /// descend into structured trivia of that node.</param>
        /// <returns></returns>
        public SyntaxTrivia FindTrivia(int position, Func<SyntaxTrivia, bool> stepInto)
        {
            if (this.FullSpan.Contains(position))
            {
                return FindTriviaByOffset(this, position - this.Position, stepInto);
            }

            return default(SyntaxTrivia);
        }

        internal static SyntaxTrivia FindTriviaByOffset(SyntaxNode node, int textOffset, Func<SyntaxTrivia, bool> stepInto = null)
        {
recurse:
            if (textOffset >= 0)
            {
                foreach (var element in node.ChildNodesAndTokens())
                {
                    var fullWidth = element.FullWidth;
                    if (textOffset < fullWidth)
                    {
                        if (element.IsNode)
                        {
                            node = element.AsNode();
                            goto recurse;
                        }
                        else if (element.IsToken)
                        {
                            var token = element.AsToken();
                            var leading = token.LeadingWidth;
                            if (textOffset < token.LeadingWidth)
                            {
                                foreach (var trivia in token.LeadingTrivia)
                                {
                                    if (textOffset < trivia.FullWidth)
                                    {
                                        if (trivia.HasStructure && stepInto != null && stepInto(trivia))
                                        {
                                            node = trivia.GetStructure();
                                            goto recurse;
                                        }

                                        return trivia;
                                    }

                                    textOffset -= trivia.FullWidth;
                                }
                            }
                            else if (textOffset >= leading + token.Width)
                            {
                                textOffset -= leading + token.Width;
                                foreach (var trivia in token.TrailingTrivia)
                                {
                                    if (textOffset < trivia.FullWidth)
                                    {
                                        if (trivia.HasStructure && stepInto != null && stepInto(trivia))
                                        {
                                            node = trivia.GetStructure();
                                            goto recurse;
                                        }

                                        return trivia;
                                    }

                                    textOffset -= trivia.FullWidth;
                                }
                            }

                            return default(SyntaxTrivia);
                        }
                    }

                    textOffset -= fullWidth;
                }
            }

            return default(SyntaxTrivia);
        }

        /// <summary>
        /// Get a list of all the trivia associated with the descendant nodes and tokens.
        /// </summary>
        public IEnumerable<SyntaxTrivia> DescendantTrivia(Func<SyntaxNode, bool> descendIntoChildren = null, bool descendIntoTrivia = false)
        {
            return DescendantTriviaImpl(this.FullSpan, descendIntoChildren, descendIntoTrivia);
        }

        /// <summary>
        /// Get a list of all the trivia associated with the descendant nodes and tokens.
        /// </summary>
        public IEnumerable<SyntaxTrivia> DescendantTrivia(TextSpan span, Func<SyntaxNode, bool> descendIntoChildren = null, bool descendIntoTrivia = false)
        {
            return DescendantTriviaImpl(span, descendIntoChildren, descendIntoTrivia);
        }

        #endregion

        #region Annotations

        /// <summary>
        /// Determines whether this node or any sub node, token or trivia has annotations.
        /// </summary>
        public bool ContainsAnnotations
        {
            get { return this.Green.ContainsAnnotations; }
        }

        /// <summary>
        /// Determines whether this node has any annotations with the specific annotation kind.
        /// </summary>
        public bool HasAnnotations(string annotationKind)
        {
            return this.Green.HasAnnotations(annotationKind);
        }

        /// <summary>
        /// Determines whether this node has any annotations with any of the specific annotation kinds.
        /// </summary>
        public bool HasAnnotations(IEnumerable<string> annotationKinds)
        {
            return this.Green.HasAnnotations(annotationKinds);
        }

        /// <summary>
        /// Determines whether this node has the specific annotation.
        /// </summary>
        public bool HasAnnotation(SyntaxAnnotation annotation)
        {
            return this.Green.HasAnnotation(annotation);
        }

        /// <summary>
        /// Gets all the annotations with the specified annotation kind. 
        /// </summary>
        public IEnumerable<SyntaxAnnotation> GetAnnotations(string annotationKind)
        {
            return this.Green.GetAnnotations(annotationKind);
        }

        /// <summary>
        /// Gets all the annotations with the specified annotation kinds. 
        /// </summary>
        public IEnumerable<SyntaxAnnotation> GetAnnotations(IEnumerable<string> annotationKinds)
        {
            return this.Green.GetAnnotations(annotationKinds);
        }

        internal SyntaxAnnotation[] GetAnnotations()
        {
            return this.Green.GetAnnotations();
        }

        /// <summary>
        /// Gets all nodes and tokens with an annotation of the specified annotation kind.
        /// </summary>
        public IEnumerable<SyntaxNodeOrToken> GetAnnotatedNodesAndTokens(string annotationKind)
        {
            return this.DescendantNodesAndTokensAndSelf(n => n.ContainsAnnotations, descendIntoTrivia: true)
                .Where(t => t.HasAnnotations(annotationKind));
        }

        /// <summary>
        /// Gets all nodes and tokens with an annotation of the specified annotation kinds.
        /// </summary>
        public IEnumerable<SyntaxNodeOrToken> GetAnnotatedNodesAndTokens(params string[] annotationKinds)
        {
            return this.DescendantNodesAndTokensAndSelf(n => n.ContainsAnnotations, descendIntoTrivia: true)
                .Where(t => t.HasAnnotations(annotationKinds));
        }

        /// <summary>
        /// Gets all nodes and tokens with the specified annotation.
        /// </summary>
        public IEnumerable<SyntaxNodeOrToken> GetAnnotatedNodesAndTokens(SyntaxAnnotation annotation)
        {
            return this.DescendantNodesAndTokensAndSelf(n => n.ContainsAnnotations, descendIntoTrivia: true)
                .Where(t => t.HasAnnotation(annotation));
        }

        /// <summary>
        /// Gets all nodes with the specified annotation.
        /// </summary>
        public IEnumerable<SyntaxNode> GetAnnotatedNodes(SyntaxAnnotation syntaxAnnotation)
        {
            return this.GetAnnotatedNodesAndTokens(syntaxAnnotation).Where(n => n.IsNode).Select(n => n.AsNode());
        }

        /// <summary>
        /// Gets all nodes with the specified annotation kind.
        /// </summary>
        /// <param name="annotationKind"></param>
        /// <returns></returns>
        public IEnumerable<SyntaxNode> GetAnnotatedNodes(string annotationKind)
        {
            return this.GetAnnotatedNodesAndTokens(annotationKind).Where(n => n.IsNode).Select(n => n.AsNode());
        }

        /// <summary>
        /// Gets all tokens with the specified annotation.
        /// </summary>
        public IEnumerable<SyntaxToken> GetAnnotatedTokens(SyntaxAnnotation syntaxAnnotation)
        {
            return this.GetAnnotatedNodesAndTokens(syntaxAnnotation).Where(n => n.IsToken).Select(n => n.AsToken());
        }

        /// <summary>
        /// Gets all tokens with the specified annotation kind.
        /// </summary>
        public IEnumerable<SyntaxToken> GetAnnotatedTokens(string annotationKind)
        {
            return this.GetAnnotatedNodesAndTokens(annotationKind).Where(n => n.IsToken).Select(n => n.AsToken());
        }

        /// <summary>
        /// Gets all trivia with an annotation of the specified annotation kind.
        /// </summary>
        public IEnumerable<SyntaxTrivia> GetAnnotatedTrivia(string annotationKind)
        {
            return this.DescendantTrivia(n => n.ContainsAnnotations, descendIntoTrivia: true)
                       .Where(tr => tr.HasAnnotations(annotationKind));
        }

        /// <summary>
        /// Gets all trivia with an annotation of the specified annotation kinds.
        /// </summary>
        public IEnumerable<SyntaxTrivia> GetAnnotatedTrivia(params string[] annotationKinds)
        {
            return this.DescendantTrivia(n => n.ContainsAnnotations, descendIntoTrivia: true)
                       .Where(tr => tr.HasAnnotations(annotationKinds));
        }

        /// <summary>
        /// Gets all trivia with the specified annotation.
        /// </summary>
        public IEnumerable<SyntaxTrivia> GetAnnotatedTrivia(SyntaxAnnotation annotation)
        {
            return this.DescendantTrivia(n => n.ContainsAnnotations, descendIntoTrivia: true)
                       .Where(tr => tr.HasAnnotation(annotation));
        }

        internal SyntaxNode WithAdditionalAnnotationsInternal(IEnumerable<SyntaxAnnotation> annotations)
        {
            return this.Green.WithAdditionalAnnotationsGreen(annotations).CreateRed();
        }

        internal SyntaxNode GetNodeWithoutAnnotations(IEnumerable<SyntaxAnnotation> annotations)
        {
            return this.Green.WithoutAnnotationsGreen(annotations).CreateRed();
        }

        /// <summary>
        /// Copies all SyntaxAnnotations, if any, from this SyntaxNode instance and attaches them to a new instance based on <paramref name="node" />.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If no annotations are copied, just returns <paramref name="node" />.
        /// </para>
        /// <para>
        /// It can also be used manually to preserve annotations in a more complex tree
        /// modification, even if the type of a node changes.
        /// </para>
        /// </remarks>
        public T CopyAnnotationsTo<T>(T node) where T : SyntaxNode
        {
            if (node == null)
            {
                return default(T);
            }

            var annotations = this.Green.GetAnnotations();
            if (annotations?.Length > 0)
            {
                return (T)(node.Green.WithAdditionalAnnotationsGreen(annotations)).CreateRed();
            }
            return node;
        }

        #endregion

        /// <summary>
        /// Determines if two nodes are the same, disregarding trivia differences.
        /// </summary>
        /// <param name="node">The node to compare against.</param>
        /// <param name="topLevel"> If true then the nodes are equivalent if the contained nodes and
        /// tokens declaring metadata visible symbolic information are equivalent, ignoring any
        /// differences of nodes inside method bodies or initializer expressions, otherwise all
        /// nodes and tokens must be equivalent. 
        /// </param>
        public bool IsEquivalentTo(SyntaxNode node, bool topLevel = false)
        {
            return IsEquivalentToCore(node, topLevel);
        }

        public abstract void SerializeTo(Stream stream, CancellationToken cancellationToken = default(CancellationToken));

        #region Core Methods

        /// <summary>
        /// Determine if this node is structurally equivalent to another.
        /// </summary>
        protected abstract bool EquivalentToCore(SyntaxNode other);

        /// <summary>
        /// Returns SyntaxTree that owns the node or null if node does not belong to a
        /// SyntaxTree
        /// </summary>
        protected abstract SyntaxTree SyntaxTreeCore { get; }

        /// <summary>
        /// Finds a descendant token of this node whose span includes the supplied position. 
        /// </summary>
        /// <param name="position">The character position of the token relative to the beginning of the file.</param>
        /// <param name="findInsideTrivia">
        /// True to return tokens that are part of trivia.
        /// If false finds the token whose full span (including trivia) includes the position.
        /// </param>
        protected virtual SyntaxToken FindTokenCore(int position, bool findInsideTrivia)
        {
            if (findInsideTrivia)
            {
                return this.FindToken(position, SyntaxTrivia.Any);
            }

            SyntaxToken EoF;
            if (this.TryGetEofAt(position, out EoF))
            {
                return EoF;
            }

            if (!this.FullSpan.Contains(position))
            {
                throw new ArgumentOutOfRangeException(nameof(position));
            }

            return this.FindTokenInternal(position);
        }

        private bool TryGetEofAt(int position, out SyntaxToken Eof)
        {
            if (position == this.EndPosition)
            {
                var compilationUnit = this as ICompilationUnitSyntax;
                if (compilationUnit != null)
                {
                    Eof = compilationUnit.EndOfFileToken;
                    Debug.Assert(Eof.EndPosition == position);
                    return true;
                }
            }

            Eof = default(SyntaxToken);
            return false;
        }

        internal SyntaxToken FindTokenInternal(int position)
        {
            // While maintaining invariant   curNode.Position <= position < curNode.FullSpan.End
            // go down the tree until a token is found
            SyntaxNodeOrToken curNode = this;

            while (true)
            {
                Debug.Assert(curNode.RawKind != 0);
                Debug.Assert(curNode.FullSpan.Contains(position));

                var node = curNode.AsNode();

                if (node != null)
                {
                    //find a child that includes the position
                    curNode = node.ChildThatContainsPosition(position);
                }
                else
                {
                    return curNode.AsToken();
                }
            }
        }

        private SyntaxToken FindToken(int position, Func<SyntaxTrivia, bool> findInsideTrivia)
        {
            return FindTokenCore(position, findInsideTrivia);
        }

        /// <summary>
        /// Finds a descendant token of this node whose span includes the supplied position. 
        /// </summary>
        /// <param name="position">The character position of the token relative to the beginning of the file.</param>
        /// <param name="stepInto">
        /// Applied on every structured trivia. Return false if the tokens included in the trivia should be skipped. 
        /// Pass null to skip all structured trivia.
        /// </param>
        protected virtual SyntaxToken FindTokenCore(int position, Func<SyntaxTrivia, bool> stepInto)
        {
            var token = this.FindToken(position, findInsideTrivia: false);
            if (stepInto != null)
            {
                var trivia = GetTriviaFromSyntaxToken(position, token);

                if (trivia.HasStructure && stepInto(trivia))
                {
                    token = trivia.GetStructure().FindTokenInternal(position);
                }
            }

            return token;
        }

        internal static SyntaxTrivia GetTriviaFromSyntaxToken(int position, SyntaxToken token)
        {
            var span = token.Span;
            var trivia = new SyntaxTrivia();
            if (position < span.Start && token.HasLeadingTrivia)
            {
                trivia = GetTriviaThatContainsPosition(token.LeadingTrivia, position);
            }
            else if (position >= span.End && token.HasTrailingTrivia)
            {
                trivia = GetTriviaThatContainsPosition(token.TrailingTrivia, position);
            }

            return trivia;
        }

        internal static SyntaxTrivia GetTriviaThatContainsPosition(SyntaxTriviaList list, int position)
        {
            foreach (var trivia in list)
            {
                if (trivia.FullSpan.Contains(position))
                {
                    return trivia;
                }

                if (trivia.Position > position)
                {
                    break;
                }
            }

            return default(SyntaxTrivia);
        }

        /// <summary>
        /// Finds a descendant trivia of this node whose span includes the supplied position.
        /// </summary>
        /// <param name="position">The character position of the trivia relative to the beginning of the file.</param>
        /// <param name="findInsideTrivia">Whether to search inside structured trivia.</param>
        protected virtual SyntaxTrivia FindTriviaCore(int position, bool findInsideTrivia)
        {
            return FindTrivia(position, findInsideTrivia);
        }

        /// <summary>
        /// Creates a new tree of nodes with the specified nodes, tokens or trivia replaced.
        /// </summary>
        protected internal abstract SyntaxNode ReplaceCore<TNode>(
            IEnumerable<TNode> nodes = null,
            Func<TNode, TNode, SyntaxNode> computeReplacementNode = null,
            IEnumerable<SyntaxToken> tokens = null,
            Func<SyntaxToken, SyntaxToken, SyntaxToken> computeReplacementToken = null,
            IEnumerable<SyntaxTrivia> trivia = null,
            Func<SyntaxTrivia, SyntaxTrivia, SyntaxTrivia> computeReplacementTrivia = null)
            where TNode : SyntaxNode;

        protected internal abstract SyntaxNode ReplaceNodeInListCore(SyntaxNode originalNode, IEnumerable<SyntaxNode> replacementNodes);
        protected internal abstract SyntaxNode InsertNodesInListCore(SyntaxNode nodeInList, IEnumerable<SyntaxNode> nodesToInsert, bool insertBefore);
        protected internal abstract SyntaxNode ReplaceTokenInListCore(SyntaxToken originalToken, IEnumerable<SyntaxToken> newTokens);
        protected internal abstract SyntaxNode InsertTokensInListCore(SyntaxToken originalToken, IEnumerable<SyntaxToken> newTokens, bool insertBefore);
        protected internal abstract SyntaxNode ReplaceTriviaInListCore(SyntaxTrivia originalTrivia, IEnumerable<SyntaxTrivia> newTrivia);
        protected internal abstract SyntaxNode InsertTriviaInListCore(SyntaxTrivia originalTrivia, IEnumerable<SyntaxTrivia> newTrivia, bool insertBefore);

        /// <summary>
        /// Creates a new tree of nodes with the specified node removed.
        /// </summary>
        protected internal abstract SyntaxNode RemoveNodesCore(
            IEnumerable<SyntaxNode> nodes,
            SyntaxRemoveOptions options);

        protected internal abstract SyntaxNode NormalizeWhitespaceCore(string indentation, string eol, bool elasticTrivia);

        /// <summary>
        /// Determines if two nodes are the same, disregarding trivia differences.
        /// </summary>
        /// <param name="node">The node to compare against.</param>
        /// <param name="topLevel"> If true then the nodes are equivalent if the contained nodes and
        /// tokens declaring metadata visible symbolic information are equivalent, ignoring any
        /// differences of nodes inside method bodies or initializer expressions, otherwise all
        /// nodes and tokens must be equivalent. 
        /// </param>
        protected abstract bool IsEquivalentToCore(SyntaxNode node, bool topLevel = false);
        #endregion
    }
}
