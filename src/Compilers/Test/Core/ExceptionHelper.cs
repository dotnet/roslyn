// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace Roslyn.Test.Utilities
{
    internal static class ExceptionHelper
    {
        internal static string GetMessageFromResult(IEnumerable<Diagnostic> diagnostics, string directory)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Emit Failed, binaries saved to: ");
            sb.AppendLine(directory);
            foreach (var d in diagnostics)
            {
                sb.AppendLine(d.ToString());
            }
            return sb.ToString();
        }

        internal static string GetMessageFromResult(string output, string exePath, bool isIlVerify = false) =>
            $"""
                {(isIlVerify ? "ILVerify" : "PEVerify")} failed for assembly '{exePath}'
                {output}
                """;

        internal static string GetMessageFromException(Exception executionException, string exePath) =>
            $"""
                Execution failed for assembly '{exePath}'
                Exception: {executionException.Message}
                """;

        internal static string GetMessageFromResult(string expectedOutput, string actualOutput, string exePath) =>
            $"""
                Execution failed for assembly '{exePath}'
                Output differed from expected
                Diff:
                {DiffUtil.DiffReport(expectedOutput, actualOutput)}
                Expected:
                {expectedOutput}
                Actual:
                {actualOutput}
                """;
    }
}
