// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Diagnostics.Telemetry;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Stores the results of analyzer execution:
    /// 1. Local and non-local diagnostics, per-analyzer.
    /// 2. Analyzer execution times, if requested.
    /// </summary>
    public class AnalysisResult
    {
        internal AnalysisResult(
            ImmutableArray<DiagnosticAnalyzer> analyzers,
            ImmutableDictionary<SyntaxTree, ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>>> localSyntaxDiagnostics,
            ImmutableDictionary<SyntaxTree, ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>>> localSemanticDiagnostics,
            ImmutableDictionary<AdditionalText, ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>>> localAdditionalFileDiagnostics,
            ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>> nonLocalDiagnostics,
            ImmutableDictionary<DiagnosticAnalyzer, AnalyzerTelemetryInfo> analyzerTelemetryInfo)
        {
            Analyzers = analyzers;
            SyntaxDiagnostics = localSyntaxDiagnostics;
            SemanticDiagnostics = localSemanticDiagnostics;
            AdditionalFileDiagnostics = localAdditionalFileDiagnostics;
            CompilationDiagnostics = nonLocalDiagnostics;
            AnalyzerTelemetryInfo = analyzerTelemetryInfo;
        }

        /// <summary>
        /// Analyzers corresponding to this analysis result.
        /// </summary>
        public ImmutableArray<DiagnosticAnalyzer> Analyzers { get; }

        /// <summary>
        /// Syntax diagnostics reported by the <see cref="Analyzers"/>.
        /// </summary>
        public ImmutableDictionary<SyntaxTree, ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>>> SyntaxDiagnostics { get; }

        /// <summary>
        /// Semantic diagnostics reported by the <see cref="Analyzers"/>.
        /// </summary>
        public ImmutableDictionary<SyntaxTree, ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>>> SemanticDiagnostics { get; }

        /// <summary>
        /// Diagnostics in additional files reported by the <see cref="Analyzers"/>.
        /// </summary>
        public ImmutableDictionary<AdditionalText, ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>>> AdditionalFileDiagnostics { get; }

        /// <summary>
        /// Compilation diagnostics reported by the <see cref="Analyzers"/>.
        /// </summary>
        public ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>> CompilationDiagnostics { get; }

        /// <summary>
        /// Analyzer telemetry info (register action counts and execution times).
        /// </summary>
        public ImmutableDictionary<DiagnosticAnalyzer, AnalyzerTelemetryInfo> AnalyzerTelemetryInfo { get; }

        /// <summary>
        /// Gets all the diagnostics reported by the given <paramref name="analyzer"/>.
        /// </summary>
        public ImmutableArray<Diagnostic> GetAllDiagnostics(DiagnosticAnalyzer analyzer)
        {
            if (!Analyzers.Contains(analyzer))
            {
                throw new ArgumentException(CodeAnalysisResources.UnsupportedAnalyzerInstance, nameof(analyzer));
            }

            return GetDiagnostics(SpecializedCollections.SingletonEnumerable(analyzer));
        }

        /// <summary>
        /// Gets all the diagnostics reported by all the <see cref="Analyzers"/>.
        /// </summary>
        public ImmutableArray<Diagnostic> GetAllDiagnostics()
        {
            return GetDiagnostics(Analyzers);
        }

        private ImmutableArray<Diagnostic> GetDiagnostics(IEnumerable<DiagnosticAnalyzer> analyzers)
        {
            var excludedAnalyzers = Analyzers.Except(analyzers);
            var excludedAnalyzersSet = excludedAnalyzers.Any() ? excludedAnalyzers.ToImmutableHashSet() : ImmutableHashSet<DiagnosticAnalyzer>.Empty;
            return GetDiagnostics(excludedAnalyzersSet);
        }

        private ImmutableArray<Diagnostic> GetDiagnostics(ImmutableHashSet<DiagnosticAnalyzer> excludedAnalyzers)
        {
            if (SyntaxDiagnostics.Count > 0 || SemanticDiagnostics.Count > 0 || AdditionalFileDiagnostics.Count > 0 || CompilationDiagnostics.Count > 0)
            {
                var builder = ImmutableArray.CreateBuilder<Diagnostic>();
                AddLocalDiagnostics(SyntaxDiagnostics, excludedAnalyzers, builder);
                AddLocalDiagnostics(SemanticDiagnostics, excludedAnalyzers, builder);
                AddLocalDiagnostics(AdditionalFileDiagnostics, excludedAnalyzers, builder);
                AddNonLocalDiagnostics(CompilationDiagnostics, excludedAnalyzers, builder);

                return builder.ToImmutable();
            }

            return ImmutableArray<Diagnostic>.Empty;
        }

        private static void AddLocalDiagnostics<T>(
            ImmutableDictionary<T, ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>>> localDiagnostics,
            ImmutableHashSet<DiagnosticAnalyzer> excludedAnalyzers,
            ImmutableArray<Diagnostic>.Builder builder)
            where T : notnull
        {
            foreach (var diagnosticsByTree in localDiagnostics)
            {
                foreach (var diagnosticsByAnalyzer in diagnosticsByTree.Value)
                {
                    if (excludedAnalyzers.Contains(diagnosticsByAnalyzer.Key))
                    {
                        continue;
                    }

                    builder.AddRange(diagnosticsByAnalyzer.Value);
                }
            }
        }

        private static void AddNonLocalDiagnostics(
            ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>> nonLocalDiagnostics,
            ImmutableHashSet<DiagnosticAnalyzer> excludedAnalyzers,
            ImmutableArray<Diagnostic>.Builder builder)
        {
            foreach (var diagnosticsByAnalyzer in nonLocalDiagnostics)
            {
                if (excludedAnalyzers.Contains(diagnosticsByAnalyzer.Key))
                {
                    continue;
                }

                builder.AddRange(diagnosticsByAnalyzer.Value);
            }
        }
    }
}
