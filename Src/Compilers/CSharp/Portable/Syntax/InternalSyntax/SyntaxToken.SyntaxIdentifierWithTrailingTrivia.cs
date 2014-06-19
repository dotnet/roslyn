// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    partial class SyntaxToken
    {
        internal class SyntaxIdentifierWithTrailingTrivia : SyntaxIdentifier
        {
            private readonly CSharpSyntaxNode trailing;

            internal SyntaxIdentifierWithTrailingTrivia(string text, CSharpSyntaxNode trailing)
                : base(text)
            {
                if (trailing != null)
                {
                    this.AdjustFlagsAndWidth(trailing);
                    this.trailing = trailing;
                }
            }

            internal SyntaxIdentifierWithTrailingTrivia(string text, CSharpSyntaxNode trailing, DiagnosticInfo[] diagnostics, SyntaxAnnotation[] annotations)
                : base(text, diagnostics, annotations)
            {
                if (trailing != null)
                {
                    this.AdjustFlagsAndWidth(trailing);
                    this.trailing = trailing;
                }
            }

            internal SyntaxIdentifierWithTrailingTrivia(ObjectReader reader)
                : base(reader)
            {
                var trailing = (CSharpSyntaxNode)reader.ReadValue();
                if (trailing != null)
                {
                    this.AdjustFlagsAndWidth(trailing);
                    this.trailing = trailing;
                }
            }

            internal override Func<ObjectReader, object> GetReader()
            {
                return r => new SyntaxIdentifierWithTrailingTrivia(r);
            }

            internal override void WriteTo(ObjectWriter writer)
            {
                base.WriteTo(writer);
                writer.WriteValue(this.trailing);
            }

            public override CSharpSyntaxNode GetTrailingTrivia()
            {
                return this.trailing;
            }

            internal override SyntaxToken WithLeadingTrivia(CSharpSyntaxNode trivia)
            {
                return new SyntaxIdentifierWithTrivia(this.Kind, this.TextField, this.TextField, trivia, this.trailing, this.GetDiagnostics(), this.GetAnnotations());
            }

            internal override SyntaxToken WithTrailingTrivia(CSharpSyntaxNode trivia)
            {
                return new SyntaxIdentifierWithTrailingTrivia(this.TextField, trivia, this.GetDiagnostics(), this.GetAnnotations());
            }

            internal override GreenNode SetDiagnostics(DiagnosticInfo[] diagnostics)
            {
                return new SyntaxIdentifierWithTrailingTrivia(this.TextField, this.trailing, diagnostics, this.GetAnnotations());
            }

            internal override GreenNode SetAnnotations(SyntaxAnnotation[] annotations)
            {
                return new SyntaxIdentifierWithTrailingTrivia(this.TextField, this.trailing, this.GetDiagnostics(), annotations);
            }
        }
    }
}