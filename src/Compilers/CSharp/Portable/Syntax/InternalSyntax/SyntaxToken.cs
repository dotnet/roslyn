// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    using Microsoft.CodeAnalysis.Syntax.InternalSyntax;

    internal partial class SyntaxToken : CSharpSyntaxNode
    {
        //====================
        // Optimization: Normally, we wouldn't accept this much duplicate code, but these constructors
        // are called A LOT and we want to keep them as short and simple as possible and increase the
        // likelihood that they will be inlined.

        internal SyntaxToken(SyntaxKind kind)
            : base(kind)
        {
            FullWidth = this.Text.Length;
            this.flags |= NodeFlags.IsNotMissing; //note: cleared by subclasses representing missing tokens
        }

        internal SyntaxToken(SyntaxKind kind, DiagnosticInfo[] diagnostics)
            : base(kind, diagnostics)
        {
            FullWidth = this.Text.Length;
            this.flags |= NodeFlags.IsNotMissing; //note: cleared by subclasses representing missing tokens
        }

        internal SyntaxToken(SyntaxKind kind, DiagnosticInfo[] diagnostics, SyntaxAnnotation[] annotations)
            : base(kind, diagnostics, annotations)
        {
            FullWidth = this.Text.Length;
            this.flags |= NodeFlags.IsNotMissing; //note: cleared by subclasses representing missing tokens
        }

        internal SyntaxToken(SyntaxKind kind, int fullWidth)
            : base(kind, fullWidth)
        {
            this.flags |= NodeFlags.IsNotMissing; //note: cleared by subclasses representing missing tokens
        }

        internal SyntaxToken(SyntaxKind kind, int fullWidth, DiagnosticInfo[] diagnostics)
            : base(kind, diagnostics, fullWidth)
        {
            this.flags |= NodeFlags.IsNotMissing; //note: cleared by subclasses representing missing tokens
        }

        internal SyntaxToken(SyntaxKind kind, int fullWidth, DiagnosticInfo[] diagnostics, SyntaxAnnotation[] annotations)
            : base(kind, diagnostics, annotations, fullWidth)
        {
            this.flags |= NodeFlags.IsNotMissing; //note: cleared by subclasses representing missing tokens
        }

        //====================

        public override bool IsToken => true;

        internal override GreenNode GetSlot(int index)
        {
            throw ExceptionUtilities.Unreachable();
        }

        internal static SyntaxToken Create(SyntaxKind kind)
        {
            if (kind > LastTokenWithWellKnownText)
            {
                if (!SyntaxFacts.IsAnyToken(kind))
                {
                    throw new ArgumentException(string.Format(CSharpResources.ThisMethodCanOnlyBeUsedToCreateTokens, kind), nameof(kind));
                }

                return CreateMissing(kind, null, null);
            }

            return s_tokensWithNoTrivia[(int)kind].Value;
        }

        internal static SyntaxToken Create(SyntaxKind kind, GreenNode leading, GreenNode trailing)
        {
            if (kind > LastTokenWithWellKnownText)
            {
                if (!SyntaxFacts.IsAnyToken(kind))
                {
                    throw new ArgumentException(string.Format(CSharpResources.ThisMethodCanOnlyBeUsedToCreateTokens, kind), nameof(kind));
                }

                return CreateMissing(kind, leading, trailing);
            }

            if (leading == null)
            {
                if (trailing == null)
                {
                    return s_tokensWithNoTrivia[(int)kind].Value;
                }
                else if (trailing == SyntaxFactory.Space)
                {
                    return s_tokensWithSingleTrailingSpace[(int)kind].Value;
                }
                else if (trailing == SyntaxFactory.CarriageReturnLineFeed)
                {
                    return s_tokensWithSingleTrailingCRLF[(int)kind].Value;
                }
            }

            if (leading == SyntaxFactory.ElasticZeroSpace && trailing == SyntaxFactory.ElasticZeroSpace)
            {
                return s_tokensWithElasticTrivia[(int)kind].Value;
            }

            return new SyntaxTokenWithTrivia(kind, leading, trailing);
        }

        internal static SyntaxToken CreateMissing(SyntaxKind kind, GreenNode leading, GreenNode trailing)
        {
            return new MissingTokenWithTrivia(kind, leading, trailing);
        }

        internal const SyntaxKind FirstTokenWithWellKnownText = SyntaxKind.TildeToken;
        internal const SyntaxKind LastTokenWithWellKnownText = SyntaxKind.EndOfFileToken;

        // TODO: eliminate the blank space before the first interesting element?
        private static readonly ArrayElement<SyntaxToken>[] s_tokensWithNoTrivia = new ArrayElement<SyntaxToken>[(int)LastTokenWithWellKnownText + 1];
        private static readonly ArrayElement<SyntaxToken>[] s_tokensWithElasticTrivia = new ArrayElement<SyntaxToken>[(int)LastTokenWithWellKnownText + 1];
        private static readonly ArrayElement<SyntaxToken>[] s_tokensWithSingleTrailingSpace = new ArrayElement<SyntaxToken>[(int)LastTokenWithWellKnownText + 1];
        private static readonly ArrayElement<SyntaxToken>[] s_tokensWithSingleTrailingCRLF = new ArrayElement<SyntaxToken>[(int)LastTokenWithWellKnownText + 1];

        static SyntaxToken()
        {
            for (var kind = FirstTokenWithWellKnownText; kind <= LastTokenWithWellKnownText; kind++)
            {
                s_tokensWithNoTrivia[(int)kind].Value = new SyntaxToken(kind);
                s_tokensWithElasticTrivia[(int)kind].Value = new SyntaxTokenWithTrivia(kind, SyntaxFactory.ElasticZeroSpace, SyntaxFactory.ElasticZeroSpace);
                s_tokensWithSingleTrailingSpace[(int)kind].Value = new SyntaxTokenWithTrivia(kind, null, SyntaxFactory.Space);
                s_tokensWithSingleTrailingCRLF[(int)kind].Value = new SyntaxTokenWithTrivia(kind, null, SyntaxFactory.CarriageReturnLineFeed);
            }
        }

        internal static IEnumerable<SyntaxToken> GetWellKnownTokens()
        {
            foreach (var element in s_tokensWithNoTrivia)
            {
                if (element.Value != null)
                {
                    yield return element.Value;
                }
            }

            foreach (var element in s_tokensWithElasticTrivia)
            {
                if (element.Value != null)
                {
                    yield return element.Value;
                }
            }

            foreach (var element in s_tokensWithSingleTrailingSpace)
            {
                if (element.Value != null)
                {
                    yield return element.Value;
                }
            }

            foreach (var element in s_tokensWithSingleTrailingCRLF)
            {
                if (element.Value != null)
                {
                    yield return element.Value;
                }
            }
        }

        internal static SyntaxToken Identifier(string text)
        {
            return new SyntaxIdentifier(text);
        }

        internal static SyntaxToken Identifier(GreenNode leading, string text, GreenNode trailing)
        {
            if (leading == null)
            {
                if (trailing == null)
                {
                    return Identifier(text);
                }
                else
                {
                    return new SyntaxIdentifierWithTrailingTrivia(text, trailing);
                }
            }

            return new SyntaxIdentifierWithTrivia(SyntaxKind.IdentifierToken, text, text, leading, trailing);
        }

        internal static SyntaxToken Identifier(SyntaxKind contextualKind, GreenNode leading, string text, string valueText, GreenNode trailing)
        {
            if (contextualKind == SyntaxKind.IdentifierToken && valueText == text)
            {
                return Identifier(leading, text, trailing);
            }

            return new SyntaxIdentifierWithTrivia(contextualKind, text, valueText, leading, trailing);
        }

        internal static SyntaxToken WithValue<T>(SyntaxKind kind, string text, T value)
        {
            return new SyntaxTokenWithValue<T>(kind, text, value);
        }

        internal static SyntaxToken WithValue<T>(SyntaxKind kind, GreenNode leading, string text, T value, GreenNode trailing)
        {
            return new SyntaxTokenWithValueAndTrivia<T>(kind, text, value, leading, trailing);
        }

        internal static SyntaxToken StringLiteral(string text)
        {
            return new SyntaxTokenWithValue<string>(SyntaxKind.StringLiteralToken, text, text);
        }

        internal static SyntaxToken StringLiteral(CSharpSyntaxNode leading, string text, CSharpSyntaxNode trailing)
        {
            return new SyntaxTokenWithValueAndTrivia<string>(SyntaxKind.StringLiteralToken, text, text, leading, trailing);
        }

        public virtual SyntaxKind ContextualKind
        {
            get
            {
                return this.Kind;
            }
        }

        public override int RawContextualKind
        {
            get
            {
                return (int)this.ContextualKind;
            }
        }

        public virtual string Text
        {
            get { return SyntaxFacts.GetText(this.Kind); }
        }

        /// <summary>
        /// Returns the string representation of this token, not including its leading and trailing trivia.
        /// </summary>
        /// <returns>The string representation of this token, not including its leading and trailing trivia.</returns>
        /// <remarks>The length of the returned string is always the same as Span.Length</remarks>
        public override string ToString()
        {
            return this.Text;
        }

        public virtual object Value
        {
            get
            {
                switch (this.Kind)
                {
                    case SyntaxKind.TrueKeyword:
                        return Boxes.BoxedTrue;
                    case SyntaxKind.FalseKeyword:
                        return Boxes.BoxedFalse;
                    case SyntaxKind.NullKeyword:
                        return null;
                    default:
                        return this.Text;
                }
            }
        }

        public override object GetValue()
        {
            return this.Value;
        }

        public virtual string ValueText
        {
            get { return this.Text; }
        }

        public override string GetValueText()
        {
            return this.ValueText;
        }

        public override int Width
        {
            get { return this.Text.Length; }
        }

        public override int GetLeadingTriviaWidth()
        {
            var leading = this.GetLeadingTrivia();
            return leading != null ? leading.FullWidth : 0;
        }

        public override int GetTrailingTriviaWidth()
        {
            var trailing = this.GetTrailingTrivia();
            return trailing != null ? trailing.FullWidth : 0;
        }

        internal SyntaxList<CSharpSyntaxNode> LeadingTrivia
        {
            get { return new SyntaxList<CSharpSyntaxNode>(this.GetLeadingTrivia()); }
        }

        internal SyntaxList<CSharpSyntaxNode> TrailingTrivia
        {
            get { return new SyntaxList<CSharpSyntaxNode>(this.GetTrailingTrivia()); }
        }

        public sealed override GreenNode WithLeadingTrivia(GreenNode trivia)
        {
            return TokenWithLeadingTrivia(trivia);
        }

        public virtual SyntaxToken TokenWithLeadingTrivia(GreenNode trivia)
        {
            return new SyntaxTokenWithTrivia(this.Kind, trivia, null, this.GetDiagnostics(), this.GetAnnotations());
        }

        public sealed override GreenNode WithTrailingTrivia(GreenNode trivia)
        {
            return TokenWithTrailingTrivia(trivia);
        }

        public virtual SyntaxToken TokenWithTrailingTrivia(GreenNode trivia)
        {
            return new SyntaxTokenWithTrivia(this.Kind, null, trivia, this.GetDiagnostics(), this.GetAnnotations());
        }

        internal override GreenNode SetDiagnostics(DiagnosticInfo[] diagnostics)
        {
            System.Diagnostics.Debug.Assert(this.GetType() == typeof(SyntaxToken));
            return new SyntaxToken(this.Kind, this.FullWidth, diagnostics, this.GetAnnotations());
        }

        internal override GreenNode SetAnnotations(SyntaxAnnotation[] annotations)
        {
            System.Diagnostics.Debug.Assert(this.GetType() == typeof(SyntaxToken));
            return new SyntaxToken(this.Kind, this.FullWidth, this.GetDiagnostics(), annotations);
        }

        internal override DirectiveStack ApplyDirectives(DirectiveStack stack)
        {
            if (this.ContainsDirectives)
            {
                stack = ApplyDirectivesToTrivia(this.GetLeadingTrivia(), stack);
                stack = ApplyDirectivesToTrivia(this.GetTrailingTrivia(), stack);
            }

            return stack;
        }

        private static DirectiveStack ApplyDirectivesToTrivia(GreenNode triviaList, DirectiveStack stack)
        {
            if (triviaList != null && triviaList.ContainsDirectives)
            {
                return ApplyDirectivesToListOrNode(triviaList, stack);
            }

            return stack;
        }

        public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor)
        {
            return visitor.VisitToken(this);
        }

        public override void Accept(CSharpSyntaxVisitor visitor)
        {
            visitor.VisitToken(this);
        }

        protected override void WriteTokenTo(System.IO.TextWriter writer, bool leading, bool trailing)
        {
            if (leading)
            {
                var trivia = this.GetLeadingTrivia();
                if (trivia != null)
                {
                    trivia.WriteTo(writer, true, true);
                }
            }

            writer.Write(this.Text);

            if (trailing)
            {
                var trivia = this.GetTrailingTrivia();
                if (trivia != null)
                {
                    trivia.WriteTo(writer, true, true);
                }
            }
        }

        public override bool IsEquivalentTo(GreenNode other)
        {
            if (!base.IsEquivalentTo(other))
            {
                return false;
            }

            var otherToken = (SyntaxToken)other;

            if (this.Text != otherToken.Text)
            {
                return false;
            }

            var thisLeading = this.GetLeadingTrivia();
            var otherLeading = otherToken.GetLeadingTrivia();
            if (thisLeading != otherLeading)
            {
                if (thisLeading == null || otherLeading == null)
                {
                    return false;
                }

                if (!thisLeading.IsEquivalentTo(otherLeading))
                {
                    return false;
                }
            }

            var thisTrailing = this.GetTrailingTrivia();
            var otherTrailing = otherToken.GetTrailingTrivia();
            if (thisTrailing != otherTrailing)
            {
                if (thisTrailing == null || otherTrailing == null)
                {
                    return false;
                }

                if (!thisTrailing.IsEquivalentTo(otherTrailing))
                {
                    return false;
                }
            }

            return true;
        }

        internal override SyntaxNode CreateRed(SyntaxNode parent, int position)
        {
            throw ExceptionUtilities.Unreachable();
        }
    }
}
