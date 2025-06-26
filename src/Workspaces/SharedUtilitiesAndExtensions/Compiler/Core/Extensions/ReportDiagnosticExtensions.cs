// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.CodeStyle;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal static partial class ReportDiagnosticExtensions
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
