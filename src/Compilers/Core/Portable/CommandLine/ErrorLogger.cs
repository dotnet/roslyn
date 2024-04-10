// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Base class for logging compiler diagnostics.
    /// </summary>
    internal abstract class ErrorLogger
    {
        public abstract void LogDiagnostic(Diagnostic diagnostic, SuppressionInfo? suppressionInfo);
        public abstract void AddAnalyzerDescriptorsAndExecutionTime(ImmutableArray<(DiagnosticDescriptor Descriptor, DiagnosticDescriptorErrorLoggerInfo Info)> descriptors, double totalAnalyzerExecutionTime);
    }

    /// <summary>
    /// Contains information associated with a <see cref="DiagnosticDescriptor"/>
    /// for the <see cref="ErrorLogger"/>. It contains the following:
    ///   1. Analyzer execution time in seconds for the analyzer owning the descriptor.
    ///   2. Analyzer execution time in percentage of the total analyzer execution time.
    ///   3. Set of all effective severities for the diagnostic Id, configured through options
    ///      from editorconfig, ruleset, command line options, etc. for either part of the compilation
    ///      or the entire compilation.
    ///   4. A boolean value "HasAnyExternalSuppression" indicating if the diagnostic ID has any
    ///      external non-source suppression from editorconfig, ruleset, command line options, etc.,
    ///      which disables the descriptor for either part of the compilation or the entire compilation.
    ///      Note that this flag doesn't account for source suppressions from pragma directives,
    ///      SuppressMessageAttributes, DiagnosticSuppressors, etc. which suppress individual instances
    ///      of reported diagnostics.
    /// </summary>
    internal readonly record struct DiagnosticDescriptorErrorLoggerInfo(
        double ExecutionTime,
        int ExecutionPercentage,
        ImmutableHashSet<ReportDiagnostic>? EffectiveSeverities,
        bool HasAnyExternalSuppression);
}
