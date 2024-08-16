// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeStyle;

/// <summary>
/// Offers different notification styles for enforcing
/// a code style. Under the hood, it simply maps to <see cref="DiagnosticSeverity"/>
/// </summary>
/// <completionlist cref="NotificationOption2"/>
[DataContract]
internal readonly partial record struct NotificationOption2(
    [property: DataMember(Order = 0)] ReportDiagnostic Severity, [property: DataMember(Order = 1)] bool IsExplicitlySpecified)
{
    /// <summary>
    /// Notification option to disable or suppress an option with <see cref="ReportDiagnostic.Suppress"/>.
    /// </summary>
    public static NotificationOption2 None => new(ReportDiagnostic.Suppress, false);

    /// <summary>
    /// Notification option for a silent or hidden option with <see cref="ReportDiagnostic.Hidden"/>.
    /// </summary>
    public static NotificationOption2 Silent => new(ReportDiagnostic.Hidden, false);

    /// <summary>
    /// Notification option for a suggestion or an info option with <see cref="ReportDiagnostic.Info"/>.
    /// </summary>
    public static NotificationOption2 Suggestion => new(ReportDiagnostic.Info, false);

    /// <summary>
    /// Notification option for a warning option with <see cref="ReportDiagnostic.Warn"/>.
    /// </summary>
    public static NotificationOption2 Warning => new(ReportDiagnostic.Warn, false);

    /// <summary>
    /// Notification option for an error option with <see cref="ReportDiagnostic.Error"/>.
    /// </summary>
    public static NotificationOption2 Error => new(ReportDiagnostic.Error, false);

    public NotificationOption2 WithIsExplicitlySpecified(bool isExplicitlySpecified)
        => new(Severity, isExplicitlySpecified);

    public static NotificationOption2 ForSeverity(ReportDiagnostic reportDiagnostic)
        => reportDiagnostic switch
        {
            ReportDiagnostic.Error => Error,
            ReportDiagnostic.Warn => Warning,
            ReportDiagnostic.Info => Suggestion,
            ReportDiagnostic.Hidden or ReportDiagnostic.Default => Silent,
            ReportDiagnostic.Suppress => None,
            _ => throw ExceptionUtilities.UnexpectedValue(reportDiagnostic)
        };

    public static NotificationOption2 ForSeverity(DiagnosticSeverity severity)
        => severity switch
        {
            DiagnosticSeverity.Error => Error,
            DiagnosticSeverity.Warning => Warning,
            DiagnosticSeverity.Info => Suggestion,
            DiagnosticSeverity.Hidden => Silent,
            _ => throw ExceptionUtilities.UnexpectedValue(severity)
        };
}
