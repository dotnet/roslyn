// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CodeCleanup
{
    /// <summary>
    /// Indicates which code fixes are enabled for a Code Cleanup operation. Each code fix in the set is triggered by
    /// one or more diagnostic IDs, which could be provided by the compiler or an analyzer.
    /// </summary>
    internal sealed class DiagnosticSet
    {
        public string Description { get; }
        public ImmutableArray<string> DiagnosticIds { get; }

        /// <summary>
        /// Diagnostic set is enabled for all severities if it has been explicitly selected as part of the cleanup profile.
        /// If the diagnostic set has not been explicitly selected, but gets bulk included by selecting
        /// "Fix all warnings and errors set in EditorConfig", then we only include diagnostics with Warning Or Error severity.
        /// </summary>
        public bool IsAnyDiagnosticIdExplicitlyEnabled { get; }

        private DiagnosticSet(string description, ImmutableArray<string> diagnosticIds, bool isAnyDiagnosticIdExplicitlyEnabled)
        {
            Description = description;
            DiagnosticIds = diagnosticIds;
            IsAnyDiagnosticIdExplicitlyEnabled = isAnyDiagnosticIdExplicitlyEnabled;
        }

        public DiagnosticSet(string description, params string[] diagnosticIds)
            : this(description, ImmutableArray.Create(diagnosticIds), isAnyDiagnosticIdExplicitlyEnabled: true)
        {
        }

        public DiagnosticSet With(bool isAnyDiagnosticIdExplicitlyEnabled)
        {
            if (this.IsAnyDiagnosticIdExplicitlyEnabled == isAnyDiagnosticIdExplicitlyEnabled)
                return this;

            return new(Description, DiagnosticIds, isAnyDiagnosticIdExplicitlyEnabled);
        }
    }
}
