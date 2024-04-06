// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.CodeStyle;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal static partial class ReportDiagnosticExtensions
{
    /// <summary>
    /// Returns the equivalent <see cref="DiagnosticSeverity"/> for a <see cref="ReportDiagnostic"/> value.
    /// </summary>
    /// <param name="reportDiagnostic">The <see cref="ReportDiagnostic"/> value.</param>
    /// <returns>
    /// The equivalent <see cref="DiagnosticSeverity"/> for a <see cref="ReportDiagnostic"/> value; otherwise,
    /// <see langword="null"/> if <see cref="DiagnosticSeverity"/> does not contain a direct equivalent for
    /// <paramref name="reportDiagnostic"/>.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// If <paramref name="reportDiagnostic"/> is not one of the expected values.
    /// </exception>
    public static DiagnosticSeverity? ToDiagnosticSeverity(this ReportDiagnostic reportDiagnostic)
    {
        switch (reportDiagnostic)
        {
            case ReportDiagnostic.Error:
                return DiagnosticSeverity.Error;

            case ReportDiagnostic.Warn:
                return DiagnosticSeverity.Warning;

            case ReportDiagnostic.Info:
                return DiagnosticSeverity.Info;

            case ReportDiagnostic.Hidden:
                return DiagnosticSeverity.Hidden;

            case ReportDiagnostic.Suppress:
            case ReportDiagnostic.Default:
                return null;

            default:
                throw ExceptionUtilities.UnexpectedValue(reportDiagnostic);
        }
    }

    /// <summary>
    /// Applies a default severity to a <see cref="ReportDiagnostic"/> value.
    /// </summary>
    /// <param name="reportDiagnostic">The <see cref="ReportDiagnostic"/> value.</param>
    /// <param name="defaultSeverity">The default severity.</param>
    /// <returns>
    /// <para>If <paramref name="reportDiagnostic"/> is <see cref="ReportDiagnostic.Default"/>, returns
    /// <paramref name="defaultSeverity"/>.</para>
    /// <para>-or-</para>
    /// <para>Otherwise, returns <paramref name="reportDiagnostic"/> if it has a non-default value.</para>
    /// </returns>
    public static ReportDiagnostic WithDefaultSeverity(this ReportDiagnostic reportDiagnostic, DiagnosticSeverity defaultSeverity)
    {
        if (reportDiagnostic != ReportDiagnostic.Default)
        {
            return reportDiagnostic;
        }

        return defaultSeverity.ToReportDiagnostic();
    }

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

    public static NotificationOption2 ToNotificationOption(this ReportDiagnostic reportDiagnostic, DiagnosticSeverity defaultSeverity)
    {
        var isNonDefault = reportDiagnostic != ReportDiagnostic.Default;
        var notificationOption = reportDiagnostic.WithDefaultSeverity(defaultSeverity) switch
        {
            ReportDiagnostic.Error => NotificationOption2.Error,
            ReportDiagnostic.Warn => NotificationOption2.Warning,
            ReportDiagnostic.Info => NotificationOption2.Suggestion,
            ReportDiagnostic.Hidden => NotificationOption2.Silent,
            ReportDiagnostic.Suppress => NotificationOption2.None,
            _ => throw ExceptionUtilities.UnexpectedValue(reportDiagnostic),
        };

        return notificationOption.WithIsExplicitlySpecified(isNonDefault);
    }
}
