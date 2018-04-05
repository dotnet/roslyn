using Microsoft.CodeAnalysis.CodeStyle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.CodeStyle
{
    public class EditorConfigCodeStyleParserTests
    {
        [Theory]
        [InlineData("true:none", true, DiagnosticSeverity.Hidden)]
        [InlineData("true:silent", true, DiagnosticSeverity.Hidden)]
        [InlineData("true:suggestion", true, DiagnosticSeverity.Info)]
        [InlineData("true:warning", true, DiagnosticSeverity.Warning)]
        [InlineData("true:error", true, DiagnosticSeverity.Error)]
        [InlineData("true", false, DiagnosticSeverity.Hidden)]
        [InlineData("false:none", false, DiagnosticSeverity.Hidden)]
        [InlineData("false:silent", false, DiagnosticSeverity.Hidden)]
        [InlineData("false:suggestion", false, DiagnosticSeverity.Info)]
        [InlineData("false:warning", false, DiagnosticSeverity.Warning)]
        [InlineData("false:error", false, DiagnosticSeverity.Error)]
        [InlineData("false", false, DiagnosticSeverity.Hidden)]
        [InlineData("*", false, DiagnosticSeverity.Hidden)]
        [InlineData("false:false", false, DiagnosticSeverity.Hidden)]
        static void TestParseEditorConfigCodeStyleOption(string args, bool isEnabled, DiagnosticSeverity severity)
        {
            var notificationOption = NotificationOption.None;
            switch (severity)
            {
                case DiagnosticSeverity.Hidden:
                    notificationOption = NotificationOption.None;
                    break;
                case DiagnosticSeverity.Info:
                    notificationOption = NotificationOption.Suggestion;
                    break;
                case DiagnosticSeverity.Warning:
                    notificationOption = NotificationOption.Warning;
                    break;
                case DiagnosticSeverity.Error:
                    notificationOption = NotificationOption.Error;
                    break;
            }

            var codeStyleOption = new CodeStyleOption<bool>(value: isEnabled, notification: notificationOption);

            CodeStyleHelpers.TryParseBoolEditorConfigCodeStyleOption(args, out var result);
            Assert.True(result.Value == isEnabled,
                        $"Expected {nameof(isEnabled)} to be {isEnabled}, was {result.Value}");
            Assert.True(result.Notification.Value == severity,
                        $"Expected {nameof(severity)} to be {severity}, was {result.Notification.Value}");
        }
    }
}
