// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Provides extension methods for working with <see cref="DiagnosticSeverity"/> values.
    /// </summary>
    public static class DiagnosticSeverityExtensions
    {
        /// <summary>
        /// Returns the equivalent <see cref="ReportDiagnostic"/> for a <see cref="DiagnosticSeverity"/> value.
        /// </summary>
        /// <param name="diagnosticSeverity">The <see cref="DiagnosticSeverity"/> value.</param>
        /// <returns>
        /// The equivalent <see cref="ReportDiagnostic"/> for the <see cref="DiagnosticSeverity"/> value.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// If <paramref name="diagnosticSeverity"/> is not one of the expected values.
        /// </exception>
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
