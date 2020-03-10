// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

#if CODE_STYLE
using WorkspacesResources = Microsoft.CodeAnalysis.CodeStyleResources;
#else
using System.Security;
using Roslyn.Utilities;
#endif

namespace Microsoft.CodeAnalysis.CodeStyle
{
    /// <summary>
    /// Offers different notification styles for enforcing
    /// a code style. Under the hood, it simply maps to <see cref="DiagnosticSeverity"/>
    /// </summary>
    /// <remarks>
    /// This also supports various properties for databinding.
    /// </remarks>
    /// <completionlist cref="NotificationOption2"/>
    internal class NotificationOption2
    {
        public string Name { get; set; }

        public ReportDiagnostic Severity
        {
            get;
            set;
        }

        public static readonly NotificationOption2 None = new NotificationOption2(WorkspacesResources.None, ReportDiagnostic.Suppress);
        public static readonly NotificationOption2 Silent = new NotificationOption2(WorkspacesResources.Refactoring_Only, ReportDiagnostic.Hidden);
        public static readonly NotificationOption2 Suggestion = new NotificationOption2(WorkspacesResources.Suggestion, ReportDiagnostic.Info);
        public static readonly NotificationOption2 Warning = new NotificationOption2(WorkspacesResources.Warning, ReportDiagnostic.Warn);
        public static readonly NotificationOption2 Error = new NotificationOption2(WorkspacesResources.Error, ReportDiagnostic.Error);

        private NotificationOption2(string name, ReportDiagnostic severity)
        {
            Name = name;
            Severity = severity;
        }

        public override string ToString() => Name;

#if !CODE_STYLE
        public static implicit operator NotificationOption2?(NotificationOption? notificationOption)
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

        public static implicit operator NotificationOption?(NotificationOption2? notificationOption)
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
            if (obj is NotificationOption)
            {
                return this.Equals((NotificationOption2)obj);
            }
            else if (obj is NotificationOption2 notificationOption2)
            {
                return this.Equals(notificationOption2);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(Name.GetHashCode(), Severity.GetHashCode());
        }

        public bool Equals(NotificationOption2? notificationOption2)
        {
            return ReferenceEquals(this, notificationOption2);
        }

        public static bool operator ==(NotificationOption? notificationOption, NotificationOption2? notificationOption2)
        {
            return Equals((NotificationOption2?)notificationOption, notificationOption2);
        }

        public static bool operator !=(NotificationOption notificationOption, NotificationOption2 notificationOption2)
        {
            return !Equals((NotificationOption2?)notificationOption, notificationOption2);
        }
#endif
    }
}
