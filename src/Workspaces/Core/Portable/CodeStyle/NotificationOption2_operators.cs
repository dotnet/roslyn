// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    internal sealed partial class NotificationOption2 : IEquatable<NotificationOption2?>
    {
        [return: NotNullIfNotNull("notificationOption")]
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

        [return: NotNullIfNotNull("notificationOption")]
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
            return ReferenceEquals(this, obj);
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
