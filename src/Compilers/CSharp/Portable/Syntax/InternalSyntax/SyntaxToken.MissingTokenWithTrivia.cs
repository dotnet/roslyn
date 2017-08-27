// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal partial class SyntaxToken
    {
        internal class MissingTokenWithTrivia : SyntaxTokenWithTrivia
        {
            internal MissingTokenWithTrivia(SyntaxKind kind, GreenNode leading, GreenNode trailing)
                : base(kind, leading, trailing)
            {
                this.flags &= ~NodeFlags.IsNotMissing;
            }

            internal MissingTokenWithTrivia(SyntaxKind kind, GreenNode leading, GreenNode trailing, DiagnosticInfo[] diagnostics, SyntaxAnnotation[] annotations)
                : base(kind, leading, trailing, diagnostics, annotations)
            {
                this.flags &= ~NodeFlags.IsNotMissing;
            }

            internal MissingTokenWithTrivia(ObjectReader reader)
                : base(reader)
            {
                this.flags &= ~NodeFlags.IsNotMissing;
            }

            static MissingTokenWithTrivia()
            {
                ObjectBinder.RegisterTypeReader(typeof(MissingTokenWithTrivia), r => new MissingTokenWithTrivia(r));
            }

            public override string Text
            {
                get { return string.Empty; }
            }

            public override object Value
            {
                get
                {
                    switch (this.Kind)
                    {
                        case SyntaxKind.IdentifierToken:
                            return string.Empty;
                        default:
                            return null;
                    }
                }
            }

            public override SyntaxToken TokenWithLeadingTrivia(GreenNode trivia)
            {
                return new MissingTokenWithTrivia(this.Kind, trivia, this.TrailingField, this.GetDiagnostics(), this.GetAnnotations());
            }

            public override SyntaxToken TokenWithTrailingTrivia(GreenNode trivia)
            {
                return new MissingTokenWithTrivia(this.Kind, this.LeadingField, trivia, this.GetDiagnostics(), this.GetAnnotations());
            }

            internal override GreenNode SetDiagnostics(DiagnosticInfo[] diagnostics)
            {
                return new MissingTokenWithTrivia(this.Kind, this.LeadingField, this.TrailingField, diagnostics, this.GetAnnotations());
            }

            internal override GreenNode SetAnnotations(SyntaxAnnotation[] annotations)
            {
                return new MissingTokenWithTrivia(this.Kind, this.LeadingField, this.TrailingField, this.GetDiagnostics(), annotations);
            }
        }
    }
}
