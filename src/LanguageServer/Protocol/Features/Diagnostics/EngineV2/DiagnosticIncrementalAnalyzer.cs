// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Workspaces.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2
{
    /// <summary>
    /// Diagnostic Analyzer Engine V2
    /// 
    /// This one follows pattern compiler has set for diagnostic analyzer.
    /// </summary>
    internal partial class DiagnosticIncrementalAnalyzer
    {
        private readonly DiagnosticAnalyzerTelemetry _telemetry = new();
        private readonly StateManager _stateManager;
        private readonly InProcOrRemoteHostAnalyzerRunner _diagnosticAnalyzerRunner;
        private readonly IncrementalMemberEditAnalyzer _incrementalMemberEditAnalyzer = new();

        internal DiagnosticAnalyzerService AnalyzerService { get; }
        internal Workspace Workspace { get; }

        [Obsolete(MefConstruction.FactoryMethodMessage, error: true)]
        public DiagnosticIncrementalAnalyzer(
            DiagnosticAnalyzerService analyzerService,
            Workspace workspace,
            DiagnosticAnalyzerInfoCache analyzerInfoCache)
        {
            Contract.ThrowIfNull(analyzerService);

            AnalyzerService = analyzerService;
            Workspace = workspace;

            _stateManager = new StateManager(workspace, analyzerInfoCache);
            _stateManager.ProjectAnalyzerReferenceChanged += OnProjectAnalyzerReferenceChanged;

            var enabled = this.AnalyzerService.GlobalOptions.GetOption(SolutionCrawlerRegistrationService.EnableSolutionCrawler);
            _diagnosticAnalyzerRunner = new InProcOrRemoteHostAnalyzerRunner(
                enabled, analyzerInfoCache, analyzerService.Listener);
        }

        internal IGlobalOptionService GlobalOptions => AnalyzerService.GlobalOptions;
        internal DiagnosticAnalyzerInfoCache DiagnosticAnalyzerInfoCache => _diagnosticAnalyzerRunner.AnalyzerInfoCache;

        private void OnProjectAnalyzerReferenceChanged(object? sender, ProjectAnalyzerReferenceChangedEventArgs e)
        {
            if (e.Removed.Length == 0)
            {
                // nothing to refresh
                return;
            }

            // make sure we drop cache related to the analyzers
            foreach (var stateSet in e.Removed)
                stateSet.OnRemoved();
        }

        public static Task<VersionStamp> GetDiagnosticVersionAsync(Project project, CancellationToken cancellationToken)
            => project.GetDependentVersionAsync(cancellationToken);

        private static DiagnosticAnalysisResult GetResultOrEmpty(ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult> map, DiagnosticAnalyzer analyzer, ProjectId projectId, VersionStamp version)
        {
            if (map.TryGetValue(analyzer, out var result))
            {
                return result;
            }

            return DiagnosticAnalysisResult.CreateEmpty(projectId, version);
        }

        internal IEnumerable<DiagnosticAnalyzer> GetAnalyzersTestOnly(Project project)
            => _stateManager.GetOrCreateStateSets(project).Select(s => s.Analyzer);

        private static string GetProjectLogMessage(Project project, ImmutableArray<StateSet> stateSets)
            => $"project: ({project.Id}), ({string.Join(Environment.NewLine, stateSets.Select(s => s.Analyzer.ToString()))})";

        private static string GetOpenLogMessage(TextDocument document)
            => $"document open: ({document.FilePath ?? document.Name})";
    }
}
