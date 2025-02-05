﻿// Licensed to the .NET Foundation under one or more agreements.
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

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2;

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
        DiagnosticAnalyzerInfoCache analyzerInfoCache,
        IGlobalOptionService globalOptionService)
    {
        Contract.ThrowIfNull(analyzerService);

        AnalyzerService = analyzerService;
        Workspace = workspace;
        GlobalOptions = globalOptionService;

        _stateManager = new StateManager(workspace, analyzerInfoCache);

        var enabled = globalOptionService.GetOption(SolutionCrawlerRegistrationService.EnableSolutionCrawler);
        _diagnosticAnalyzerRunner = new InProcOrRemoteHostAnalyzerRunner(
            enabled, analyzerInfoCache, analyzerService.Listener);
    }

    internal IGlobalOptionService GlobalOptions { get; }
    internal DiagnosticAnalyzerInfoCache DiagnosticAnalyzerInfoCache => _diagnosticAnalyzerRunner.AnalyzerInfoCache;

    public static Task<VersionStamp> GetDiagnosticVersionAsync(Project project, CancellationToken cancellationToken)
        => project.GetDependentVersionAsync(cancellationToken);

    public static Task<Checksum> GetDiagnosticChecksumAsync(Project project, CancellationToken cancellationToken)
        => project.GetDependentChecksumAsync(cancellationToken);

    private static DiagnosticAnalysisResult GetResultOrEmpty(ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult> map, DiagnosticAnalyzer analyzer, ProjectId projectId, Checksum checksum)
        => map.TryGetValue(analyzer, out var result) ? result : DiagnosticAnalysisResult.CreateEmpty(projectId, checksum);

    internal async Task<IEnumerable<DiagnosticAnalyzer>> GetAnalyzersTestOnlyAsync(Project project, CancellationToken cancellationToken)
    {
        var analyzers = await _stateManager.GetOrCreateStateSetsAsync(project, cancellationToken).ConfigureAwait(false);

        return analyzers.Select(static s => s.Analyzer);
    }

    private static string GetProjectLogMessage(Project project, ImmutableArray<StateSet> stateSets)
        => $"project: ({project.Id}), ({string.Join(Environment.NewLine, stateSets.Select(s => s.Analyzer.ToString()))})";

    private static string GetOpenLogMessage(TextDocument document)
        => $"document open: ({document.FilePath ?? document.Name})";
}
