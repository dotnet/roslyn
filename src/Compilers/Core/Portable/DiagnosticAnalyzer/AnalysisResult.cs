// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
            ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>> nonLocalDiagnostics,
            ImmutableDictionary<DiagnosticAnalyzer, AnalyzerTelemetryInfo> analyzerTelemetryInfo)
        {
            Analyzers = analyzers;
            SyntaxDiagnostics = localSyntaxDiagnostics;
            SemanticDiagnostics = localSemanticDiagnostics;
            CompilationDiagnostics = nonLocalDiagnostics;
            AnalyzerTelemetryInfo = analyzerTelemetryInfo;
        }

        /// <summary>
        /// Analyzers corresponding to this analysis result.
        /// </summary>
        public ImmutableArray<DiagnosticAnalyzer> Analyzers { get; private set; }

        /// <summary>
        /// Syntax diagnostics reported by the <see cref="Analyzers"/>.
        /// </summary>
        public ImmutableDictionary<SyntaxTree, ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>>> SyntaxDiagnostics { get; private set; }

        /// <summary>
        /// Semantic diagnostics reported by the <see cref="Analyzers"/>.
        /// </summary>
        public ImmutableDictionary<SyntaxTree, ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>>> SemanticDiagnostics { get; private set; }

        /// <summary>
        /// Compilation diagnostics reported by the <see cref="Analyzers"/>.
        /// </summary>
        public ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>> CompilationDiagnostics { get; private set; }

        /// <summary>
        /// Analyzer telemetry info (register action counts and execution times).
        /// </summary>
        public ImmutableDictionary<DiagnosticAnalyzer, AnalyzerTelemetryInfo> AnalyzerTelemetryInfo { get; private set; }

        /// <summary>
        /// Gets all the diagnostics reported by the given <paramref name="analyzer"/>.
        /// </summary>
        public ImmutableArray<Diagnostic> GetAllDiagnostics(DiagnosticAnalyzer analyzer)
        {
            if (!Analyzers.Contains(analyzer))
            {
                throw new ArgumentException(CodeAnalysisResources.UnsupportedAnalyzerInstance, nameof(analyzer));
            }

            return GetDiagnostics(SpecializedCollections.SingletonEnumerable(analyzer), getLocalSyntaxDiagnostics: true, getLocalSemanticDiagnostics: true, getNonLocalDiagnostics: true);
        }

        /// <summary>
        /// Gets all the diagnostics reported by all the <see cref="Analyzers"/>.
        /// </summary>
        public ImmutableArray<Diagnostic> GetAllDiagnostics()
        {
            return GetDiagnostics(Analyzers, getLocalSyntaxDiagnostics: true, getLocalSemanticDiagnostics: true, getNonLocalDiagnostics: true);
        }

        internal ImmutableArray<Diagnostic> GetSyntaxDiagnostics(ImmutableArray<DiagnosticAnalyzer> analyzers)
        {
            return GetDiagnostics(analyzers, getLocalSyntaxDiagnostics: true, getLocalSemanticDiagnostics: false, getNonLocalDiagnostics: false);
        }

        internal ImmutableArray<Diagnostic> GetSemanticDiagnostics(ImmutableArray<DiagnosticAnalyzer> analyzers)
        {
            return GetDiagnostics(analyzers, getLocalSyntaxDiagnostics: false, getLocalSemanticDiagnostics: true, getNonLocalDiagnostics: false);
        }

        internal ImmutableArray<Diagnostic> GetDiagnostics(IEnumerable<DiagnosticAnalyzer> analyzers, bool getLocalSyntaxDiagnostics, bool getLocalSemanticDiagnostics, bool getNonLocalDiagnostics)
        {
            var excludedAnalyzers = Analyzers.Except(analyzers);
            var excludedAnalyzersSet = excludedAnalyzers.Any() ? excludedAnalyzers.ToImmutableHashSet() : ImmutableHashSet<DiagnosticAnalyzer>.Empty;
            return GetDiagnostics(excludedAnalyzersSet, getLocalSyntaxDiagnostics, getLocalSemanticDiagnostics, getNonLocalDiagnostics);
        }

        private ImmutableArray<Diagnostic> GetDiagnostics(ImmutableHashSet<DiagnosticAnalyzer> excludedAnalyzers, bool getLocalSyntaxDiagnostics, bool getLocalSemanticDiagnostics, bool getNonLocalDiagnostics)
        {
            if (SyntaxDiagnostics.Count > 0 || SemanticDiagnostics.Count > 0 || CompilationDiagnostics.Count > 0)
            {
                var builder = ImmutableArray.CreateBuilder<Diagnostic>();
                if (getLocalSyntaxDiagnostics)
                {
                    AddLocalDiagnostics(SyntaxDiagnostics, excludedAnalyzers, builder);
                }

                if (getLocalSemanticDiagnostics)
                {
                    AddLocalDiagnostics(SemanticDiagnostics, excludedAnalyzers, builder);
                }

                if (getNonLocalDiagnostics)
                {
                    AddNonLocalDiagnostics(CompilationDiagnostics, excludedAnalyzers, builder);
                }

                return builder.ToImmutable();
            }

            return ImmutableArray<Diagnostic>.Empty;
        }

        private static void AddLocalDiagnostics(
            ImmutableDictionary<SyntaxTree, ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>>> localDiagnostics,
            ImmutableHashSet<DiagnosticAnalyzer> excludedAnalyzers,
            ImmutableArray<Diagnostic>.Builder builder)
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
