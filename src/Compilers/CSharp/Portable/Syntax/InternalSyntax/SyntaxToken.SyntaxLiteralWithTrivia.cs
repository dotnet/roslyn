﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal partial class SyntaxToken
    {
        internal class SyntaxTokenWithValueAndTrivia<T> : SyntaxTokenWithValue<T>
        {
            static SyntaxTokenWithValueAndTrivia()
            {
                ObjectBinder.RegisterTypeReader(typeof(SyntaxTokenWithValueAndTrivia<T>), r => new SyntaxTokenWithValueAndTrivia<T>(r));
            }

            private readonly GreenNode _leading;
            private readonly GreenNode _trailing;

            internal SyntaxTokenWithValueAndTrivia(SyntaxKind kind, string text, T value, GreenNode leading, GreenNode trailing)
                : base(kind, text, value)
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

            internal SyntaxTokenWithValueAndTrivia(
                SyntaxKind kind,
                string text,
                T value,
                GreenNode leading,
                GreenNode trailing,
                DiagnosticInfo[] diagnostics,
                SyntaxAnnotation[] annotations)
                : base(kind, text, value, diagnostics, annotations)
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

            internal SyntaxTokenWithValueAndTrivia(ObjectReader reader)
                : base(reader)
            {
                var leading = (GreenNode)reader.ReadValue();
                if (leading != null)
                {
                    this.AdjustFlagsAndWidth(leading);
                    _leading = leading;
                }
                var trailing = (GreenNode)reader.ReadValue();
                if (trailing != null)
                {
                    this.AdjustFlagsAndWidth(trailing);
                    _trailing = trailing;
                }
            }

            internal override void WriteTo(ObjectWriter writer)
            {
                base.WriteTo(writer);
                writer.WriteValue(_leading);
                writer.WriteValue(_trailing);
            }

            public override GreenNode GetLeadingTrivia()
            {
                return _leading;
            }

            public override GreenNode GetTrailingTrivia()
            {
                return _trailing;
            }

            public override SyntaxToken TokenWithLeadingTrivia(GreenNode trivia)
            {
                return new SyntaxTokenWithValueAndTrivia<T>(this.Kind, this.TextField, this.ValueField, trivia, _trailing, this.GetDiagnostics(), this.GetAnnotations());
            }

            public override SyntaxToken TokenWithTrailingTrivia(GreenNode trivia)
            {
                return new SyntaxTokenWithValueAndTrivia<T>(this.Kind, this.TextField, this.ValueField, _leading, trivia, this.GetDiagnostics(), this.GetAnnotations());
            }

            internal override GreenNode SetDiagnostics(DiagnosticInfo[] diagnostics)
            {
                return new SyntaxTokenWithValueAndTrivia<T>(this.Kind, this.TextField, this.ValueField, _leading, _trailing, diagnostics, this.GetAnnotations());
            }

            internal override GreenNode SetAnnotations(SyntaxAnnotation[] annotations)
            {
                return new SyntaxTokenWithValueAndTrivia<T>(this.Kind, this.TextField, this.ValueField, _leading, _trailing, this.GetDiagnostics(), annotations);
            }
        }
    }
}
