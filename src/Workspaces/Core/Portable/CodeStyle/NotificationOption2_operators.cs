// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    internal partial class NotificationOption2
    {
        public static explicit operator NotificationOption2?(NotificationOption? notificationOption)
        {
            if (notificationOption is null)
            {
                return null;
            }

            return notificationOption.Severity switch
            {
                ReportDiagnostic.Suppress => None,
                ReportDiagnostic.Hidden => Silent,
                ReportDiagnostic.Info => Suggestion,
                ReportDiagnostic.Warn => Warning,
                ReportDiagnostic.Error => Error,
                _ => throw ExceptionUtilities.UnexpectedValue(notificationOption.Severity),
            };
        }

        public static explicit operator NotificationOption?(NotificationOption2? notificationOption)
        {
            if (notificationOption is null)
            {
                return null;
            }

            return notificationOption.Severity switch
            {
                ReportDiagnostic.Suppress => NotificationOption.None,
                ReportDiagnostic.Hidden => NotificationOption.Silent,
                ReportDiagnostic.Info => NotificationOption.Suggestion,
                ReportDiagnostic.Warn => NotificationOption.Warning,
                ReportDiagnostic.Error => NotificationOption.Error,
                _ => throw ExceptionUtilities.UnexpectedValue(notificationOption.Severity),
            };
        }

        public override bool Equals(object? obj)
        {
            if (obj is NotificationOption2 notificationOption2)
            {
                return this.Equals(notificationOption2);
            }

            return false;
        }

        public bool Equals(NotificationOption2? notificationOption2)
        {
            return ReferenceEquals(this, notificationOption2);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(Name.GetHashCode(), Severity.GetHashCode());
        }
    }
}
