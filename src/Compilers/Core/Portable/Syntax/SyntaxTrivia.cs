// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
#pragma warning disable CA1200 // Avoid using cref tags with a prefix
    /// <summary>
    /// Represents a trivia in the syntax tree.
    /// </summary>
#pragma warning restore CA1200 // Avoid using cref tags with a prefix
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    [StructLayout(LayoutKind.Auto)]
    public readonly struct SyntaxTrivia : IEquatable<SyntaxTrivia>
    {
        internal static readonly Func<SyntaxTrivia, bool> Any = t => true;

        internal SyntaxTrivia(in SyntaxToken token, GreenNode? triviaNode, int position, int index)
        {
            Token = token;
            UnderlyingNode = triviaNode;
            Position = position;
            Index = index;

            Debug.Assert(this.RawKind != 0 || this.Equals(default(SyntaxTrivia)));
        }

        /// <summary>
        /// An integer representing the language specific kind of this trivia.
        /// </summary>
        public int RawKind => UnderlyingNode?.RawKind ?? 0;

        private string GetDebuggerDisplay()
        {
            return GetType().Name + " " + (UnderlyingNode?.KindText ?? "None") + " " + ToString();
        }

        /// <summary>
        /// The language name that this trivia is syntax of.
        /// </summary>
        public string Language => UnderlyingNode?.Language ?? string.Empty;

        /// <summary>
        /// The parent token that contains this token in its LeadingTrivia or TrailingTrivia collection.
        /// </summary>
        public SyntaxToken Token { get; }

        internal GreenNode? UnderlyingNode { get; }

        internal GreenNode RequiredUnderlyingNode
        {
            get
            {
                var node = UnderlyingNode;
                Debug.Assert(node is object);
                return node;
            }
        }

        internal int Position { get; }

        internal int Index { get; }

        /// <summary>
        /// The width of this trivia in characters. If this trivia is a structured trivia then the returned width will
        /// not include the widths of any leading or trailing trivia present on the child non-terminal node of this
        /// trivia.
        /// </summary>
        internal int Width => UnderlyingNode?.Width ?? 0;

        /// <summary>
        /// The width of this trivia in characters. If this trivia is a structured trivia then the returned width will
        /// include the widths of any leading or trailing trivia present on the child non-terminal node of this trivia.
        /// </summary>
        internal int FullWidth => UnderlyingNode?.FullWidth ?? 0;

        /// <summary>
        /// The absolute span of this trivia in characters. If this trivia is a structured trivia then the returned span
        /// will not include spans of any leading or trailing trivia present on the child non-terminal node of this
        /// trivia.
        /// </summary>
        public TextSpan Span
        {
            get
            {
                return UnderlyingNode != null
                    ? new TextSpan(Position + UnderlyingNode.GetLeadingTriviaWidth(), UnderlyingNode.Width)
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
                return UnderlyingNode != null
                    ? Position + UnderlyingNode.GetLeadingTriviaWidth()
                    : 0; // default(TextSpan).Start
            }
        }

        /// <summary>
        /// The absolute span of this trivia in characters. If this trivia is a structured trivia then the returned span
        /// will include spans of any leading or trailing trivia present on the child non-terminal node of this trivia.
        /// </summary>
        public TextSpan FullSpan
        {
            get { return UnderlyingNode != null ? new TextSpan(Position, UnderlyingNode.FullWidth) : default(TextSpan); }
        }

        /// <summary>
        /// Determines whether this trivia has any diagnostics on it. If this trivia is a structured trivia then the
        /// returned value will indicate whether this trivia or any of its descendant nodes, tokens or trivia have any
        /// diagnostics on them.
        /// </summary>>
        public bool ContainsDiagnostics => UnderlyingNode?.ContainsDiagnostics ?? false;

        /// <summary>
        /// Determines whether this trivia is a structured trivia.
        /// </summary>
        public bool HasStructure => UnderlyingNode?.IsStructuredTrivia ?? false;

        /// <summary>
        /// Determines whether this trivia is a descendant of a structured trivia.
        /// </summary>
        public bool IsPartOfStructuredTrivia()
        {
            return Token.Parent?.IsPartOfStructuredTrivia() ?? false;
        }

        /// <summary>
        /// Determines whether this trivia or any of its structure has annotations.
        /// </summary>
        internal bool ContainsAnnotations => UnderlyingNode?.ContainsAnnotations ?? false;

        /// <summary>
        /// Determines where this trivia has annotations of the specified annotation kind.
        /// </summary>
        public bool HasAnnotations(string annotationKind)
        {
            return UnderlyingNode?.HasAnnotations(annotationKind) ?? false;
        }

        /// <summary>
        /// Determines where this trivia has any annotations of the specified annotation kinds.
        /// </summary>
        public bool HasAnnotations(params string[] annotationKinds)
        {
            return UnderlyingNode?.HasAnnotations(annotationKinds) ?? false;
        }

        /// <summary>
        /// Determines whether this trivia has the specific annotation.
        /// </summary>
        public bool HasAnnotation([NotNullWhen(true)] SyntaxAnnotation? annotation)
        {
            return UnderlyingNode?.HasAnnotation(annotation) ?? false;
        }

        /// <summary>
        /// Get all the annotations of the specified annotation kind.
        /// </summary>
        public IEnumerable<SyntaxAnnotation> GetAnnotations(string annotationKind)
        {
            return UnderlyingNode != null
                ? UnderlyingNode.GetAnnotations(annotationKind)
                : SpecializedCollections.EmptyEnumerable<SyntaxAnnotation>();
        }

        /// <summary>
        /// Get all the annotations of the specified annotation kinds.
        /// </summary>
        public IEnumerable<SyntaxAnnotation> GetAnnotations(params string[] annotationKinds)
        {
            return UnderlyingNode != null
                ? UnderlyingNode.GetAnnotations(annotationKinds)
                : SpecializedCollections.EmptyEnumerable<SyntaxAnnotation>();
        }

        /// <summary>
        /// Determines whether this trivia represents a preprocessor directive.
        /// </summary>
        public bool IsDirective => UnderlyingNode?.IsDirective ?? false;

        internal bool IsSkippedTokensTrivia => UnderlyingNode?.IsSkippedTokensTrivia ?? false;
        internal bool IsDocumentationCommentTrivia => UnderlyingNode?.IsDocumentationCommentTrivia ?? false;

        /// <summary>
        /// Returns the child non-terminal node representing the syntax tree structure under this structured trivia.
        /// </summary>
        /// <returns>The child non-terminal node representing the syntax tree structure under this structured
        /// trivia.</returns>
        public SyntaxNode? GetStructure()
        {
            return HasStructure ? UnderlyingNode!.GetStructure(this) : null;
        }

        internal bool TryGetStructure([NotNullWhen(true)] out SyntaxNode? structure)
        {
            structure = GetStructure();
            return structure is object;
        }

        /// <summary> 
        /// Returns the string representation of this trivia. If this trivia is structured trivia then the returned string
        /// will not include any leading or trailing trivia present on the StructuredTriviaSyntax node of this trivia.
        /// </summary>
        /// <returns>The string representation of this trivia.</returns>
        /// <remarks>The length of the returned string is always the same as Span.Length</remarks>
        public override string ToString()
        {
            return UnderlyingNode != null ? UnderlyingNode.ToString() : string.Empty;
        }

        /// <summary> 
        /// Returns the full string representation of this trivia. If this trivia is structured trivia then the returned string will
        /// include any leading or trailing trivia present on the StructuredTriviaSyntax node of this trivia.
        /// </summary>
        /// <returns>The full string representation of this trivia.</returns>
        /// <remarks>The length of the returned string is always the same as FullSpan.Length</remarks>
        public string ToFullString()
        {
            return UnderlyingNode != null ? UnderlyingNode.ToFullString() : string.Empty;
        }

        /// <summary>
        /// Writes the full text of this trivia to the specified TextWriter.
        /// </summary>
        public void WriteTo(System.IO.TextWriter writer)
        {
            UnderlyingNode?.WriteTo(writer);
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
            return Token == other.Token && UnderlyingNode == other.UnderlyingNode && Position == other.Position && Index == other.Index;
        }

        /// <summary>
        /// Determines whether the supplied <see cref="SyntaxTrivia"/> is equal to this
        /// <see cref="SyntaxTrivia"/>.
        /// </summary>
        public override bool Equals(object? obj)
        {
            return obj is SyntaxTrivia trivia && Equals(trivia);
        }

        /// <summary>
        /// Serves as hash function for <see cref="SyntaxTrivia"/>.
        /// </summary>
        public override int GetHashCode()
        {
            return Hash.Combine(Token.GetHashCode(), Hash.Combine(UnderlyingNode, Hash.Combine(Position, Index)));
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
                throw new ArgumentNullException(nameof(annotations));
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
                throw new ArgumentNullException(nameof(annotations));
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
                throw new ArgumentNullException(nameof(annotationKind));
            }

            if (this.HasAnnotations(annotationKind))
            {
                return this.WithoutAnnotations(this.GetAnnotations(annotationKind));
            }

            return this;
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
        public SyntaxTree? SyntaxTree
        {
            get
            {
                return Token.SyntaxTree;
            }
        }

        /// <summary>
        /// Get the location of this trivia.
        /// </summary>
        public Location GetLocation()
        {
            return this.SyntaxTree?.GetLocation(this.Span) ?? Location.None;
        }

        /// <summary>
        /// Gets a list of all the diagnostics associated with this trivia.
        /// This method does not filter diagnostics based on #pragmas and compiler options
        /// like nowarn, warnaserror etc.
        /// </summary>
        public IEnumerable<Diagnostic> GetDiagnostics()
        {
            if (UnderlyingNode is null)
            {
                return SpecializedCollections.EmptyEnumerable<Diagnostic>();
            }

            if (this.SyntaxTree is { } syntaxTree)
            {
                return syntaxTree.GetDiagnostics(this);
            }
            else
            {
                var diagnostics = UnderlyingNode.GetDiagnostics();

                return diagnostics.Length == 0
                    ? SpecializedCollections.EmptyEnumerable<Diagnostic>()
                    : diagnostics.Select(Diagnostic.Create);
            }
        }

        /// <summary>
        /// Determines if this trivia is equivalent to the specified trivia.
        /// </summary>
        public bool IsEquivalentTo(SyntaxTrivia trivia)
        {
            return
                (UnderlyingNode == null && trivia.UnderlyingNode == null) ||
                (UnderlyingNode != null && trivia.UnderlyingNode != null && UnderlyingNode.IsEquivalentTo(trivia.UnderlyingNode));
        }
    }
}
