﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics.EngineV2;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    [Export(typeof(IDiagnosticAnalyzerService))]
    [Shared]
    internal partial class DiagnosticAnalyzerService : IDiagnosticAnalyzerService
    {
        private const string DiagnosticsUpdatedEventName = "DiagnosticsUpdated";

        // use eventMap and taskQueue to serialize events
        private readonly EventMap _eventMap;
        private readonly TaskQueue _eventQueue;

        public DiagnosticAnalyzerInfoCache AnalyzerInfoCache { get; private set; }

        public IAsynchronousOperationListener Listener { get; }

        private readonly ConditionalWeakTable<Workspace, DiagnosticIncrementalAnalyzer> _map;
        private readonly ConditionalWeakTable<Workspace, DiagnosticIncrementalAnalyzer>.CreateValueCallback _createIncrementalAnalyzer;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DiagnosticAnalyzerService(
            IDiagnosticUpdateSourceRegistrationService registrationService,
            IAsynchronousOperationListenerProvider listenerProvider)
        {
            AnalyzerInfoCache = new DiagnosticAnalyzerInfoCache();
            Listener = listenerProvider.GetListener(FeatureAttribute.DiagnosticService);

            _map = new ConditionalWeakTable<Workspace, DiagnosticIncrementalAnalyzer>();
            _createIncrementalAnalyzer = CreateIncrementalAnalyzerCallback;
            _eventMap = new EventMap();

            _eventQueue = new TaskQueue(Listener, TaskScheduler.Default);

            registrationService.Register(this);
        }

        public void Reanalyze(Workspace workspace, IEnumerable<ProjectId>? projectIds = null, IEnumerable<DocumentId>? documentIds = null, bool highPriority = false)
        {
            var service = workspace.Services.GetService<ISolutionCrawlerService>();
            if (service != null && _map.TryGetValue(workspace, out var analyzer))
            {
                service.Reanalyze(workspace, analyzer, projectIds, documentIds, highPriority);
            }
        }

        public Task<bool> TryAppendDiagnosticsForSpanAsync(Document document, TextSpan range, ArrayBuilder<DiagnosticData> diagnostics, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default)
        {
            if (_map.TryGetValue(document.Project.Solution.Workspace, out var analyzer))
            {
                // always make sure that analyzer is called on background thread.
                return Task.Run(() => analyzer.TryAppendDiagnosticsForSpanAsync(document, range, diagnostics, diagnosticId: null, includeSuppressedDiagnostics, blockForData: false, addOperationScope: null, cancellationToken), cancellationToken);
            }

            return SpecializedTasks.False;
        }

        public Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForSpanAsync(Document document, TextSpan range, string? diagnosticId = null, bool includeSuppressedDiagnostics = false, Func<string, IDisposable?>? addOperationScope = null, CancellationToken cancellationToken = default)
        {
            if (_map.TryGetValue(document.Project.Solution.Workspace, out var analyzer))
            {
                // always make sure that analyzer is called on background thread.
                return Task.Run(() => analyzer.GetDiagnosticsForSpanAsync(document, range, diagnosticId, includeSuppressedDiagnostics, blockForData: true, addOperationScope, cancellationToken), cancellationToken);
            }

            return SpecializedTasks.EmptyImmutableArray<DiagnosticData>();
        }

        public Task<ImmutableArray<DiagnosticData>> GetCachedDiagnosticsAsync(Workspace workspace, ProjectId? projectId = null, DocumentId? documentId = null, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default)
        {
            if (_map.TryGetValue(workspace, out var analyzer))
            {
                return analyzer.GetCachedDiagnosticsAsync(workspace.CurrentSolution, projectId, documentId, includeSuppressedDiagnostics, cancellationToken);
            }

            return SpecializedTasks.EmptyImmutableArray<DiagnosticData>();
        }

        public Task<ImmutableArray<DiagnosticData>> GetSpecificCachedDiagnosticsAsync(Workspace workspace, object id, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default)
        {
            if (_map.TryGetValue(workspace, out var analyzer))
            {
                return analyzer.GetSpecificCachedDiagnosticsAsync(workspace.CurrentSolution, id, includeSuppressedDiagnostics, cancellationToken);
            }

            return SpecializedTasks.EmptyImmutableArray<DiagnosticData>();
        }

        public Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(Solution solution, ProjectId? projectId = null, DocumentId? documentId = null, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default)
        {
            if (_map.TryGetValue(solution.Workspace, out var analyzer))
            {
                return analyzer.GetDiagnosticsAsync(solution, projectId, documentId, includeSuppressedDiagnostics, cancellationToken);
            }

            return SpecializedTasks.EmptyImmutableArray<DiagnosticData>();
        }

        public async Task ForceAnalyzeAsync(Solution solution, Action<Project> onProjectAnalyzed, ProjectId? projectId = null, CancellationToken cancellationToken = default)
        {
            if (_map.TryGetValue(solution.Workspace, out var analyzer))
            {
                if (projectId != null)
                {
                    var project = solution.GetProject(projectId);
                    if (project != null)
                    {
                        await analyzer.ForceAnalyzeProjectAsync(project, cancellationToken).ConfigureAwait(false);
                        onProjectAnalyzed(project);
                    }
                }
                else
                {
                    var tasks = new Task[solution.ProjectIds.Count];
                    var index = 0;
                    foreach (var project in solution.Projects)
                    {
                        tasks[index++] = Task.Run(async () =>
                            {
                                await analyzer.ForceAnalyzeProjectAsync(project, cancellationToken).ConfigureAwait(false);
                                onProjectAnalyzed(project);
                            }, cancellationToken);
                    }

                    await Task.WhenAll(tasks).ConfigureAwait(false);
                }
            }
        }

        public Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForIdsAsync(
            Solution solution, ProjectId? projectId = null, DocumentId? documentId = null, ImmutableHashSet<string>? diagnosticIds = null, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default)
        {
            if (_map.TryGetValue(solution.Workspace, out var analyzer))
            {
                return analyzer.GetDiagnosticsForIdsAsync(solution, projectId, documentId, diagnosticIds, includeSuppressedDiagnostics, cancellationToken);
            }

            return SpecializedTasks.EmptyImmutableArray<DiagnosticData>();
        }

        public Task<ImmutableArray<DiagnosticData>> GetProjectDiagnosticsForIdsAsync(
            Solution solution, ProjectId? projectId = null, ImmutableHashSet<string>? diagnosticIds = null, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default)
        {
            if (_map.TryGetValue(solution.Workspace, out var analyzer))
            {
                return analyzer.GetProjectDiagnosticsForIdsAsync(solution, projectId, diagnosticIds, includeSuppressedDiagnostics, cancellationToken);
            }

            return SpecializedTasks.EmptyImmutableArray<DiagnosticData>();
        }

        public async Task<bool> IsCompilationEndAnalyzerAsync(DiagnosticAnalyzer diagnosticAnalyzer, Project project, CancellationToken cancellationToken)
        {
            if (_map.TryGetValue(project.Solution.Workspace, out var analyzer))
            {
                return await analyzer.IsCompilationEndAnalyzerAsync(diagnosticAnalyzer, project, cancellationToken).ConfigureAwait(false);
            }

            return false;
        }

        public bool ContainsDiagnostics(Workspace workspace, ProjectId projectId)
        {
            if (_map.TryGetValue(workspace, out var analyzer))
            {
                return analyzer.ContainsDiagnostics(projectId);
            }

            return false;
        }
    }
}
