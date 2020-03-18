// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

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
    internal partial class NotificationOption2
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
    }
}
