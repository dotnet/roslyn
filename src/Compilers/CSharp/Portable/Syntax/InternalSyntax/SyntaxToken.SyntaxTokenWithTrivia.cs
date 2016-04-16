// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal partial class SyntaxToken
    {
        internal class SyntaxTokenWithTrivia : SyntaxToken
        {
            protected readonly CSharpSyntaxNode LeadingField;
            protected readonly CSharpSyntaxNode TrailingField;

            internal SyntaxTokenWithTrivia(SyntaxKind kind, CSharpSyntaxNode leading, CSharpSyntaxNode trailing)
                : base(kind)
            {
                if (leading != null)
                {
                    this.AdjustFlagsAndWidth(leading);
                    this.LeadingField = leading;
                }
                if (trailing != null)
                {
                    this.AdjustFlagsAndWidth(trailing);
                    this.TrailingField = trailing;
                }
            }

            internal SyntaxTokenWithTrivia(SyntaxKind kind, CSharpSyntaxNode leading, CSharpSyntaxNode trailing, DiagnosticInfo[] diagnostics, SyntaxAnnotation[] annotations)
                : base(kind, diagnostics, annotations)
            {
                if (leading != null)
                {
                    this.AdjustFlagsAndWidth(leading);
                    this.LeadingField = leading;
                }
                if (trailing != null)
                {
                    this.AdjustFlagsAndWidth(trailing);
                    this.TrailingField = trailing;
                }
            }

            internal SyntaxTokenWithTrivia(ObjectReader reader)
                : base(reader)
            {
                var leading = (CSharpSyntaxNode)reader.ReadValue();
                if (leading != null)
                {
                    this.AdjustFlagsAndWidth(leading);
                    this.LeadingField = leading;
                }
                var trailing = (CSharpSyntaxNode)reader.ReadValue();
                if (trailing != null)
                {
                    this.AdjustFlagsAndWidth(trailing);
                    this.TrailingField = trailing;
                }
            }

            internal override Func<ObjectReader, object> GetReader()
            {
                return r => new SyntaxTokenWithTrivia(r);
            }

            internal override void WriteTo(ObjectWriter writer)
            {
                base.WriteTo(writer);
                writer.WriteValue(this.LeadingField);
                writer.WriteValue(this.TrailingField);
            }

            public override CSharpSyntaxNode GetLeadingTrivia()
            {
                return this.LeadingField;
            }

            public override CSharpSyntaxNode GetTrailingTrivia()
            {
                return this.TrailingField;
            }

            internal override SyntaxToken WithLeadingTrivia(CSharpSyntaxNode trivia)
            {
                return new SyntaxTokenWithTrivia(this.Kind, trivia, this.TrailingField, this.GetDiagnostics(), this.GetAnnotations());
            }

            internal override SyntaxToken WithTrailingTrivia(CSharpSyntaxNode trivia)
            {
                return new SyntaxTokenWithTrivia(this.Kind, this.LeadingField, trivia, this.GetDiagnostics(), this.GetAnnotations());
            }

            internal override GreenNode SetDiagnostics(DiagnosticInfo[] diagnostics)
            {
                return new SyntaxTokenWithTrivia(this.Kind, this.LeadingField, this.TrailingField, diagnostics, this.GetAnnotations());
            }

            internal override GreenNode SetAnnotations(SyntaxAnnotation[] annotations)
            {
                return new SyntaxTokenWithTrivia(this.Kind, this.LeadingField, this.TrailingField, this.GetDiagnostics(), annotations);
            }
        }
    }
}
