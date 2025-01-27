// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Formatting;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.CodeStyle
{
    public class EditorConfigCodeStyleParserTests
    {
        [Theory]
        [InlineData("true:none", true, ReportDiagnostic.Suppress)]
        [InlineData("true:refactoring", true, ReportDiagnostic.Hidden)]
        [InlineData("true:silent", true, ReportDiagnostic.Hidden)]
        [InlineData("true:suggestion", true, ReportDiagnostic.Info)]
        [InlineData("true:warning", true, ReportDiagnostic.Warn)]
        [InlineData("true:error", true, ReportDiagnostic.Error)]
        [InlineData("true", true, ReportDiagnostic.Hidden)]
        [InlineData("false:none", false, ReportDiagnostic.Suppress)]
        [InlineData("false:refactoring", false, ReportDiagnostic.Hidden)]
        [InlineData("false:silent", false, ReportDiagnostic.Hidden)]
        [InlineData("false:suggestion", false, ReportDiagnostic.Info)]
        [InlineData("false:warning", false, ReportDiagnostic.Warn)]
        [InlineData("false:error", false, ReportDiagnostic.Error)]
        [InlineData("false", false, ReportDiagnostic.Hidden)]
        [InlineData("*", false, ReportDiagnostic.Hidden)]
        [InlineData("false:false", false, ReportDiagnostic.Hidden)]

        [WorkItem("https://github.com/dotnet/roslyn/issues/27685")]
        [InlineData("true : warning", true, ReportDiagnostic.Warn)]
        [InlineData("false : warning", false, ReportDiagnostic.Warn)]
        [InlineData("true : error", true, ReportDiagnostic.Error)]
        [InlineData("false : error", false, ReportDiagnostic.Error)]
        public void TestParseEditorConfigCodeStyleOption(string args, bool isEnabled, ReportDiagnostic severity)
        {
            CodeStyleHelpers.TryParseBoolEditorConfigCodeStyleOption(args, defaultValue: CodeStyleOption2<bool>.Default, out var result);
            Assert.True(result.Value == isEnabled,
                        $"Expected {nameof(isEnabled)} to be {isEnabled}, was {result.Value}");
            Assert.True(result.Notification.Severity == severity,
                        $"Expected {nameof(severity)} to be {severity}, was {result.Notification.Severity}");
        }

        [Theory]
        [InlineData("never:none", AccessibilityModifiersRequired.Never, ReportDiagnostic.Suppress)]
        [InlineData("always:suggestion", AccessibilityModifiersRequired.Always, ReportDiagnostic.Info)]
        [InlineData("for_non_interface_members:warning", AccessibilityModifiersRequired.ForNonInterfaceMembers, ReportDiagnostic.Warn)]
        [InlineData("omit_if_default:error", AccessibilityModifiersRequired.OmitIfDefault, ReportDiagnostic.Error)]
        [InlineData("never : none", AccessibilityModifiersRequired.Never, ReportDiagnostic.Suppress), WorkItem("https://github.com/dotnet/roslyn/issues/27685")]
        [InlineData("always : suggestion", AccessibilityModifiersRequired.Always, ReportDiagnostic.Info)]
        [InlineData("for_non_interface_members : warning", AccessibilityModifiersRequired.ForNonInterfaceMembers, ReportDiagnostic.Warn)]
        [InlineData("omit_if_default : error", AccessibilityModifiersRequired.OmitIfDefault, ReportDiagnostic.Error)]
        internal void TestParseEditorConfigAccessibilityModifiers(string configurationString, AccessibilityModifiersRequired value, ReportDiagnostic severity)
        {
            Assert.True(CodeStyleOptions2.AccessibilityModifiersRequired.Definition.Serializer.TryParseValue(configurationString, out var parsedCodeStyleOption));

            Assert.Equal(value, parsedCodeStyleOption!.Value);
            Assert.Equal(severity, parsedCodeStyleOption.Notification.Severity);
        }

        [Theory]
        [InlineData("lf", "\n")]
        [InlineData("cr", "\r")]
        [InlineData("crlf", "\r\n")]

        [WorkItem("https://github.com/dotnet/roslyn/issues/27685")]
        [InlineData(" lf ", "\n")]
        [InlineData(" cr ", "\r")]
        [InlineData(" crlf ", "\r\n")]
        public void TestParseEditorConfigEndOfLine(string configurationString, string newLine)
        {
            Assert.True(FormattingOptions2.NewLine.Definition.Serializer.TryParseValue(configurationString, out var parsedNewLine));
            Assert.Equal(newLine, parsedNewLine);
        }
    }
}
