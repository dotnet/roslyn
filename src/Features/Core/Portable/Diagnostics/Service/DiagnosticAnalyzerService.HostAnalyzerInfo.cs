// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal sealed partial class DiagnosticAnalyzerService
{
    private sealed class HostAnalyzerInfo
    {
        private const int BuiltInCompilerPriority = -2;
        private const int RegularDiagnosticAnalyzerPriority = -1;

        private readonly ImmutableHashSet<DiagnosticAnalyzer> _hostAnalyzers;
        private readonly ImmutableHashSet<DiagnosticAnalyzer> _allAnalyzers;
        public readonly ImmutableArray<DiagnosticAnalyzer> OrderedAllAnalyzers;

        public HostAnalyzerInfo(
            ImmutableHashSet<DiagnosticAnalyzer> hostAnalyzers,
            ImmutableHashSet<DiagnosticAnalyzer> allAnalyzers)
        {
            _hostAnalyzers = hostAnalyzers;
            _allAnalyzers = allAnalyzers;

            // order analyzers.
            // order will be in this order
            // BuiltIn Compiler Analyzer (C#/VB) < Regular DiagnosticAnalyzers < Document/ProjectDiagnosticAnalyzers
            OrderedAllAnalyzers = [.. _allAnalyzers.OrderBy(PriorityComparison)];
        }

        public bool IsHostAnalyzer(DiagnosticAnalyzer analyzer)
            => _hostAnalyzers.Contains(analyzer);

        public HostAnalyzerInfo WithExcludedAnalyzers(ImmutableHashSet<DiagnosticAnalyzer> excludedAnalyzers)
        {
            if (excludedAnalyzers.IsEmpty)
            {
                return this;
            }

            return new(_hostAnalyzers, _allAnalyzers.Except(excludedAnalyzers));
        }

        private int PriorityComparison(DiagnosticAnalyzer state1, DiagnosticAnalyzer state2)
            => GetPriority(state1) - GetPriority(state2);

        private static int GetPriority(DiagnosticAnalyzer state)
        {
            // compiler gets highest priority
            if (state.IsCompilerAnalyzer())
            {
                return BuiltInCompilerPriority;
            }

            return state switch
            {
                DocumentDiagnosticAnalyzer analyzer => analyzer.Priority,
                _ => RegularDiagnosticAnalyzerPriority,
            };
        }
    }
}
