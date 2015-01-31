// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
#pragma warning disable RS0010
    /// <summary>
    /// Represents a trivia in the syntax tree. This is the language agnostic equivalent of <see
    /// cref="T:Microsoft.CodeAnalysis.CSharp.SyntaxTrivia"/> and <see cref="T:Microsoft.CodeAnalysis.VisualBasic.SyntaxTrivia"/>.
    /// </summary>
#pragma warning restore RS0010
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    public struct SyntaxTrivia : IEquatable<SyntaxTrivia>
    {
        internal static readonly Func<SyntaxTrivia, bool> Any = t => true;

        private readonly SyntaxToken _token;
        private readonly GreenNode _triviaNode;
        private readonly int _position;
        private readonly int _index;

        internal SyntaxTrivia(SyntaxToken token, GreenNode triviaNode, int position, int index)
        {
            _token = token;
            _triviaNode = triviaNode;
            _position = position;
            _index = index;

            Debug.Assert(this.RawKind != 0 || this.Equals(default(SyntaxTrivia)));
        }

        /// <summary>
        /// An integer representing the language specific kind of this trivia.
        /// </summary>
        public int RawKind
        {
            get { return _triviaNode != null ? _triviaNode.RawKind : 0; }
        }

        private string GetDebuggerDisplay()
        {
            return GetType().Name + " " + (_triviaNode != null ? _triviaNode.KindText : "None") + " " + ToString();
        }

        /// <summary>
        /// The language name that this trivia is syntax of.
        /// </summary>
        public string Language
        {
            get
            {
                return _triviaNode != null ? _triviaNode.Language : string.Empty;
            }
        }

        /// <summary>
        /// The parent token that contains this token in its LeadingTrivia or TrailingTrivia collection.
        /// </summary>
        public SyntaxToken Token
        {
            get { return _token; }
        }

        internal GreenNode UnderlyingNode
        {
            get { return _triviaNode; }
        }

        internal int Position
        {
            get { return _position; }
        }

        internal int Index
        {
            get { return _index; }
        }

        /// <summary>
        /// The width of this trivia in characters. If this trivia is a structured trivia then the returned width will
        /// not include the widths of any leading or trailing trivia present on the child non-terminal node of this
        /// trivia.
        /// </summary>
        internal int Width
        {
            get { return _triviaNode != null ? _triviaNode.Width : 0; }
        }

        /// <summary>
        /// The width of this trivia in characters. If this trivia is a structured trivia then the returned width will
        /// include the widths of any leading or trailing trivia present on the child non-terminal node of this trivia.
        /// </summary>
        internal int FullWidth
        {
            get { return _triviaNode != null ? _triviaNode.FullWidth : 0; }
        }

        /// <summary>
        /// The absolute span of this trivia in characters. If this trivia is a structured trivia then the returned span
        /// will not include spans of any leading or trailing trivia present on the child non-terminal node of this
        /// trivia.
        /// </summary>
        public TextSpan Span
        {
            get
            {
                return _triviaNode != null
                    ? new TextSpan(_position + _triviaNode.GetLeadingTriviaWidth(), _triviaNode.Width)
                    : default(TextSpan);
            }
        }

        /// <summary>
        /// Same as accessing <see cref="TextSpan.Start"/> on <see cref="Span"/>.
        /// </summary>
        /// <remarks>
        /// Slight performance improvement.
        /// </remarks>
        public int SpanStart
        {
            get
            {
                return _triviaNode != null
                    ? _position + _triviaNode.GetLeadingTriviaWidth()
                    : 0; // default(TextSpan).Start
            }
        }

        /// <summary>
        /// The absolute span of this trivia in characters. If this trivia is a structured trivia then the returned span
        /// will include spans of any leading or trailing trivia present on the child non-terminal node of this trivia.
        /// </summary>
        public TextSpan FullSpan
        {
            get { return _triviaNode != null ? new TextSpan(_position, _triviaNode.FullWidth) : default(TextSpan); }
        }

        /// <summary>
        /// Determines whether this trivia has any diagnostics on it. If this trivia is a structured trivia then the
        /// returned value will indicate whether this trivia or any of its descendant nodes, tokens or trivia have any
        /// diagnostics on them.
        /// </summary>>
        public bool ContainsDiagnostics
        {
            get { return _triviaNode != null && _triviaNode.ContainsDiagnostics; }
        }

        /// <summary>
        /// Determines whether this trivia is a structured trivia.
        /// </summary>
        public bool HasStructure
        {
            get { return _triviaNode != null && _triviaNode.IsStructuredTrivia; }
        }

        /// <summary>
        /// Determines whether this trivia is a descendant of a structured trivia.
        /// </summary>
        public bool IsPartOfStructuredTrivia()
        {
            return _token.Parent != null && _token.Parent.IsPartOfStructuredTrivia();
        }

        /// <summary>
        /// Determines whether this trivia or any of its structure has annotations.
        /// </summary>
        internal bool ContainsAnnotations
        {
            get { return _triviaNode != null && _triviaNode.ContainsAnnotations; }
        }

        /// <summary>
        /// Determines where this trivia has annotations of the specified annotation kind.
        /// </summary>
        public bool HasAnnotations(string annotationKind)
        {
            return _triviaNode != null && _triviaNode.HasAnnotations(annotationKind);
        }

        /// <summary>
        /// Determines where this trivia has any annotations of the specified annotation kinds.
        /// </summary>
        public bool HasAnnotations(params string[] annotationKinds)
        {
            return _triviaNode != null && _triviaNode.HasAnnotations(annotationKinds);
        }

        /// <summary>
        /// Determines whether this trivia has the specific annotation.
        /// </summary>
        public bool HasAnnotation(SyntaxAnnotation annotation)
        {
            return _triviaNode != null && _triviaNode.HasAnnotation(annotation);
        }

        /// <summary>
        /// Get all the annotations of the specified annotation kind.
        /// </summary>
        public IEnumerable<SyntaxAnnotation> GetAnnotations(string annotationKind)
        {
            return _triviaNode != null
                ? _triviaNode.GetAnnotations(annotationKind)
                : SpecializedCollections.EmptyEnumerable<SyntaxAnnotation>();
        }

        /// <summary>
        /// Get all the annotations of the specified annotation kinds.
        /// </summary>
        public IEnumerable<SyntaxAnnotation> GetAnnotations(params string[] annotationKinds)
        {
            return _triviaNode != null
                ? _triviaNode.GetAnnotations(annotationKinds)
                : SpecializedCollections.EmptyEnumerable<SyntaxAnnotation>();
        }

        /// <summary>
        /// Determines whether this trivia represents a preprocessor directive.
        /// </summary>
        public bool IsDirective
        {
            get { return _triviaNode != null && _triviaNode.IsDirective; }
        }

        /// <summary>
        /// Returns the child non-terminal node representing the syntax tree structure under this structured trivia.
        /// </summary>
        /// <returns>The child non-terminal node representing the syntax tree structure under this structured
        /// trivia.</returns>
        public SyntaxNode GetStructure()
        {
            return HasStructure ? _triviaNode.GetStructure(this) : null;
        }

        /// <summary> 
        /// Returns the string representation of this trivia. If this trivia is structured trivia then the returned string
        /// will not include any leading or trailing trivia present on the StructuredTriviaSyntax node of this trivia.
        /// </summary>
        /// <returns>The string representation of this trivia.</returns>
        /// <remarks>The length of the returned string is always the same as Span.Length</remarks>
        public override string ToString()
        {
            return _triviaNode != null ? _triviaNode.ToString() : string.Empty;
        }

        /// <summary> 
        /// Returns the full string representation of this trivia. If this trivia is structured trivia then the returned string will
        /// include any leading or trailing trivia present on the StructuredTriviaSyntax node of this trivia.
        /// </summary>
        /// <returns>The full string representation of this trivia.</returns>
        /// <remarks>The length of the returned string is always the same as FullSpan.Length</remarks>
        public string ToFullString()
        {
            return _triviaNode != null ? _triviaNode.ToFullString() : string.Empty;
        }

        /// <summary>
        /// Writes the full text of this trivia to the specified TextWriter.
        /// </summary>
        public void WriteTo(System.IO.TextWriter writer)
        {
            if (_triviaNode != null)
            {
                _triviaNode.WriteTo(writer);
            }
        }

        /// <summary>
        /// Determines whether two <see cref="SyntaxTrivia"/>s are equal.
        /// </summary>
        public static bool operator ==(SyntaxTrivia left, SyntaxTrivia right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two <see cref="SyntaxTrivia"/>s are unequal.
        /// </summary>
        public static bool operator !=(SyntaxTrivia left, SyntaxTrivia right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Determines whether the supplied <see cref="SyntaxTrivia"/> is equal to this
        /// <see cref="SyntaxTrivia"/>.
        /// </summary>
        public bool Equals(SyntaxTrivia other)
        {
            return _token == other._token && _triviaNode == other._triviaNode && _position == other._position && _index == other._index;
        }

        /// <summary>
        /// Determines whether the supplied <see cref="SyntaxTrivia"/> is equal to this
        /// <see cref="SyntaxTrivia"/>.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is SyntaxTrivia && Equals((SyntaxTrivia)obj);
        }

        /// <summary>
        /// Serves as hash function for <see cref="SyntaxTrivia"/>.
        /// </summary>
        public override int GetHashCode()
        {
            return Hash.Combine(_token.GetHashCode(), Hash.Combine(_triviaNode, Hash.Combine(_position, _index)));
        }

        #region Annotations 
        /// <summary>
        /// Creates a new SyntaxTrivia with the specified annotations.
        /// </summary>
        public SyntaxTrivia WithAdditionalAnnotations(params SyntaxAnnotation[] annotations)
        {
            return WithAdditionalAnnotations((IEnumerable<SyntaxAnnotation>)annotations);
        }

        /// <summary>
        /// Creates a new SyntaxTrivia with the specified annotations.
        /// </summary>
        public SyntaxTrivia WithAdditionalAnnotations(IEnumerable<SyntaxAnnotation> annotations)
        {
            if (annotations == null)
            {
                throw new ArgumentNullException("annotations");
            }

            if (this.UnderlyingNode != null)
            {
                return new SyntaxTrivia(
                    token: default(SyntaxToken),
                    triviaNode: this.UnderlyingNode.WithAdditionalAnnotationsGreen(annotations),
                    position: 0, index: 0);
            }

            return default(SyntaxTrivia);
        }

        /// <summary>
        /// Creates a new SyntaxTrivia without the specified annotations.
        /// </summary>
        public SyntaxTrivia WithoutAnnotations(params SyntaxAnnotation[] annotations)
        {
            return WithoutAnnotations((IEnumerable<SyntaxAnnotation>)annotations);
        }

        /// <summary>
        /// Creates a new SyntaxTrivia without the specified annotations.
        /// </summary>
        public SyntaxTrivia WithoutAnnotations(IEnumerable<SyntaxAnnotation> annotations)
        {
            if (annotations == null)
            {
                throw new ArgumentNullException("annotations");
            }

            if (this.UnderlyingNode != null)
            {
                return new SyntaxTrivia(
                    token: default(SyntaxToken),
                    triviaNode: this.UnderlyingNode.WithoutAnnotationsGreen(annotations),
                    position: 0, index: 0);
            }

            return default(SyntaxTrivia);
        }

        /// <summary>
        /// Creates a new SyntaxTrivia without annotations of the specified kind.
        /// </summary>
        public SyntaxTrivia WithoutAnnotations(string annotationKind)
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
        /// Copies all SyntaxAnnotations, if any, from this SyntaxTrivia instance and attaches them to a new instance based on <paramref name="trivia" />.
        /// </summary>
        public SyntaxTrivia CopyAnnotationsTo(SyntaxTrivia trivia)
        {
            if (trivia.UnderlyingNode == null)
            {
                return default(SyntaxTrivia);
            }

            if (this.UnderlyingNode == null)
            {
                return trivia;
            }

            var annotations = this.UnderlyingNode.GetAnnotations();
            if (annotations == null || annotations.Length == 0)
            {
                return trivia;
            }

            return new SyntaxTrivia(
                token: default(SyntaxToken),
                triviaNode: trivia.UnderlyingNode.WithAdditionalAnnotationsGreen(annotations),
                position: 0, index: 0);
        }
        #endregion

        /// <summary>
        /// SyntaxTree which contains current SyntaxTrivia.
        /// </summary>
        public SyntaxTree SyntaxTree
        {
            get
            {
                return _token.SyntaxTree;
            }
        }

        /// <summary>
        /// Get the location of this trivia.
        /// </summary>
        public Location GetLocation()
        {
            return this.SyntaxTree.GetLocation(this.Span);
        }

        /// <summary>
        /// Gets a list of all the diagnostics associated with this trivia.
        /// This method does not filter diagnostics based on #pragmas and compiler options
        /// like nowarn, warnaserror etc.
        /// </summary>
        public IEnumerable<Diagnostic> GetDiagnostics()
        {
            return this.SyntaxTree.GetDiagnostics(this);
        }

        /// <summary>
        /// Determines if this trivia is equivalent to the specified trivia.
        /// </summary>
        public bool IsEquivalentTo(SyntaxTrivia trivia)
        {
            return
                (_triviaNode == null && trivia.UnderlyingNode == null) ||
                (_triviaNode != null && trivia.UnderlyingNode != null && _triviaNode.IsEquivalentTo(trivia.UnderlyingNode));
        }
    }
}
