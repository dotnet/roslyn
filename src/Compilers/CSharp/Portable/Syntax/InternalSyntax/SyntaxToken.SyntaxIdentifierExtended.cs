// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal partial class SyntaxToken
    {
        internal class SyntaxIdentifierExtended : SyntaxIdentifier
        {
            protected readonly SyntaxKind contextualKind;
            protected readonly string valueText;

            internal SyntaxIdentifierExtended(SyntaxKind contextualKind, string text, string valueText)
                : base(text)
            {
                this.contextualKind = contextualKind;
                this.valueText = valueText;
            }

            internal SyntaxIdentifierExtended(SyntaxKind contextualKind, string text, string valueText, DiagnosticInfo[] diagnostics, SyntaxAnnotation[] annotations)
                : base(text, diagnostics, annotations)
            {
                this.contextualKind = contextualKind;
                this.valueText = valueText;
            }

            public override SyntaxKind ContextualKind
            {
                get { return this.contextualKind; }
            }

            public override string ValueText
            {
                get { return this.valueText; }
            }

            public override object Value
            {
                get { return this.valueText; }
            }

            public override SyntaxToken TokenWithLeadingTrivia(GreenNode trivia)
            {
                return new SyntaxIdentifierWithTrivia(this.contextualKind, this.TextField, this.valueText, trivia, null, this.GetDiagnostics(), this.GetAnnotations());
            }

            public override SyntaxToken TokenWithTrailingTrivia(GreenNode trivia)
            {
                return new SyntaxIdentifierWithTrivia(this.contextualKind, this.TextField, this.valueText, null, trivia, this.GetDiagnostics(), this.GetAnnotations());
            }

            internal override GreenNode SetDiagnostics(DiagnosticInfo[] diagnostics)
            {
                return new SyntaxIdentifierExtended(this.contextualKind, this.TextField, this.valueText, diagnostics, this.GetAnnotations());
            }

            internal override GreenNode SetAnnotations(SyntaxAnnotation[] annotations)
            {
                return new SyntaxIdentifierExtended(this.contextualKind, this.TextField, this.valueText, this.GetDiagnostics(), annotations);
            }
        }
    }
}
