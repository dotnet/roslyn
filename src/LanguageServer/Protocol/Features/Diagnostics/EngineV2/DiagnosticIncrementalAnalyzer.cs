// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal partial class DiagnosticAnalyzerService
{
    /// <summary>
    /// Diagnostic Analyzer Engine V2
    /// 
    /// This one follows pattern compiler has set for diagnostic analyzer.
    /// </summary>
    private partial class DiagnosticIncrementalAnalyzer
    {
        private readonly DiagnosticAnalyzerTelemetry _telemetry = new();
        private readonly StateManager _stateManager;
        private readonly InProcOrRemoteHostAnalyzerRunner _diagnosticAnalyzerRunner;
        private readonly IncrementalMemberEditAnalyzer _incrementalMemberEditAnalyzer = new();

        internal DiagnosticAnalyzerService AnalyzerService { get; }

        public DiagnosticIncrementalAnalyzer(
            DiagnosticAnalyzerService analyzerService,
            DiagnosticAnalyzerInfoCache analyzerInfoCache,
            IGlobalOptionService globalOptionService)
        {
            Contract.ThrowIfNull(analyzerService);

            AnalyzerService = analyzerService;
            GlobalOptions = globalOptionService;

            _stateManager = new StateManager(analyzerInfoCache);

            var enabled = globalOptionService.GetOption(SolutionCrawlerRegistrationService.EnableSolutionCrawler);
            _diagnosticAnalyzerRunner = new InProcOrRemoteHostAnalyzerRunner(
                enabled, analyzerInfoCache, analyzerService.Listener);
        }

        internal IGlobalOptionService GlobalOptions { get; }
        internal DiagnosticAnalyzerInfoCache DiagnosticAnalyzerInfoCache => _diagnosticAnalyzerRunner.AnalyzerInfoCache;

        public Task<ImmutableArray<DiagnosticAnalyzer>> GetAnalyzersForTestingPurposesOnlyAsync(Project project, CancellationToken cancellationToken)
            => _stateManager.GetOrCreateAnalyzersAsync(project.Solution.SolutionState, project.State, cancellationToken);

        private static string GetProjectLogMessage(Project project, ImmutableArray<DiagnosticAnalyzer> analyzers)
            => $"project: ({project.Id}), ({string.Join(Environment.NewLine, analyzers.Select(a => a.ToString()))})";
    }
}
