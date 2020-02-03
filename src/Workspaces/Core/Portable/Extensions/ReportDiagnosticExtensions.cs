// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal static class ReportDiagnosticExtensions
    {
        public static string ToEditorConfigString(this ReportDiagnostic reportDiagnostic)
        {
            return reportDiagnostic switch
            {
                ReportDiagnostic.Suppress => EditorConfigSeverityStrings.None,
                ReportDiagnostic.Hidden => EditorConfigSeverityStrings.Silent,
                ReportDiagnostic.Info => EditorConfigSeverityStrings.Suggestion,
                ReportDiagnostic.Warn => EditorConfigSeverityStrings.Warning,
                ReportDiagnostic.Error => EditorConfigSeverityStrings.Error,
                _ => throw ExceptionUtilities.UnexpectedValue(reportDiagnostic)
            };
        }
    }
}
