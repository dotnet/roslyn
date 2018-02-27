// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.Classification;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Classification
{
    public partial class ClassificationBuilder
    {
        public sealed class OperatorClassificationTypes
        {
            [DebuggerStepThrough]
            private static FormattedClassification New(string text)
                => new FormattedClassification(text, ClassificationTypeNames.Operator);

            public FormattedClassification Ampersand { get; } = New("&");
            public FormattedClassification AmpersandAmpersand { get; } = New("&&");
            public FormattedClassification AmpersandEquals { get; } = New("&=");
            public FormattedClassification Asterisk { get; } = New("*");
            public FormattedClassification AsteriskEquals { get; } = New("*=");
            public FormattedClassification Bar { get; } = New("|");
            public FormattedClassification BarBar { get; } = New("||");
            public FormattedClassification BarEquals { get; } = New("|=");
            public FormattedClassification Caret { get; } = New("^");
            public FormattedClassification CaretEquals { get; } = New("^=");
            public FormattedClassification Colon { get; } = New(":");
            public FormattedClassification ColonColon { get; } = New("::");
            public FormattedClassification ColonEquals { get; } = New(":=");
            public FormattedClassification Dot { get; } = New(".");
            public new FormattedClassification Equals { get; } = New("=");
            public FormattedClassification EqualsEquals { get; } = New("==");
            public FormattedClassification EqualsGreaterThan { get; } = New("=>");
            public FormattedClassification Exclamation { get; } = New("!");
            public FormattedClassification ExclamationEquals { get; } = New("!=");
            public FormattedClassification GreaterThan { get; } = New(">");
            public FormattedClassification GreaterThanEquals { get; } = New(">=");
            public FormattedClassification GreaterThanGreaterThan { get; } = New(">>");
            public FormattedClassification GreaterThanGreaterThanEquals { get; } = New(">>=");
            public FormattedClassification LessThan { get; } = New("<");
            public FormattedClassification LessThanEquals { get; } = New("<=");
            public FormattedClassification LessThanGreaterThan { get; } = New("<>");
            public FormattedClassification LessThanLessThan { get; } = New("<<");
            public FormattedClassification LessThanLessThanEquals { get; } = New("<<=");
            public FormattedClassification Minus { get; } = New("-");
            public FormattedClassification MinusEquals { get; } = New("-=");
            public FormattedClassification MinusGreaterThan { get; } = New("->");
            public FormattedClassification MinusMinus { get; } = New("--");
            public FormattedClassification Percent { get; } = New("%");
            public FormattedClassification PercentEquals { get; } = New("%=");
            public FormattedClassification Plus { get; } = New("+");
            public FormattedClassification PlusEquals { get; } = New("+=");
            public FormattedClassification PlusPlus { get; } = New("++");
            public FormattedClassification QuestionMark { get; } = New("?");
            public FormattedClassification Slash { get; } = New("/");
            public FormattedClassification SlashEquals { get; } = New("/=");
            public FormattedClassification Tilde { get; } = New("~");
        }
    }
}
