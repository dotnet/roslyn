// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    partial class SyntaxToken
    {
        internal class SyntaxIdentifierWithTrivia : SyntaxIdentifierExtended
        {
            private readonly CSharpSyntaxNode leading;
            private readonly CSharpSyntaxNode trailing;

            internal SyntaxIdentifierWithTrivia(
                SyntaxKind contextualKind,
                string text,
                string valueText,
                CSharpSyntaxNode leading,
                CSharpSyntaxNode trailing)
                : base(contextualKind, text, valueText)
            {
                if (leading != null)
                {
                    this.AdjustFlagsAndWidth(leading);
                    this.leading = leading;
                }
                if (trailing != null)
                {
                    this.AdjustFlagsAndWidth(trailing);
                    this.trailing = trailing;
                }
            }

            internal SyntaxIdentifierWithTrivia(
                SyntaxKind contextualKind,
                string text,
                string valueText,
                CSharpSyntaxNode leading,
                CSharpSyntaxNode trailing,
                DiagnosticInfo[] diagnostics,
                SyntaxAnnotation[] annotations)
                : base(contextualKind, text, valueText, diagnostics, annotations)
            {
                if (leading != null)
                {
                    this.AdjustFlagsAndWidth(leading);
                    this.leading = leading;
                }
                if (trailing != null)
                {
                    this.AdjustFlagsAndWidth(trailing);
                    this.trailing = trailing;
                }
            }

            internal SyntaxIdentifierWithTrivia(ObjectReader reader)
                : base(reader)
            {
                var leading = (CSharpSyntaxNode)reader.ReadValue();
                if (leading != null)
                {
                    this.AdjustFlagsAndWidth(leading);
                    this.leading = leading;
                }
                var trailing = (CSharpSyntaxNode)reader.ReadValue();
                if (trailing != null)
                {
                    this.trailing = trailing;
                    this.AdjustFlagsAndWidth(trailing);
                }
            }

            internal override Func<ObjectReader, object> GetReader()
            {
                return r => new SyntaxIdentifierWithTrivia(r);
            }

            internal override void WriteTo(ObjectWriter writer)
            {
                base.WriteTo(writer);
                writer.WriteValue(this.leading);
                writer.WriteValue(this.trailing);
            }

            public override CSharpSyntaxNode GetLeadingTrivia()
            {
                return this.leading;
            }

            public override CSharpSyntaxNode GetTrailingTrivia()
            {
                return this.trailing;
            }

            internal override SyntaxToken WithLeadingTrivia(CSharpSyntaxNode trivia)
            {
                return new SyntaxIdentifierWithTrivia(this.contextualKind, this.TextField, this.valueText, trivia, this.trailing, this.GetDiagnostics(), this.GetAnnotations());
            }

            internal override SyntaxToken WithTrailingTrivia(CSharpSyntaxNode trivia)
            {
                return new SyntaxIdentifierWithTrivia(this.contextualKind, this.TextField, this.valueText, this.leading, trivia, this.GetDiagnostics(), this.GetAnnotations());
            }

            internal override GreenNode SetDiagnostics(DiagnosticInfo[] diagnostics)
            {
                return new SyntaxIdentifierWithTrivia(this.contextualKind, this.TextField, this.valueText, this.leading, this.trailing, diagnostics, this.GetAnnotations());
            }

            internal override GreenNode SetAnnotations(SyntaxAnnotation[] annotations)
            {
                return new SyntaxIdentifierWithTrivia(this.contextualKind, this.TextField, this.valueText, this.leading, this.trailing, this.GetDiagnostics(), annotations);
            }
        }
    }
}