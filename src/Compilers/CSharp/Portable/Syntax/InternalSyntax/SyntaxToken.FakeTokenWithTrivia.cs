// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal partial class SyntaxToken
    {
        internal class FakeTokenWithTrivia : SyntaxTokenWithTrivia
        {
            private string _value;
            private bool _allowTrivia;

            internal FakeTokenWithTrivia(SyntaxKind kind, string value, bool allowTrivia)
                : base(kind, null, null)
            {
                _value = value;
                _allowTrivia = allowTrivia;
            }

            internal FakeTokenWithTrivia(SyntaxKind kind, GreenNode leading, GreenNode trailing, string value, bool allowTrivia)
                : base(kind, leading, trailing)
            {
                _value = value;
                _allowTrivia = allowTrivia;
            }

            internal FakeTokenWithTrivia(SyntaxKind kind, GreenNode leading, GreenNode trailing, DiagnosticInfo[] diagnostics, SyntaxAnnotation[] annotations, string value, bool allowTrivia)
                : base(kind, leading, trailing, diagnostics, annotations)
            {
                _value = value;
                _allowTrivia = allowTrivia;
            }

            internal FakeTokenWithTrivia(ObjectReader reader)
                : base(reader)
            {
            }

            static FakeTokenWithTrivia()
            {
                ObjectBinder.RegisterTypeReader(typeof(FakeTokenWithTrivia), r => new FakeTokenWithTrivia(r));
            }

            public override string Text => string.Empty;

            public override object Value => _value;

            public override SyntaxToken TokenWithLeadingTrivia(GreenNode trivia)
            {
                return new FakeTokenWithTrivia(this.Kind, _allowTrivia ? trivia : null, this.TrailingField, this.GetDiagnostics(), this.GetAnnotations(), _value, _allowTrivia);
            }

            public override SyntaxToken TokenWithTrailingTrivia(GreenNode trivia)
            {
                return new FakeTokenWithTrivia(this.Kind, this.LeadingField, _allowTrivia ? trivia : null, this.GetDiagnostics(), this.GetAnnotations(), _value, _allowTrivia);
            }

            internal override GreenNode SetDiagnostics(DiagnosticInfo[] diagnostics)
            {
                return new FakeTokenWithTrivia(this.Kind, this.LeadingField, this.TrailingField, diagnostics, this.GetAnnotations(), _value, _allowTrivia);
            }

            internal override GreenNode SetAnnotations(SyntaxAnnotation[] annotations)
            {
                return new FakeTokenWithTrivia(this.Kind, this.LeadingField, this.TrailingField, this.GetDiagnostics(), annotations, _value, _allowTrivia);
            }
        }
    }

    internal static class FakeSyntaxTokenExtensions
    {
        internal static bool IsFakeToken(this SyntaxToken token)
        {
            return token is SyntaxToken.FakeTokenWithTrivia;
        }
    }
}
