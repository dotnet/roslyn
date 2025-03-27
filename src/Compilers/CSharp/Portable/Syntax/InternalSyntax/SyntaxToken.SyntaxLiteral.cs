// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Globalization;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal partial class SyntaxToken
    {
        internal class SyntaxTokenWithValue<T> : SyntaxToken
        {
            protected readonly string TextField;
            protected readonly T ValueField;

            internal SyntaxTokenWithValue(SyntaxKind kind, string text, T value)
                : base(kind, text.Length)
            {
                this.TextField = text;
                this.ValueField = value;
            }

            internal SyntaxTokenWithValue(SyntaxKind kind, string text, T value, DiagnosticInfo[] diagnostics, SyntaxAnnotation[] annotations)
                : base(kind, text.Length, diagnostics, annotations)
            {
                this.TextField = text;
                this.ValueField = value;
            }

            public override string Text
            {
                get
                {
                    return this.TextField;
                }
            }

            public override object Value
            {
                get
                {
                    return this.ValueField;
                }
            }

            public override string ValueText
            {
                get
                {
                    return Convert.ToString(this.ValueField, CultureInfo.InvariantCulture);
                }
            }

            public override SyntaxToken TokenWithLeadingTrivia(GreenNode trivia)
            {
                return new SyntaxTokenWithValueAndTrivia<T>(this.Kind, this.TextField, this.ValueField, trivia, null, this.GetDiagnostics(), this.GetAnnotations());
            }

            public override SyntaxToken TokenWithTrailingTrivia(GreenNode trivia)
            {
                return new SyntaxTokenWithValueAndTrivia<T>(this.Kind, this.TextField, this.ValueField, null, trivia, this.GetDiagnostics(), this.GetAnnotations());
            }

            internal override GreenNode SetDiagnostics(DiagnosticInfo[] diagnostics)
            {
                return new SyntaxTokenWithValue<T>(this.Kind, this.TextField, this.ValueField, diagnostics, this.GetAnnotations());
            }

            internal override GreenNode SetAnnotations(SyntaxAnnotation[] annotations)
            {
                return new SyntaxTokenWithValue<T>(this.Kind, this.TextField, this.ValueField, this.GetDiagnostics(), annotations);
            }
        }
    }
}
