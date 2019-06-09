// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Diagnostics;

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
    /// <completionlist cref="NotificationOption"/>
    public class NotificationOption
    {
        public string Name { get; set; }

        public ReportDiagnostic Severity
        {
            get;
            set;
        }

        [Obsolete("Use " + nameof(Severity) + " instead.")]
        public DiagnosticSeverity Value
        {
            get => Severity.ToDiagnosticSeverity() ?? DiagnosticSeverity.Hidden;
            set => Severity = value.ToReportDiagnostic();
        }

        public static readonly NotificationOption None = new NotificationOption(WorkspacesResources.None, ReportDiagnostic.Suppress);
        public static readonly NotificationOption Silent = new NotificationOption(WorkspacesResources.Refactoring_Only, ReportDiagnostic.Hidden);
        public static readonly NotificationOption Suggestion = new NotificationOption(WorkspacesResources.Suggestion, ReportDiagnostic.Info);
        public static readonly NotificationOption Warning = new NotificationOption(WorkspacesResources.Warning, ReportDiagnostic.Warn);
        public static readonly NotificationOption Error = new NotificationOption(WorkspacesResources.Error, ReportDiagnostic.Error);

        private NotificationOption(string name, ReportDiagnostic severity)
        {
            Name = name;
            Severity = severity;
        }

        public override string ToString() => Name;
    }
}
