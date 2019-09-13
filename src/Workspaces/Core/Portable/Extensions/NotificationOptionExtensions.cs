// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    internal static class NotificationOptionExtensions
    {
        public static string ToEditorConfigString(this NotificationOption notificationOption)
        {
            return notificationOption.Severity switch
            {
                ReportDiagnostic.Suppress => EditorConfigSeverityStrings.None,
                ReportDiagnostic.Hidden => EditorConfigSeverityStrings.Silent,
                ReportDiagnostic.Info => EditorConfigSeverityStrings.Suggestion,
                ReportDiagnostic.Warn => EditorConfigSeverityStrings.Warning,
                ReportDiagnostic.Error => EditorConfigSeverityStrings.Error,
                _ => throw ExceptionUtilities.UnexpectedValue(notificationOption.Severity)
            };
        }
    }
}
