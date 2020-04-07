// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Information about analyzers which can be completely skipped and analyzers whose subset of reported diagnostic IDs must be filtered.
    /// </summary>
    internal interface ISkippedAnalyzersInfo
    {
        /// <summary>
        /// Analyzers which must be skipped from execution.
        /// </summary>
        ImmutableHashSet<DiagnosticAnalyzer> SkippedAnalyzers { get; }

        /// <summary>
        /// Analyzer to diagnostic ID map, such that the diagnostics of those IDs reported by the analyzer should be filtered.
        /// </summary>
        ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<string>> FilteredDiagnosticIdsForAnalyzers { get; }
    }
}
