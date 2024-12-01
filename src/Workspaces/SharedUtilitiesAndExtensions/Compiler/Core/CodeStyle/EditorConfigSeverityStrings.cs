// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis;

internal static class EditorConfigSeverityStrings
{
    public const string None = "none";
    public const string Refactoring = "refactoring";
    public const string Silent = "silent";
    public const string Suggestion = "suggestion";
    public const string Warning = "warning";
    public const string Error = "error";

    public static bool TryParse(string editorconfigSeverityString, out ReportDiagnostic reportDiagnostic)
    {
        switch (editorconfigSeverityString)
        {
            case None:
                reportDiagnostic = ReportDiagnostic.Suppress;
                return true;

            case Refactoring:
            case Silent:
                reportDiagnostic = ReportDiagnostic.Hidden;
                return true;

            case Suggestion:
                reportDiagnostic = ReportDiagnostic.Info;
                return true;

            case Warning:
                reportDiagnostic = ReportDiagnostic.Warn;
                return true;

            case Error:
                reportDiagnostic = ReportDiagnostic.Error;
                return true;

            default:
                reportDiagnostic = default;
                return false;
        }
    }
}
