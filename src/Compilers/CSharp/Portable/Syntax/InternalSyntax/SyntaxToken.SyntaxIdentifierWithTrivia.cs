// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal partial class SyntaxToken
    {
        internal class SyntaxIdentifierWithTrivia : SyntaxIdentifierExtended
        {
            private readonly CSharpSyntaxNode _leading;
            private readonly CSharpSyntaxNode _trailing;

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
                    _leading = leading;
                }
                if (trailing != null)
                {
                    this.AdjustFlagsAndWidth(trailing);
                    _trailing = trailing;
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
                    _leading = leading;
                }
                if (trailing != null)
                {
                    this.AdjustFlagsAndWidth(trailing);
                    _trailing = trailing;
                }
            }

            internal SyntaxIdentifierWithTrivia(ObjectReader reader)
                : base(reader)
            {
                var leading = (CSharpSyntaxNode)reader.ReadValue();
                if (leading != null)
                {
                    this.AdjustFlagsAndWidth(leading);
                    _leading = leading;
                }
                var trailing = (CSharpSyntaxNode)reader.ReadValue();
                if (trailing != null)
                {
                    _trailing = trailing;
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
                writer.WriteValue(_leading);
                writer.WriteValue(_trailing);
            }

            public override CSharpSyntaxNode GetLeadingTrivia()
            {
                return _leading;
            }

            public override CSharpSyntaxNode GetTrailingTrivia()
            {
                return _trailing;
            }

            internal override SyntaxToken WithLeadingTrivia(CSharpSyntaxNode trivia)
            {
                return new SyntaxIdentifierWithTrivia(this.contextualKind, this.TextField, this.valueText, trivia, _trailing, this.GetDiagnostics(), this.GetAnnotations());
            }

            internal override SyntaxToken WithTrailingTrivia(CSharpSyntaxNode trivia)
            {
                return new SyntaxIdentifierWithTrivia(this.contextualKind, this.TextField, this.valueText, _leading, trivia, this.GetDiagnostics(), this.GetAnnotations());
            }

            internal override GreenNode SetDiagnostics(DiagnosticInfo[] diagnostics)
            {
                return new SyntaxIdentifierWithTrivia(this.contextualKind, this.TextField, this.valueText, _leading, _trailing, diagnostics, this.GetAnnotations());
            }

            internal override GreenNode SetAnnotations(SyntaxAnnotation[] annotations)
            {
                return new SyntaxIdentifierWithTrivia(this.contextualKind, this.TextField, this.valueText, _leading, _trailing, this.GetDiagnostics(), annotations);
            }
        }
    }
}
