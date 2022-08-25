// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    internal readonly partial record struct NotificationOption2
    {
        public static explicit operator NotificationOption(NotificationOption2 notificationOption)
            => notificationOption.Severity switch
            {
                ReportDiagnostic.Suppress => NotificationOption.None,
                ReportDiagnostic.Hidden => NotificationOption.Silent,
                ReportDiagnostic.Info => NotificationOption.Suggestion,
                ReportDiagnostic.Warn => NotificationOption.Warning,
                ReportDiagnostic.Error => NotificationOption.Error,
                _ => throw ExceptionUtilities.UnexpectedValue(notificationOption.Severity),
            };
    }
}
