// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Roslyn.Utilities;
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

            internal SyntaxTokenWithValue(ObjectReader reader)
                : base(reader)
            {
                this.TextField = reader.ReadString();
                this.FullWidth = this.TextField.Length;
                this.ValueField = (T)reader.ReadValue();
            }

            static SyntaxTokenWithValue()
            {
                ObjectBinder.RegisterTypeReader(typeof(SyntaxTokenWithValue<T>), r => new SyntaxTokenWithValue<T>(r));
            }

            internal override void WriteTo(ObjectWriter writer)
            {
                base.WriteTo(writer);
                writer.WriteString(this.TextField);
                writer.WriteValue(this.ValueField);
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
