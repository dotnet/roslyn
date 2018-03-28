// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal partial class SyntaxToken
    {
        internal class SyntaxIdentifier : SyntaxToken
        {
            static SyntaxIdentifier()
            {
                ObjectBinder.RegisterTypeReader(typeof(SyntaxIdentifier), r => new SyntaxIdentifier(r));
            }

            protected readonly string TextField;

            internal SyntaxIdentifier(string text)
                : base(SyntaxKind.IdentifierToken, text.Length)
            {
                this.TextField = text;
            }

            internal SyntaxIdentifier(string text, DiagnosticInfo[] diagnostics, SyntaxAnnotation[] annotations)
                : base(SyntaxKind.IdentifierToken, text.Length, diagnostics, annotations)
            {
                this.TextField = text;
            }

            internal SyntaxIdentifier(ObjectReader reader)
                : base(reader)
            {
                this.TextField = reader.ReadString();
                this.FullWidth = this.TextField.Length;
            }

            internal override void WriteTo(ObjectWriter writer)
            {
                base.WriteTo(writer);
                writer.WriteString(this.TextField);
            }

            public override string Text
            {
                get { return this.TextField; }
            }

            public override object Value
            {
                get { return this.TextField; }
            }

            public override string ValueText
            {
                get { return this.TextField; }
            }

            public override SyntaxToken TokenWithLeadingTrivia(GreenNode trivia)
            {
                return new SyntaxIdentifierWithTrivia(this.Kind, this.TextField, this.TextField, trivia, null, this.GetDiagnostics(), this.GetAnnotations());
            }

            public override SyntaxToken TokenWithTrailingTrivia(GreenNode trivia)
            {
                return new SyntaxIdentifierWithTrivia(this.Kind, this.TextField, this.TextField, null, trivia, this.GetDiagnostics(), this.GetAnnotations());
            }

            internal override GreenNode SetDiagnostics(DiagnosticInfo[] diagnostics)
            {
                return new SyntaxIdentifier(this.Text, diagnostics, this.GetAnnotations());
            }

            internal override GreenNode SetAnnotations(SyntaxAnnotation[] annotations)
            {
                return new SyntaxIdentifier(this.Text, this.GetDiagnostics(), annotations);
            }
        }
    }
}
