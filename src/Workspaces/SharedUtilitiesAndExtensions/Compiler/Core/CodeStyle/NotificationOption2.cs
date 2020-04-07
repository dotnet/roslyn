// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;

#if CODE_STYLE
using WorkspacesResources = Microsoft.CodeAnalysis.CodeStyleResources;
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
    internal sealed partial class NotificationOption2 : IEquatable<NotificationOption2?>
    {
        /// <summary>
        /// Name for the notification option.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Diagnostic severity associated with notification option.
        /// </summary>
        public ReportDiagnostic Severity
        {
            get;
            set;
        }

        /// <summary>
        /// Notification option to disable or suppress an option with <see cref="ReportDiagnostic.Suppress"/>.
        /// </summary>
        public static readonly NotificationOption2 None = new NotificationOption2(WorkspacesResources.None, ReportDiagnostic.Suppress);

        /// <summary>
        /// Notification option for a silent or hidden option with <see cref="ReportDiagnostic.Hidden"/>.
        /// </summary>
        public static readonly NotificationOption2 Silent = new NotificationOption2(WorkspacesResources.Refactoring_Only, ReportDiagnostic.Hidden);

        /// <summary>
        /// Notification option for a suggestion or an info option with <see cref="ReportDiagnostic.Info"/>.
        /// </summary>
        public static readonly NotificationOption2 Suggestion = new NotificationOption2(WorkspacesResources.Suggestion, ReportDiagnostic.Info);

        /// <summary>
        /// Notification option for a warning option with <see cref="ReportDiagnostic.Warn"/>.
        /// </summary>
        public static readonly NotificationOption2 Warning = new NotificationOption2(WorkspacesResources.Warning, ReportDiagnostic.Warn);

        /// <summary>
        /// Notification option for an error option with <see cref="ReportDiagnostic.Error"/>.
        /// </summary>
        public static readonly NotificationOption2 Error = new NotificationOption2(WorkspacesResources.Error, ReportDiagnostic.Error);

        private NotificationOption2(string name, ReportDiagnostic severity)
        {
            Name = name;
            Severity = severity;
        }

        public override string ToString() => Name;

        public override bool Equals(object? obj)
            => ReferenceEquals(this, obj);

        public bool Equals(NotificationOption2? notificationOption2)
            => ReferenceEquals(this, notificationOption2);

        public override int GetHashCode()
        {
            var hash = this.Name.GetHashCode();
            hash = unchecked((hash * (int)0xA5555529) + this.Severity.GetHashCode());
            return hash;
        }
    }
}
