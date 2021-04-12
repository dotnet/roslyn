﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    /// <inheritdoc cref="NotificationOption2"/>
    public class NotificationOption
    {
        private readonly NotificationOption2 _notificationOptionImpl;

        /// <inheritdoc cref="NotificationOption2.Name"/>
        public string Name
        {
            get => _notificationOptionImpl.Name;
            set => _notificationOptionImpl.Name = value;
        }

        /// <inheritdoc cref="NotificationOption2.Severity"/>
        public ReportDiagnostic Severity
        {
            get => _notificationOptionImpl.Severity;
            set => _notificationOptionImpl.Severity = value;
        }

        [Obsolete("Use " + nameof(Severity) + " instead.")]
        public DiagnosticSeverity Value
        {
            get => Severity.ToDiagnosticSeverity() ?? DiagnosticSeverity.Hidden;
            set => Severity = value.ToReportDiagnostic();
        }

        /// <inheritdoc cref="NotificationOption2.None"/>
        public static readonly NotificationOption None = new(NotificationOption2.None);

        /// <inheritdoc cref="NotificationOption2.Silent"/>
        public static readonly NotificationOption Silent = new(NotificationOption2.Silent);

        /// <inheritdoc cref="NotificationOption2.Suggestion"/>
        public static readonly NotificationOption Suggestion = new(NotificationOption2.Suggestion);

        /// <inheritdoc cref="NotificationOption2.Warning"/>
        public static readonly NotificationOption Warning = new(NotificationOption2.Warning);

        /// <inheritdoc cref="NotificationOption2.Error"/>
        public static readonly NotificationOption Error = new(NotificationOption2.Error);

        private NotificationOption(NotificationOption2 notificationOptionImpl)
            => _notificationOptionImpl = notificationOptionImpl;

        public override string ToString() => _notificationOptionImpl.ToString();
    }
}
