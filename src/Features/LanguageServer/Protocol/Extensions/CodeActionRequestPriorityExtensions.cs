// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CodeActions
{
    internal static class CodeActionRequestPriorityExtensions
    {
        /// <summary>
        /// Returns true if the given <paramref name="analyzer"/> can report diagnostics that can have
        /// fixes from a code fix provider with <see cref="CodeFixProvider.RequestPriority"/>
        /// matching the given <paramref name="priority"/>.
        /// This method is useful for performing a performance optimization for lightbulb diagnostic computation,
        /// wherein we can reduce the set of analyzers to be executed when computing fixes for a specific
        /// <paramref name="priority"/>.
        /// </summary>
        public static async Task<bool> MatchesPriorityAsync(
            this CodeActionRequestPriority priority,
            DiagnosticAnalyzer analyzer,
            CompilationWithAnalyzers? compilationWithAnalyzers,
            CancellationToken cancellationToken)
        {
            // If caller isn't asking for prioritized result, then run all analyzers.
            if (priority == CodeActionRequestPriority.None)
                return true;

            // 'CodeActionRequestPriority.Lowest' is used for suppression/configuration fixes,
            // which requires all analyzer diagnostics.
            if (priority == CodeActionRequestPriority.Lowest)
                return true;

            // The compiler analyzer always counts for any priority.  It's diagnostics may be fixed
            // by high pri or normal pri fixers.
            if (analyzer.IsCompilerAnalyzer())
                return true;

            var analyzerPriority = analyzer is IBuiltInAnalyzer { RequestPriority: var requestPriority }
                ? requestPriority
                : CodeActionRequestPriority.Normal;

            // 'CodeActionRequestPriority.Low' is used for SymbolStart/End analyzers,
            // which are computationally more expensive.
            if (compilationWithAnalyzers != null && !analyzer.IsWorkspaceDiagnosticAnalyzer())
            {
                var telemetryInfo = await compilationWithAnalyzers.GetAnalyzerTelemetryInfoAsync(analyzer, cancellationToken).ConfigureAwait(false);
                if (telemetryInfo?.SymbolStartActionsCount > 0)
                    analyzerPriority = CodeActionRequestPriority.Low;
            }

            return priority == analyzerPriority;
        }

        /// <summary>
        /// Returns true if the given <paramref name="codeFixProvider"/> should be considered
        /// a candidate when computing fixes for the given <paramref name="priority"/>.
        /// </summary>
        public static bool MatchesPriority(
            this CodeActionRequestPriority priority,
            CodeFixProvider codeFixProvider)
            => priority switch
            {
                // We are computing fixes for all priorities
                CodeActionRequestPriority.None => true,

                // 'Low' priority is used for fixes for SymbolStart/End analyzers,
                // which are computationally more expensive.
                // We accept fixers with any RequestPriority that can fix diagnostics
                // from SymbolStart/End analyzers.
                CodeActionRequestPriority.Low => true,

                _ => priority == codeFixProvider.RequestPriority,
            };
    }
}
