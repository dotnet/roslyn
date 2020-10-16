﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeStyle
{
    public class CSharpEditorConfigCodeStyleParserTests
    {
        [Theory]
        [InlineData("ignore", BinaryOperatorSpacingOptions.Ignore)]
        [InlineData("none", BinaryOperatorSpacingOptions.Remove)]
        [InlineData("before_and_after", BinaryOperatorSpacingOptions.Single)]

        [WorkItem(27685, "https://github.com/dotnet/roslyn/issues/27685")]
        [InlineData(" ignore ", BinaryOperatorSpacingOptions.Ignore)]
        [InlineData(" none ", BinaryOperatorSpacingOptions.Remove)]
        [InlineData(" before_and_after ", BinaryOperatorSpacingOptions.Single)]
        public void TestParseSpacingAroundBinaryOperator(string rawValue, BinaryOperatorSpacingOptions parsedValue)
            => Assert.Equal(parsedValue, CSharpFormattingOptions2.ParseEditorConfigSpacingAroundBinaryOperator(rawValue));

        [Theory]
        [InlineData("flush_left", LabelPositionOptions.LeftMost)]
        [InlineData("no_change", LabelPositionOptions.NoIndent)]
        [InlineData("one_less_than_current", LabelPositionOptions.OneLess)]

        [WorkItem(27685, "https://github.com/dotnet/roslyn/issues/27685")]
        [InlineData(" flush_left ", LabelPositionOptions.LeftMost)]
        [InlineData(" no_change ", LabelPositionOptions.NoIndent)]
        [InlineData(" one_less_than_current ", LabelPositionOptions.OneLess)]
        public void TestParseLabelPositioning(string rawValue, LabelPositionOptions parsedValue)
            => Assert.Equal(parsedValue, CSharpFormattingOptions2.ParseEditorConfigLabelPositioning(rawValue));

        [Theory]
        [InlineData("false:none", (int)ExpressionBodyPreference.Never, ReportDiagnostic.Suppress)]
        [InlineData("true:warning", (int)ExpressionBodyPreference.WhenPossible, ReportDiagnostic.Warn)]
        [InlineData("when_on_single_line:error", (int)ExpressionBodyPreference.WhenOnSingleLine, ReportDiagnostic.Error)]

        [WorkItem(27685, "https://github.com/dotnet/roslyn/issues/27685")]
        [InlineData("false : none", (int)ExpressionBodyPreference.Never, ReportDiagnostic.Suppress)]
        [InlineData("true : warning", (int)ExpressionBodyPreference.WhenPossible, ReportDiagnostic.Warn)]
        [InlineData("when_on_single_line : error", (int)ExpressionBodyPreference.WhenOnSingleLine, ReportDiagnostic.Error)]
        public void TestParseExpressionBodyPreference(string optionString, int parsedValue, ReportDiagnostic severity)
        {
            var defaultValue = new CodeStyleOption2<ExpressionBodyPreference>(ExpressionBodyPreference.Never, NotificationOption2.Error);
            var codeStyleOption = CSharpCodeStyleOptions.ParseExpressionBodyPreference(optionString, defaultValue);

            Assert.NotSame(defaultValue, codeStyleOption);
            Assert.Equal((ExpressionBodyPreference)parsedValue, codeStyleOption.Value);
            Assert.Equal(severity, codeStyleOption.Notification.Severity);
        }
    }
}
