// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal static class ReportDiagnosticExtensions
    {
        public static string ToAnalyzerConfigString(this ReportDiagnostic reportDiagnostic)
        {
            return reportDiagnostic switch
            {
                ReportDiagnostic.Error => "error",
                ReportDiagnostic.Warn => "warning",
                ReportDiagnostic.Info => "suggestion",
                ReportDiagnostic.Hidden => "silent",
                ReportDiagnostic.Suppress => "none",
                _ => throw ExceptionUtilities.Unreachable,
            };
        }
    }
}
