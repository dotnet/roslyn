// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeActions
{
    internal sealed class CodeActionRequestPriorityProvider
    {
        /// <summary>
        /// Set of analyzers which have been de-prioritized to <see cref="CodeActionRequestPriority.Low"/> bucket.
        /// </summary>
        private readonly HashSet<DiagnosticAnalyzer> _lowPriorityAnalyzers;

        /// <summary>
        /// Default provider with <see cref="CodeActionRequestPriority.None"/> <see cref="Priority"/>.
        /// </summary>
        public static readonly CodeActionRequestPriorityProvider Default = new(CodeActionRequestPriority.None, new());

        public CodeActionRequestPriority Priority { get; }

        private CodeActionRequestPriorityProvider(CodeActionRequestPriority priority, HashSet<DiagnosticAnalyzer> lowPriorityAnalyzers)
        {
            Priority = priority;
            _lowPriorityAnalyzers = lowPriorityAnalyzers;
        }

        public static CodeActionRequestPriorityProvider Create(CodeActionRequestPriority priority)
        {
            if (priority == CodeActionRequestPriority.None)
                return Default;

            return new(priority, new());
        }

        public CodeActionRequestPriorityProvider With(CodeActionRequestPriority priority)
        {
            Debug.Assert(priority != CodeActionRequestPriority.None);

            return new(priority, _lowPriorityAnalyzers);
        }

        /// <summary>
        /// Returns true if the given <paramref name="analyzer"/> can report diagnostics that can have
        /// fixes from a code fix provider with <see cref="CodeFixProvider.RequestPriority"/>
        /// matching <see cref="Priority"/>.
        /// This method is useful for performing a performance optimization for lightbulb diagnostic computation,
        /// wherein we can reduce the set of analyzers to be executed when computing fixes for a specific
        /// <see cref="Priority"/>.
        /// </summary>
        public bool MatchesPriority(DiagnosticAnalyzer analyzer)
        {
            // If caller isn't asking for prioritized result, then run all analyzers.
            if (Priority == CodeActionRequestPriority.None)
                return true;

            // 'CodeActionRequestPriority.Lowest' is used for suppression/configuration fixes,
            // which requires all analyzer diagnostics.
            if (Priority == CodeActionRequestPriority.Lowest)
                return true;

            // The compiler analyzer always counts for any priority.  It's diagnostics may be fixed
            // by high pri or normal pri fixers.
            if (analyzer.IsCompilerAnalyzer())
                return true;

            // Check if we are computing diagnostics for 'CodeActionRequestPriority.Low' and
            // this analyzer was de-prioritized to low priority bucket.
            if (Priority == CodeActionRequestPriority.Low &&
                _lowPriorityAnalyzers.Contains(analyzer))
            {
                return true;
            }

            var analyzerPriority = analyzer is IBuiltInAnalyzer { RequestPriority: var requestPriority }
                ? requestPriority
                : CodeActionRequestPriority.Normal;

            return Priority == analyzerPriority;
        }

        /// <summary>
        /// Returns true if this is an analyzer that is a candidate to be de-prioritized to
        /// <see cref="CodeActionRequestPriority.Low"/> <see cref="Priority"/> for improvement in analyzer
        /// execution performance for priority buckets above 'Low' priority.
        /// </summary>
        /// <remarks>
        /// Based on performance measurements, currently only analyzers which register SymbolStart/End actions
        /// or SemanticModel actions are considered candidates to be de-prioritized. However, these semantics
        /// could be changed in future based on performance measurements.
        /// </remarks>
        public static async Task<bool> IsCandidateForDeprioritizationBasedOnRegisteredActionsAsync(
            DiagnosticAnalyzer analyzer,
            CompilationWithAnalyzers? compilationWithAnalyzers,
            CancellationToken cancellationToken)
        {
            // We deprioritize SymbolStart/End and SemanticModel analyzers from 'Normal' to 'Low' priority bucket,
            // as these are computationally more expensive.
            if (compilationWithAnalyzers != null && !analyzer.IsWorkspaceDiagnosticAnalyzer())
            {
                var telemetryInfo = await compilationWithAnalyzers.GetAnalyzerTelemetryInfoAsync(analyzer, cancellationToken).ConfigureAwait(false);
                return telemetryInfo?.SymbolStartActionsCount > 0 || telemetryInfo?.SemanticModelActionsCount > 0;
            }

            return false;
        }

        /// <summary>
        /// Tracks the given <paramref name="analyzer"/> as a de-prioritized analyzer that should be moved to
        /// <see cref="CodeActionRequestPriority.Low"/> bucket.
        /// </summary>
        public void TrackDeprioritizedAnalyzer(DiagnosticAnalyzer analyzer)
        {
            Debug.Assert(Priority == CodeActionRequestPriority.Normal);
            _lowPriorityAnalyzers.Add(analyzer);
        }

        /// <summary>
        /// Returns true if the given <paramref name="codeFixProvider"/> should be considered
        /// a candidate when computing fixes for the given <see cref="Priority"/>.
        /// </summary>
        public bool MatchesPriority(
            CodeFixProvider codeFixProvider)
            => Priority switch
            {
                // We are computing fixes for all priorities
                CodeActionRequestPriority.None => true,

                // 'Low' priority is used for fixes for expensive analyzers which were de-prioritized
                // to low priority bucket.
                // We accept fixers with any RequestPriority that can fix diagnostics
                // from low priority analyzers.
                CodeActionRequestPriority.Low => true,

                _ => Priority == codeFixProvider.RequestPriority,
            };
    }
}
