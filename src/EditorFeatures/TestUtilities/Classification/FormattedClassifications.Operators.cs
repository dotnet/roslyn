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
        public static class Operators
        {
            [DebuggerStepThrough]
            private static FormattedClassification New(string text)
                => new FormattedClassification(text, ClassificationTypeNames.Operator);

            public static FormattedClassification Ampersand { get; } = New("&");
            public static FormattedClassification AmpersandAmpersand { get; } = New("&&");
            public static FormattedClassification AmpersandEquals { get; } = New("&=");
            public static FormattedClassification Asterisk { get; } = New("*");
            public static FormattedClassification AsteriskEquals { get; } = New("*=");
            public static FormattedClassification Bar { get; } = New("|");
            public static FormattedClassification BarBar { get; } = New("||");
            public static FormattedClassification BarEquals { get; } = New("|=");
            public static FormattedClassification Caret { get; } = New("^");
            public static FormattedClassification CaretEquals { get; } = New("^=");
            public static FormattedClassification Colon { get; } = New(":");
            public static FormattedClassification ColonColon { get; } = New("::");
            public static FormattedClassification ColonEquals { get; } = New(":=");
            public static FormattedClassification Dot { get; } = New(".");
            public static new FormattedClassification Equals { get; } = New("=");
            public static FormattedClassification EqualsEquals { get; } = New("==");
            public static FormattedClassification EqualsGreaterThan { get; } = New("=>");
            public static FormattedClassification Exclamation { get; } = New("!");
            public static FormattedClassification ExclamationEquals { get; } = New("!=");
            public static FormattedClassification GreaterThan { get; } = New(">");
            public static FormattedClassification GreaterThanEquals { get; } = New(">=");
            public static FormattedClassification GreaterThanGreaterThan { get; } = New(">>");
            public static FormattedClassification GreaterThanGreaterThanGreaterThan { get; } = New(">>>");
            public static FormattedClassification GreaterThanGreaterThanEquals { get; } = New(">>=");
            public static FormattedClassification GreaterThanGreaterThanGreaterThanEquals { get; } = New(">>>=");
            public static FormattedClassification LessThan { get; } = New("<");
            public static FormattedClassification LessThanEquals { get; } = New("<=");
            public static FormattedClassification LessThanGreaterThan { get; } = New("<>");
            public static FormattedClassification LessThanLessThan { get; } = New("<<");
            public static FormattedClassification LessThanLessThanEquals { get; } = New("<<=");
            public static FormattedClassification Minus { get; } = New("-");
            public static FormattedClassification MinusEquals { get; } = New("-=");
            public static FormattedClassification MinusGreaterThan { get; } = New("->");
            public static FormattedClassification MinusMinus { get; } = New("--");
            public static FormattedClassification Percent { get; } = New("%");
            public static FormattedClassification PercentEquals { get; } = New("%=");
            public static FormattedClassification Plus { get; } = New("+");
            public static FormattedClassification PlusEquals { get; } = New("+=");
            public static FormattedClassification PlusPlus { get; } = New("++");
            public static FormattedClassification QuestionMark { get; } = New("?");
            public static FormattedClassification QuestionQuestionEquals { get; } = New("??=");
            public static FormattedClassification Slash { get; } = New("/");
            public static FormattedClassification SlashEquals { get; } = New("/=");
            public static FormattedClassification Tilde { get; } = New("~");
        }
    }
}
