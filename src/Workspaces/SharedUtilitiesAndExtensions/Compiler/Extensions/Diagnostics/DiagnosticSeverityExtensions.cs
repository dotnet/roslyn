// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal static partial class DiagnosticSeverityExtensions
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
        => diagnosticSeverity switch
        {
            DiagnosticSeverity.Hidden => ReportDiagnostic.Hidden,
            DiagnosticSeverity.Info => ReportDiagnostic.Info,
            DiagnosticSeverity.Warning => ReportDiagnostic.Warn,
            DiagnosticSeverity.Error => ReportDiagnostic.Error,
            _ => throw ExceptionUtilities.UnexpectedValue(diagnosticSeverity),
        };
}
