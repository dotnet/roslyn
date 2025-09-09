// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace Roslyn.Test.Utilities
{
    internal static class ExceptionHelper
    {
        internal static string GetMessageFromResult(IEnumerable<Diagnostic> diagnostics, string? directory)
        {
            StringBuilder sb = new StringBuilder();
            if (directory is not null)
            {
                sb.AppendLine("Emit Failed, binaries saved to: ");
                sb.AppendLine(directory);
            }
            else
            {
                sb.AppendLine();
            }

            DiagnosticDescription.GetPrettyDiagnostics(sb, diagnostics, includeDiagnosticMessagesAsComments: true, indentDepth: 0, includeDefaultSeverity: false, includeEffectiveSeverity: false);

            return sb.ToString();
        }

        internal static string GetMessageFromResult(string output, string exePath, bool isIlVerify = false)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine();
            string tool = isIlVerify ? "ILVerify" : "PEVerify";
            sb.Append($"{tool} failed for assembly '");
            sb.Append(exePath);
            sb.AppendLine("':");
            sb.AppendLine(output);
            return sb.ToString();
        }

        internal static string GetMessageFromException(Exception executionException, string exePath)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine();
            sb.Append("Execution failed for assembly '");
            sb.Append(exePath);
            sb.AppendLine("'.");
            sb.Append("Exception: " + executionException);
            return sb.ToString();
        }

        internal static string GetMessageFromResult(string expectedOutput, string actualOutput, string exePath)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine();
            sb.Append("Execution failed for assembly '");
            sb.Append(exePath);
            sb.AppendLine("'.");
            if (expectedOutput != null)
            {
                sb.Append("Expected: ");
                sb.AppendLine(expectedOutput);
                sb.Append("Actual: ");
                sb.AppendLine(actualOutput);
            }
            else
            {
                sb.Append("Output: ");
                sb.AppendLine(actualOutput);
            }
            return sb.ToString();
        }
    }
}
