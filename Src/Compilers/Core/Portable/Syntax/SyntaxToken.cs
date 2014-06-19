// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a token in the syntax tree. This is the language agnostic equivalent of <see
    /// cref="T:Microsoft.CodeAnalysis.CSharp.SyntaxToken"/> and <see cref="T:Microsoft.CodeAnalysis.VisualBasic.SyntaxToken"/>.
    /// </summary>
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    public struct SyntaxToken : IEquatable<SyntaxToken>
    {
        internal static readonly Func<SyntaxToken, bool> NonZeroWidth = t => t.Width > 0;
        internal static readonly Func<SyntaxToken, bool> Any = t => true;

        private readonly SyntaxNode parent;
        private readonly GreenNode token;
        private readonly int position;
        private readonly int index;

        internal SyntaxToken(SyntaxNode parent, GreenNode token, int position, int index)
        {
            Debug.Assert(parent == null || !parent.Green.IsList, "list cannot be a parent");
            Debug.Assert(token == null || token.IsToken, "token must be a token");
            this.parent = parent;
            this.token = token;
            this.position = position;
            this.index = index;
        }

        internal SyntaxToken(GreenNode token)
            : this()
        {
            Debug.Assert(token == null || token.IsToken, "token must be a token");
            this.token = token;
        }

        private string GetDebuggerDisplay()
        {
            return GetType().Name + " " + (this.token != null ? this.token.KindText : "None") + " " + ToString();
        }

        /// <summary>
        /// An integer representing the language specific kind of this token.
        /// </summary>
        public int RawKind
        {
            get { return this.token != null ? this.token.RawKind : 0; }
        }

        /// <summary>
        /// The language name that this token is syntax of.
        /// </summary>
        public string Language
        {
            get
            {
                return this.token != null ? this.token.Language : string.Empty;
            }
        }

        /// <summary>
        /// The kind of token, given its position in the syntax. This differs from <see
        /// cref="RawKind"/> when a contextual keyword is used in a place in the syntax that gives it
        /// its keyword meaning.
        /// </summary>
        /// <remarks>
        /// The ContextualKind is relevant only on contextual keyword tokens. ContextualKind differs
        /// from Kind when a token is used in context where the token should be interpreted as a
        /// keyword.
        /// </remarks>
        internal int RawContextualKind
        {
            get
            {
                return this.token != null ? this.token.RawContextualKind : 0;
            }
        }

        /// <summary>
        /// The node that contains this token in its Children collection.
        /// </summary>
        public SyntaxNode Parent
        {
            get
            {
                return this.parent;
            }
        }

        internal GreenNode Node
        {
            get
            {
                return this.token;
            }
        }

        internal int Index
        {
            get { return this.index; }
        }

        internal int Position
        {
            get { return this.position; }
        }

        /// <summary>
        /// The width of the token in characters, not including its leading and trailing trivia.
        /// </summary>
        internal int Width
        {
            get { return token != null ? token.Width : 0; }
        }

        /// <summary>
        /// The complete width of the token in characters including its leading and trailing trivia.
        /// </summary>
        internal int FullWidth
        {
            get { return token != null ? token.FullWidth : 0; }
        }

        /// <summary>
        /// The absolute span of this token in characters, not including its leading and trailing trivia.
        /// </summary>
        public TextSpan Span
        {
            get { return this.token != null ? new TextSpan(position + this.token.GetLeadingTriviaWidth(), this.token.Width) : default(TextSpan); }
        }

        internal int EndPosition
        {
            get { return this.token != null ? this.position + this.token.FullWidth : 0; }
        }

        /// <summary>
        /// Same as accessing <see cref="TextSpan.Start"/> on <see cref="Span"/>.
        /// </summary>
        /// <remarks>
        /// Slight performance improvement.
        /// </remarks>
        public int SpanStart
        {
            get { return this.token != null ? position + this.token.GetLeadingTriviaWidth() : 0; }
        }

        /// <summary>
        /// The absolute span of this token in characters, including its leading and trailing trivia.
        /// </summary>
        public TextSpan FullSpan
        {
            get { return new TextSpan(position, FullWidth); }
        }

        /// <summary>
        /// Determines whether this token represents a language construct that was actually parsed from source code.
        /// Missing tokens are typically generated by the parser in error scenarios to represent constructs that should
        /// have been present in the source code for the source code to compile successfully but were actually missing.
        /// </summary>
        public bool IsMissing
        {
            get { return token != null && token.IsMissing; }
        }

        /// <summary>
        /// Returns the value of the token. For example, if the token represents an integer literal, then this property
        /// would return the actual integer.
        /// </summary>
        public object Value
        {
            get { return token != null ? token.GetValue() : null; }
        }

        /// <summary>
        /// Returns the text representation of the value of the token. For example, if the token represents an integer
        /// literal, then this property would return a string representing the integer.
        /// </summary>
        public string ValueText
        {
            get { return token != null ? token.GetValueText() : null; }
        }

        public string Text
        {
            get { return token != null ? token.ToString() : string.Empty; }
        }

        /// <summary>
        /// Returns the string representation of this token, not including its leading and trailing trivia.
        /// </summary>
        /// <returns>The string representation of this token, not including its leading and trailing trivia.</returns>
        /// <remarks>The length of the returned string is always the same as Span.Length</remarks>
        public override string ToString()
        {
            return token != null ? token.ToString() : string.Empty;
        }

        /// <summary>
        /// Returns the full string representation of this token including its leading and trailing trivia.
        /// </summary>
        /// <returns>The full string representation of this token including its leading and trailing trivia.</returns>
        /// <remarks>The length of the returned string is always the same as FullSpan.Length</remarks>
        public string ToFullString()
        {
            return token != null ? token.ToFullString() : string.Empty;
        }

        /// <summary>
        /// Writes the full text of this token to the specified TextWriter
        /// </summary>
        /// <param name="writer"></param>
        public void WriteTo(System.IO.TextWriter writer)
        {
            if (token != null)
            {
                this.token.WriteTo(writer);
            }
        }

        /// <summary>
        /// Writes the text of this token to the specified TextWriter, optionally including trivia.
        /// </summary>
        internal void WriteTo(System.IO.TextWriter writer, bool leading, bool trailing)
        {
            if (this.token != null)
            {
                this.token.WriteTo(writer, leading, trailing);
            }
        }

        /// <summary>
        /// Determines whether this token has any leading trivia.
        /// </summary>
        public bool HasLeadingTrivia
        {
            get { return token != null && token.GetLeadingTriviaCore() != null; }
        }

        /// <summary>
        /// Determines whether this token has any trailing trivia.
        /// </summary>
        public bool HasTrailingTrivia
        {
            get { return token != null && token.GetTrailingTriviaCore() != null; }
        }

        /// <summary>
        /// Full width of the leading trivia of this token.
        /// </summary>
        internal int LeadingWidth
        {
            get { return token != null ? token.GetLeadingTriviaWidth() : 0; }
        }

        /// <summary>
        /// Full width of the trailing trivia of this token.
        /// </summary>
        internal int TrailingWidth
        {
            get { return token != null ? token.GetTrailingTriviaWidth() : 0; }
        }

        /// <summary>
        /// Determines whether this token or any of its descendant trivia have any diagnostics on them. 
        /// </summary>>
        public bool ContainsDiagnostics
        {
            get { return token != null && token.ContainsDiagnostics; }
        }

        /// <summary>
        /// Determines whether this token has any descendant preprocessor directives.
        /// </summary>
        public bool ContainsDirectives
        {
            get { return token != null && token.ContainsDirectives; }
        }

        /// <summary>
        /// Determines whether this token is a descendant of a structured trivia.
        /// </summary>
        public bool IsPartOfStructuredTrivia()
        {
            return this.parent != null && this.parent.IsPartOfStructuredTrivia();
        }

        /// <summary>
        /// Determines whether any of this token's trivia is structured.
        /// </summary>
        public bool HasStructuredTrivia
        {
            get { return token != null && token.ContainsStructuredTrivia; }
        }

        #region Annotations 
        /// <summary>
        /// True if this token or its trivia has any annotations.
        /// </summary>
        public bool ContainsAnnotations
        {
            get { return token != null && token.ContainsAnnotations; }
        }

        /// <summary>
        /// True if this token has annotations of the specified annotation kind.
        /// </summary>
        public bool HasAnnotations(string annotationKind)
        {
            return token != null && token.HasAnnotations(annotationKind);
        }

        /// <summary>
        /// True if this token has annotations of the specified annotation kinds.
        /// </summary>
        public bool HasAnnotations(params string[] annotationKinds)
        {
            return token != null && token.HasAnnotations(annotationKinds);
        }

        /// <summary>
        /// True if this token has the specified annotation.
        /// </summary>
        public bool HasAnnotation(SyntaxAnnotation annotation)
        {
            return token != null && token.HasAnnotation(annotation);
        }

        /// <summary>
        /// Gets all the annotations of the specified annotation kind.
        /// </summary>
        public IEnumerable<SyntaxAnnotation> GetAnnotations(string annotationKind)
        {
            return token != null
                ? token.GetAnnotations(annotationKind)
                : SpecializedCollections.EmptyEnumerable<SyntaxAnnotation>();
        }

        /// <summary>
        /// Gets all the annotations of the specified annotation kind.
        /// </summary>
        public IEnumerable<SyntaxAnnotation> GetAnnotations(params string[] annotationKinds)
        {
            return GetAnnotations((IEnumerable<string>)annotationKinds);
        }

        /// <summary>
        /// Gets all the annotations of the specified annotation kind.
        /// </summary>
        public IEnumerable<SyntaxAnnotation> GetAnnotations(IEnumerable<string> annotationKinds)
        {
            return token != null
                ? token.GetAnnotations(annotationKinds)
                : SpecializedCollections.EmptyEnumerable<SyntaxAnnotation>();
        }

        /// <summary>
        /// Adds this annotation to a given syntax token, creating a new syntax token of the same type with the
        /// annotation on it.
        /// </summary>
        public SyntaxToken WithAdditionalAnnotations(params SyntaxAnnotation[] annotations)
        {
            return WithAdditionalAnnotations((IEnumerable<SyntaxAnnotation>)annotations);
        }

        /// <summary>
        /// Adds this annotation to a given syntax token, creating a new syntax token of the same type with the
        /// annotation on it.
        /// </summary>
        public SyntaxToken WithAdditionalAnnotations(IEnumerable<SyntaxAnnotation> annotations)
        {
            if (annotations == null)
            {
                throw new ArgumentNullException("annotations");
            }

            if (this.Node != null)
            {
                return new SyntaxToken(
                    parent: null,
                    token: this.token.WithAdditionalAnnotationsGreen(annotations),
                    position: 0, index: 0);
            }

            return default(SyntaxToken);
        }

        /// <summary>
        /// Creates a new syntax token identical to this one without the specified annotations.
        /// </summary>
        public SyntaxToken WithoutAnnotations(params SyntaxAnnotation[] annotations)
        {
            return WithoutAnnotations((IEnumerable<SyntaxAnnotation>)annotations);
        }

        /// <summary>
        /// Creates a new syntax token identical to this one without the specified annotations.
        /// </summary>
        public SyntaxToken WithoutAnnotations(IEnumerable<SyntaxAnnotation> annotations)
        {
            if (annotations == null)
            {
                throw new ArgumentNullException("annotations");
            }

            if (this.Node != null)
            {
                return new SyntaxToken(
                    parent: null,
                    token: this.token.WithoutAnnotationsGreen(annotations),
                    position: 0, index: 0
                    );
            }

            return default(SyntaxToken);
        }

        /// <summary>
        /// Creates a new syntax token identical to this one without annotations of the specified kind.
        /// </summary>
        public SyntaxToken WithoutAnnotations(string annotationKind)
        {
            if (annotationKind == null)
            {
                throw new ArgumentNullException("annotationKind");
            }

            if (this.HasAnnotations(annotationKind))
            {
                return this.WithoutAnnotations(this.GetAnnotations(annotationKind));
            }
            else
            {
                return this;
            }
        }

        /// <summary>
        /// Copies all SyntaxAnnotations, if any, from this SyntaxToken instance and attaches them to a new instance based on <paramref name="token" />.
        /// </summary>
        /// <remarks>
        /// If no annotations are copied, just returns <paramref name="token" />.
        /// </remarks>
        public SyntaxToken CopyAnnotationsTo(SyntaxToken token)
        {
            if (token.Node == null)
            {
                return default(SyntaxToken);
            }

            if (this.token == null)
            {
                return token;
            }

            var annotations = this.Node.GetAnnotations();
            if (annotations == null || annotations.Length == 0)
            {
                return token;
            }

            return new SyntaxToken(
                parent: null,
                token: token.Node.WithAdditionalAnnotationsGreen(annotations),
                position: 0,
                index: 0);
        }
        #endregion

        /// <summary>
        /// The list of trivia that appear before this token in the source code.
        /// </summary>
        public SyntaxTriviaList LeadingTrivia
        {
            get
            {
                return this.token != null
                    ? new SyntaxTriviaList(this, this.token.GetLeadingTriviaCore(), this.Position)
                    : default(SyntaxTriviaList);
            }
        }

        /// <summary>
        /// The list of trivia that appear after this token in the source code and are attached to this token or any of
        /// its descendants.
        /// </summary>
        public SyntaxTriviaList TrailingTrivia
        {
            get
            {
                if (this.token != null)
                {
                    var leading = this.token.GetLeadingTriviaCore();
                    int index = 0;
                    if (leading != null)
                    {
                        index = leading.IsList ? leading.SlotCount : 1;
                    }

                    var trailingGreen = this.token.GetTrailingTriviaCore();
                    int trailingPosition = this.position + this.FullWidth;
                    if (trailingGreen != null)
                    {
                        trailingPosition -= trailingGreen.FullWidth;
                    }

                    return new SyntaxTriviaList(this,
                        trailingGreen,
                        trailingPosition,
                        index);
                }

                return default(SyntaxTriviaList);
            }
        }

        /// <summary>
        /// Clones the token and adds the <paramref name="trivia"/> as leading trivia.
        /// </summary>
        /// <param name="trivia">The <see cref="SyntaxTriviaList"/> to add as leading trivia.</param>
        /// <returns>A new token with leading trivia.</returns>
        public SyntaxToken WithLeadingTrivia(SyntaxTriviaList trivia)
        {
            return this.WithLeadingTrivia((IEnumerable<SyntaxTrivia>)trivia);
        }

        /// <summary>
        /// Clones the token and adds the <paramref name="trivia"/> as trailing trivia.
        /// </summary>
        /// <param name="trivia">The <see cref="SyntaxTriviaList"/> to add as trailing trivia.</param>
        /// <returns>A new token with trailing trivia.</returns>
        public SyntaxToken WithTrailingTrivia(SyntaxTriviaList trivia)
        {
            return this.WithTrailingTrivia((IEnumerable<SyntaxTrivia>)trivia);
        }

        public SyntaxToken WithLeadingTrivia(params SyntaxTrivia[] trivia)
        {
            return this.WithLeadingTrivia((IEnumerable<SyntaxTrivia>)trivia);
        }

        public SyntaxToken WithLeadingTrivia(IEnumerable<SyntaxTrivia> trivia)
        {
            var greenList = trivia == null ? null : trivia.Select(t => t.UnderlyingNode);

            return this.token != null
                ? new SyntaxToken(null, this.token.WithLeadingTrivia(this.token.CreateList(greenList)), position: 0, index: 0)
                : default(SyntaxToken);
        }

        public SyntaxToken WithTrailingTrivia(params SyntaxTrivia[] trivia)
        {
            return this.WithTrailingTrivia((IEnumerable<SyntaxTrivia>)trivia);
        }

        public SyntaxToken WithTrailingTrivia(IEnumerable<SyntaxTrivia> trivia)
        {
            var greenList = trivia == null ? null : trivia.Select(t => t.UnderlyingNode);

            return this.token != null
                ? new SyntaxToken(null, this.token.WithTrailingTrivia(this.token.CreateList(greenList)), position: 0, index: 0)
                : default(SyntaxToken);
        }

        /// <summary>
        /// Gets a list of all the trivia (both leading and trailing) for this token.
        /// </summary>
        public IEnumerable<SyntaxTrivia> GetAllTrivia()
        {
            if (this.HasLeadingTrivia)
            {
                if (this.HasTrailingTrivia)
                {
                    return this.LeadingTrivia.Concat(this.TrailingTrivia);
                }

                return this.LeadingTrivia;
            }
            else if (this.HasTrailingTrivia)
            {
                return this.TrailingTrivia;
            }
            else
            {
                return SpecializedCollections.EmptyEnumerable<SyntaxTrivia>();
            }
        }

        /// <summary>
        /// Determines whether two <see cref="T:Microsoft.CodeAnalysis.SyntaxToken"/>s are equal.
        /// </summary>
        public static bool operator ==(SyntaxToken left, SyntaxToken right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two <see cref="T:Microsoft.CodeAnalysis.SyntaxToken"/>s are unequal.
        /// </summary>
        public static bool operator !=(SyntaxToken left, SyntaxToken right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Determines whether the supplied <see cref="T:Microsoft.CodeAnalysis.SyntaxToken"/> is equal to this
        /// <see cref="T:Microsoft.CodeAnalysis.SyntaxToken"/>.
        /// </summary>
        public bool Equals(SyntaxToken other)
        {
            return this.parent == other.parent &&
                   this.token == other.token &&
                   this.position == other.position &&
                   this.index == other.index;
        }

        /// <summary>
        /// Determines whether the supplied <see cref="T:Microsoft.CodeAnalysis.SyntaxToken"/> is equal to this
        /// <see cref="T:Microsoft.CodeAnalysis.SyntaxToken"/>.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is SyntaxToken && Equals((SyntaxToken)obj);
        }

        /// <summary>
        /// Serves as hash function for <see cref="T:Microsoft.CodeAnalysis.SyntaxToken"/>.
        /// </summary>
        public override int GetHashCode()
        {
            return Hash.Combine(this.parent, Hash.Combine(this.token, Hash.Combine(this.position, this.index)));
        }

        /// <summary>
        /// Gets the token that follows this token in the syntax tree.
        /// </summary>
        /// <returns>The token that follows this token in the syntax tree.</returns>
        public SyntaxToken GetNextToken(bool includeZeroWidth = false, bool includeSkipped = false, bool includeDirectives = false, bool includeDocumentationComments = false)
        {
            if (this.token == null)
            {
                return default(SyntaxToken);
            }

            return this.token.Navigator.GetNextToken(this, includeZeroWidth, includeSkipped, includeDirectives, includeDocumentationComments);
        }

        /// <summary>
        /// Returns the token after this token in the syntax tree.
        /// </summary>
        /// <param name="predicate">Delegate applied to each token.  The token is returned if the predicate returns
        /// true.</param>
        /// <param name="stepInto">Delegate applied to trivia.  If this delegate is present then trailing trivia is
        /// included in the search.</param>
        internal SyntaxToken GetNextToken(Func<SyntaxToken, bool> predicate, Func<SyntaxTrivia, bool> stepInto = null)
        {
            if (this.token == null)
            {
                return default(SyntaxToken);
            }

            return (SyntaxToken)this.token.Navigator.GetNextToken(this, predicate, stepInto);
        }

        /// <summary>
        /// Gets the token that precedes this token in the syntax tree.
        /// </summary>
        /// <returns>The next token that follows this token in the syntax tree.</returns>
        public SyntaxToken GetPreviousToken(bool includeZeroWidth = false, bool includeSkipped = false, bool includeDirectives = false, bool includeDocumentationComments = false)
        {
            if (this.token == null)
            {
                return default(SyntaxToken);
            }

            return this.token.Navigator.GetPreviousToken(this, includeZeroWidth, includeSkipped, includeDirectives, includeDocumentationComments);
        }

        /// <summary>
        /// Returns the token before this token in the syntax tree.
        /// </summary>
        /// <param name="predicate">Delegate applied to each token.  The token is returned if the predicate returns
        /// true.</param>
        /// <param name="stepInto">Delegate applied to trivia.  If this delegate is present then trailing trivia is
        /// included in the search.</param>
        internal SyntaxToken GetPreviousToken(Func<SyntaxToken, bool> predicate, Func<SyntaxTrivia, bool> stepInto = null)
        {
            return (SyntaxToken)this.token.Navigator.GetPreviousToken(this, predicate, stepInto);
        }

        /// <summary>
        /// SyntaxTree which contains current SyntaxToken.
        /// </summary>
        public SyntaxTree SyntaxTree
        {
            get
            {
                var parent = this.parent;
                return parent == null ? null : parent.SyntaxTree;
            }
        }

        /// <summary>
        /// Gets the location for this token.
        /// </summary>
        public Location GetLocation()
        {
            return this.token != null
                ? this.SyntaxTree.GetLocation(this.Span)
                : Location.None;
        }

        /// <summary>
        /// Gets a list of all the diagnostics associated with this token and any related trivia.
        /// This method does not filter diagnostics based on #pragmas and compiler options
        /// like nowarn, warnaserror etc.
        /// </summary>
        public IEnumerable<Diagnostic> GetDiagnostics()
        {
            return this.token != null
                ? this.SyntaxTree.GetDiagnostics(this)
                : SpecializedCollections.EmptyEnumerable<Diagnostic>();
        }

        /// <summary>
        /// Determines if this token is equivalent to the specified token.
        /// </summary>
        public bool IsEquivalentTo(SyntaxToken token)
        {
            return
                (this.token == null && token.Node == null) ||
                (this.token != null && token.Node != null && this.token.IsEquivalentTo(token.Node));
        }
    }
}