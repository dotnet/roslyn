// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
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
        [InlineData("true", false, ReportDiagnostic.Hidden)]
        [InlineData("false:none", false, ReportDiagnostic.Suppress)]
        [InlineData("false:refactoring", false, ReportDiagnostic.Hidden)]
        [InlineData("false:silent", false, ReportDiagnostic.Hidden)]
        [InlineData("false:suggestion", false, ReportDiagnostic.Info)]
        [InlineData("false:warning", false, ReportDiagnostic.Warn)]
        [InlineData("false:error", false, ReportDiagnostic.Error)]
        [InlineData("false", false, ReportDiagnostic.Hidden)]
        [InlineData("*", false, ReportDiagnostic.Hidden)]
        [InlineData("false:false", false, ReportDiagnostic.Hidden)]

        [WorkItem(27685, "https://github.com/dotnet/roslyn/issues/27685")]
        [InlineData("true : warning", true, ReportDiagnostic.Warn)]
        [InlineData("false : warning", false, ReportDiagnostic.Warn)]
        [InlineData("true : error", true, ReportDiagnostic.Error)]
        [InlineData("false : error", false, ReportDiagnostic.Error)]
        public void TestParseEditorConfigCodeStyleOption(string args, bool isEnabled, ReportDiagnostic severity)
        {
            var notificationOption = NotificationOption.Silent;
            switch (severity)
            {
                case ReportDiagnostic.Hidden:
                    notificationOption = NotificationOption.Silent;
                    break;
                case ReportDiagnostic.Info:
                    notificationOption = NotificationOption.Suggestion;
                    break;
                case ReportDiagnostic.Warn:
                    notificationOption = NotificationOption.Warning;
                    break;
                case ReportDiagnostic.Error:
                    notificationOption = NotificationOption.Error;
                    break;
            }

            var codeStyleOption = new CodeStyleOption<bool>(value: isEnabled, notification: notificationOption);

            CodeStyleHelpers.TryParseBoolEditorConfigCodeStyleOption(args, out var result);
            Assert.True(result.Value == isEnabled,
                        $"Expected {nameof(isEnabled)} to be {isEnabled}, was {result.Value}");
            Assert.True(result.Notification.Severity == severity,
                        $"Expected {nameof(severity)} to be {severity}, was {result.Notification.Severity}");
        }

        [Theory]
        [InlineData("never:none", (int)AccessibilityModifiersRequired.Never, ReportDiagnostic.Suppress)]
        [InlineData("always:suggestion", (int)AccessibilityModifiersRequired.Always, ReportDiagnostic.Info)]
        [InlineData("for_non_interface_members:warning", (int)AccessibilityModifiersRequired.ForNonInterfaceMembers, ReportDiagnostic.Warn)]
        [InlineData("omit_if_default:error", (int)AccessibilityModifiersRequired.OmitIfDefault, ReportDiagnostic.Error)]

        [WorkItem(27685, "https://github.com/dotnet/roslyn/issues/27685")]
        [InlineData("never : none", (int)AccessibilityModifiersRequired.Never, ReportDiagnostic.Suppress)]
        [InlineData("always : suggestion", (int)AccessibilityModifiersRequired.Always, ReportDiagnostic.Info)]
        [InlineData("for_non_interface_members : warning", (int)AccessibilityModifiersRequired.ForNonInterfaceMembers, ReportDiagnostic.Warn)]
        [InlineData("omit_if_default : error", (int)AccessibilityModifiersRequired.OmitIfDefault, ReportDiagnostic.Error)]
        public void TestParseEditorConfigAccessibilityModifiers(string args, int value, ReportDiagnostic severity)
        {
            var storageLocation = CodeStyleOptions.RequireAccessibilityModifiers.StorageLocations
                .OfType<EditorConfigStorageLocation<CodeStyleOption<AccessibilityModifiersRequired>>>()
                .Single();
            var allRawConventions = new Dictionary<string, string> { { storageLocation.KeyName, args } };

            Assert.True(storageLocation.TryGetOption(allRawConventions, typeof(CodeStyleOption<AccessibilityModifiersRequired>), out var parsedCodeStyleOption));
            var codeStyleOption = (CodeStyleOption<AccessibilityModifiersRequired>)parsedCodeStyleOption;
            Assert.Equal((AccessibilityModifiersRequired)value, codeStyleOption.Value);
            Assert.Equal(severity, codeStyleOption.Notification.Severity);
        }

        [Theory]
        [InlineData("lf", "\n")]
        [InlineData("cr", "\r")]
        [InlineData("crlf", "\r\n")]

        [WorkItem(27685, "https://github.com/dotnet/roslyn/issues/27685")]
        [InlineData(" lf ", "\n")]
        [InlineData(" cr ", "\r")]
        [InlineData(" crlf ", "\r\n")]
        public void TestParseEditorConfigEndOfLine(string configurationString, string newLine)
        {
            var storageLocation = FormattingOptions.NewLine.StorageLocations
                .OfType<EditorConfigStorageLocation<string>>()
                .Single();
            var allRawConventions = new Dictionary<string, string> { { storageLocation.KeyName, configurationString } };

            Assert.True(storageLocation.TryGetOption(allRawConventions, typeof(string), out var parsedNewLine));
            Assert.Equal(newLine, (string)parsedNewLine);
        }
    }
}
