// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public static class DiagnosticSeverityExtensions
    {
        public static ReportDiagnostic ToReportDiagnostic(this DiagnosticSeverity diagnosticSeverity)
        {
            switch (diagnosticSeverity)
            {
            case DiagnosticSeverity.Hidden:
                return ReportDiagnostic.Hidden;

            case DiagnosticSeverity.Info:
                return ReportDiagnostic.Info;

            case DiagnosticSeverity.Warning:
                return ReportDiagnostic.Warn;

            case DiagnosticSeverity.Error:
                return ReportDiagnostic.Error;

            default:
                throw ExceptionUtilities.UnexpectedValue(diagnosticSeverity);
            }
        }
    }
}
