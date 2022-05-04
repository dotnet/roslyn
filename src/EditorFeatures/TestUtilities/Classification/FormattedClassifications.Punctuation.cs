// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Diagnostics;
using Microsoft.CodeAnalysis.Classification;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Classification
{
    public static partial class FormattedClassifications
    {
        public static class Punctuation
        {
            [DebuggerStepThrough]
            private static FormattedClassification New(string text)
               => new FormattedClassification(text, ClassificationTypeNames.Punctuation);

            public static FormattedClassification OpenCurly { get; } = New("{");
            public static FormattedClassification CloseCurly { get; } = New("}");
            public static FormattedClassification OpenParen { get; } = New("(");
            public static FormattedClassification CloseParen { get; } = New(")");
            public static FormattedClassification OpenAngle { get; } = New("<");
            public static FormattedClassification CloseAngle { get; } = New(">");
            public static FormattedClassification OpenBracket { get; } = New("[");
            public static FormattedClassification CloseBracket { get; } = New("]");
            public static FormattedClassification Comma { get; } = New(",");
            public static FormattedClassification Semicolon { get; } = New(";");
            public static FormattedClassification Colon { get; } = New(":");
            public static FormattedClassification DotDot { get; } = New("..");
            public static FormattedClassification ExclamationExclamation { get; } = New("!!");

            [DebuggerStepThrough]
            public static FormattedClassification Text(string text) => New(text);
        }
    }
}
