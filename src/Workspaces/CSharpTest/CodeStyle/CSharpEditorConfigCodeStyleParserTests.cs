// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeStyle;

public sealed class CSharpEditorConfigCodeStyleParserTests
{
    [Theory]
    [InlineData("ignore", BinaryOperatorSpacingOptions.Ignore)]
    [InlineData("none", BinaryOperatorSpacingOptions.Remove)]
    [InlineData("before_and_after", BinaryOperatorSpacingOptions.Single)]

    [WorkItem("https://github.com/dotnet/roslyn/issues/27685")]
    [InlineData(" ignore ", BinaryOperatorSpacingOptions.Ignore)]
    [InlineData(" none ", BinaryOperatorSpacingOptions.Remove)]
    [InlineData(" before_and_after ", BinaryOperatorSpacingOptions.Single)]
    public void TestParseSpacingAroundBinaryOperator(string rawValue, BinaryOperatorSpacingOptions parsedValue)
        => Assert.Equal(parsedValue, (BinaryOperatorSpacingOptions)CSharpFormattingOptions2.ParseEditorConfigSpacingAroundBinaryOperator(rawValue));

    [Theory]
    [InlineData("flush_left", LabelPositionOptions.LeftMost)]
    [InlineData("no_change", LabelPositionOptions.NoIndent)]
    [InlineData("one_less_than_current", LabelPositionOptions.OneLess)]

    [WorkItem("https://github.com/dotnet/roslyn/issues/27685")]
    [InlineData(" flush_left ", LabelPositionOptions.LeftMost)]
    [InlineData(" no_change ", LabelPositionOptions.NoIndent)]
    [InlineData(" one_less_than_current ", LabelPositionOptions.OneLess)]
    public void TestParseLabelPositioning(string rawValue, LabelPositionOptions parsedValue)
        => Assert.Equal(parsedValue, (LabelPositionOptions)CSharpFormattingOptions2.ParseEditorConfigLabelPositioning(rawValue));

    [Theory]
    [InlineData("false:none", (int)ExpressionBodyPreference.Never, ReportDiagnostic.Suppress)]
    [InlineData("true:warning", (int)ExpressionBodyPreference.WhenPossible, ReportDiagnostic.Warn)]
    [InlineData("when_on_single_line:error", (int)ExpressionBodyPreference.WhenOnSingleLine, ReportDiagnostic.Error)]

    [WorkItem("https://github.com/dotnet/roslyn/issues/27685")]
    [InlineData("false : none", (int)ExpressionBodyPreference.Never, ReportDiagnostic.Suppress)]
    [InlineData("true : warning", (int)ExpressionBodyPreference.WhenPossible, ReportDiagnostic.Warn)]
    [InlineData("when_on_single_line : error", (int)ExpressionBodyPreference.WhenOnSingleLine, ReportDiagnostic.Error)]

    [InlineData("false", (int)ExpressionBodyPreference.Never, null)]
    [InlineData("true", (int)ExpressionBodyPreference.WhenPossible, null)]
    [InlineData("when_on_single_line", (int)ExpressionBodyPreference.WhenOnSingleLine, null)]
    public void TestParseExpressionBodyPreference(string optionString, int parsedValue, ReportDiagnostic? severity)
    {
        var defaultValue = new CodeStyleOption2<ExpressionBodyPreference>(ExpressionBodyPreference.Never, NotificationOption2.Error);
        severity ??= ReportDiagnostic.Error;
        var codeStyleOption = CSharpCodeStyleOptions.ParseExpressionBodyPreference(optionString, defaultValue);

        Assert.NotSame(defaultValue, codeStyleOption);
        Assert.Equal((ExpressionBodyPreference)parsedValue, codeStyleOption.Value);
        Assert.Equal(severity, codeStyleOption.Notification.Severity);
    }

    [Theory]
    [InlineData("inside_namespace:warning", AddImportPlacement.InsideNamespace, ReportDiagnostic.Warn)]
    [InlineData("outside_namespace:suggestion", AddImportPlacement.OutsideNamespace, ReportDiagnostic.Info)]
    [InlineData("inside_namespace", AddImportPlacement.InsideNamespace, null)]
    [InlineData("outside_namespace", AddImportPlacement.OutsideNamespace, null)]
    internal void TestParseUsingDirectivesPlacement(string optionString, AddImportPlacement parsedValue, ReportDiagnostic? severity)
    {
        var defaultValue = new CodeStyleOption2<AddImportPlacement>(AddImportPlacement.InsideNamespace, NotificationOption2.Error);
        severity ??= ReportDiagnostic.Error;
        var codeStyleOption = CSharpCodeStyleOptions.ParseUsingDirectivesPlacement(optionString, defaultValue);

        Assert.NotSame(defaultValue, codeStyleOption);
        Assert.Equal(parsedValue, codeStyleOption.Value);
        Assert.Equal(severity, codeStyleOption.Notification.Severity);
    }
}
