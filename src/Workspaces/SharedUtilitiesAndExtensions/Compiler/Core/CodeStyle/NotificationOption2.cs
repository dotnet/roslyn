// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        /// Notification option to disable or suppress an option with <see cref="ReportDiagnostic.Suppress"/>.
        /// </summary>
        public static readonly NotificationOption2 None = new(ReportDiagnostic.Suppress);

        /// <summary>
        /// Notification option for a silent or hidden option with <see cref="ReportDiagnostic.Hidden"/>.
        /// </summary>
        public static readonly NotificationOption2 Silent = new(ReportDiagnostic.Hidden);

        /// <summary>
        /// Notification option for a suggestion or an info option with <see cref="ReportDiagnostic.Info"/>.
        /// </summary>
        public static readonly NotificationOption2 Suggestion = new(ReportDiagnostic.Info);

        /// <summary>
        /// Notification option for a warning option with <see cref="ReportDiagnostic.Warn"/>.
        /// </summary>
        public static readonly NotificationOption2 Warning = new(ReportDiagnostic.Warn);

        /// <summary>
        /// Notification option for an error option with <see cref="ReportDiagnostic.Error"/>.
        /// </summary>
        public static readonly NotificationOption2 Error = new(ReportDiagnostic.Error);

        /// <summary>
        /// Diagnostic severity associated with notification option.
        /// </summary>
        public ReportDiagnostic Severity { get; }

        private NotificationOption2(ReportDiagnostic severity)
        {
            Severity = severity;
        }

        public override bool Equals(object? obj)
            => obj is NotificationOption2 other && Equals(other);

        public bool Equals(NotificationOption2? other)
            => other != null && Severity == other.Severity;

        public override int GetHashCode()
            => Severity.GetHashCode();
    }
}
