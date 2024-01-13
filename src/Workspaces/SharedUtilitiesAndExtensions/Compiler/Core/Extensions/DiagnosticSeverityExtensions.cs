// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeStyle;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal static partial class DiagnosticSeverityExtensions
    {
        public static NotificationOption2 ToNotificationOption(this DiagnosticSeverity severity, bool isOverridenSeverity)
        {
            var notificationOption = severity switch
            {
                DiagnosticSeverity.Error => NotificationOption2.Error,
                DiagnosticSeverity.Warning => NotificationOption2.Warning,
                DiagnosticSeverity.Info => NotificationOption2.Suggestion,
                DiagnosticSeverity.Hidden => NotificationOption2.Silent,
                _ => throw ExceptionUtilities.UnexpectedValue(severity),
            };

            return notificationOption.WithIsExplicitlySpecified(isOverridenSeverity);
        }
    }
}
