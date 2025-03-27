// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal partial class SyntaxToken
    {
        internal class SyntaxIdentifierWithTrailingTrivia : SyntaxIdentifier
        {
            private readonly GreenNode _trailing;

            internal SyntaxIdentifierWithTrailingTrivia(string text, GreenNode trailing)
                : base(text)
            {
                if (trailing != null)
                {
                    this.AdjustFlagsAndWidth(trailing);
                    _trailing = trailing;
                }
            }

            internal SyntaxIdentifierWithTrailingTrivia(string text, GreenNode trailing, DiagnosticInfo[] diagnostics, SyntaxAnnotation[] annotations)
                : base(text, diagnostics, annotations)
            {
                if (trailing != null)
                {
                    this.AdjustFlagsAndWidth(trailing);
                    _trailing = trailing;
                }
            }

            public override GreenNode GetTrailingTrivia()
            {
                return _trailing;
            }

            public override SyntaxToken TokenWithLeadingTrivia(GreenNode trivia)
            {
                return new SyntaxIdentifierWithTrivia(this.Kind, this.TextField, this.TextField, trivia, _trailing, this.GetDiagnostics(), this.GetAnnotations());
            }

            public override SyntaxToken TokenWithTrailingTrivia(GreenNode trivia)
            {
                return new SyntaxIdentifierWithTrailingTrivia(this.TextField, trivia, this.GetDiagnostics(), this.GetAnnotations());
            }

            internal override GreenNode SetDiagnostics(DiagnosticInfo[] diagnostics)
            {
                return new SyntaxIdentifierWithTrailingTrivia(this.TextField, _trailing, diagnostics, this.GetAnnotations());
            }

            internal override GreenNode SetAnnotations(SyntaxAnnotation[] annotations)
            {
                return new SyntaxIdentifierWithTrailingTrivia(this.TextField, _trailing, this.GetDiagnostics(), annotations);
            }
        }
    }
}
