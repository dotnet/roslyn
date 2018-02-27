// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.Classification;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Classification
{
    public partial class ClassificationBuilder
    {
        public sealed class PunctuationClassificationTypes
        {
            [DebuggerStepThrough]
            private static FormattedClassification New(string text)
               => new FormattedClassification(text, ClassificationTypeNames.Punctuation);

            public FormattedClassification OpenCurly { get; } = New("{");
            public FormattedClassification CloseCurly { get; } = New("}");
            public FormattedClassification OpenParen { get; } = New("(");
            public FormattedClassification CloseParen { get; } = New(")");
            public FormattedClassification OpenAngle { get; } = New("<");
            public FormattedClassification CloseAngle { get; } = New(">");
            public FormattedClassification OpenBracket { get; } = New("[");
            public FormattedClassification CloseBracket { get; } = New("]");
            public FormattedClassification Comma { get; } = New(",");
            public FormattedClassification Semicolon { get; } = New(";");
            public FormattedClassification Colon { get; } = New(":");

            [DebuggerStepThrough]
            public FormattedClassification Text(string text)
            {
                return New(text);
            }
        }
    }
}
