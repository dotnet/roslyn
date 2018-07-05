// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.CodeStyle
{
    public class EditorConfigCodeStyleParserTests
    {
        [Theory]
        [InlineData("true:none", true, ReportDiagnostic.Suppress)]
        [InlineData("true:silent", true, ReportDiagnostic.Hidden)]
        [InlineData("true:suggestion", true, ReportDiagnostic.Info)]
        [InlineData("true:warning", true, ReportDiagnostic.Warn)]
        [InlineData("true:error", true, ReportDiagnostic.Error)]
        [InlineData("true", false, ReportDiagnostic.Hidden)]
        [InlineData("false:none", false, ReportDiagnostic.Suppress)]
        [InlineData("false:silent", false, ReportDiagnostic.Hidden)]
        [InlineData("false:suggestion", false, ReportDiagnostic.Info)]
        [InlineData("false:warning", false, ReportDiagnostic.Warn)]
        [InlineData("false:error", false, ReportDiagnostic.Error)]
        [InlineData("false", false, ReportDiagnostic.Hidden)]
        [InlineData("*", false, ReportDiagnostic.Hidden)]
        [InlineData("false:false", false, ReportDiagnostic.Hidden)]
        static void TestParseEditorConfigCodeStyleOption(string args, bool isEnabled, ReportDiagnostic severity)
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
    }
}
