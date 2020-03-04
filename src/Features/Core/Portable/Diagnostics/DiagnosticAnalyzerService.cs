// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics.Log;
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
        public DiagnosticAnalyzerInfoCache AnalyzerInfoCache { get; private set; }

        private readonly AbstractHostDiagnosticUpdateSource? _hostDiagnosticUpdateSource;

        public IAsynchronousOperationListener Listener { get; }

        [ImportingConstructor]
        public DiagnosticAnalyzerService(
            IDiagnosticUpdateSourceRegistrationService registrationService,
            IAsynchronousOperationListenerProvider listenerProvider,
            PrimaryWorkspace primaryWorkspace,
            [Import(AllowDefault = true)]IHostDiagnosticAnalyzerPackageProvider? diagnosticAnalyzerProviderService = null,
            [Import(AllowDefault = true)]AbstractHostDiagnosticUpdateSource? hostDiagnosticUpdateSource = null)
            : this(new Lazy<ImmutableArray<HostDiagnosticAnalyzerPackage>>(() => GetHostDiagnosticAnalyzerPackage(diagnosticAnalyzerProviderService), isThreadSafe: true),
                   diagnosticAnalyzerProviderService?.GetAnalyzerAssemblyLoader(),
                   hostDiagnosticUpdateSource,
                   primaryWorkspace,
                   registrationService, listenerProvider.GetListener(FeatureAttribute.DiagnosticService))
        {
            // diagnosticAnalyzerProviderService and hostDiagnosticUpdateSource can only be null in test harness. Otherwise, it should never be null.
        }

        // protected for testing purposes.
        protected DiagnosticAnalyzerService(
            Lazy<ImmutableArray<HostDiagnosticAnalyzerPackage>> workspaceAnalyzerPackages,
            IAnalyzerAssemblyLoader? hostAnalyzerAssemblyLoader,
            AbstractHostDiagnosticUpdateSource? hostDiagnosticUpdateSource,
            PrimaryWorkspace primaryWorkspace,
            IDiagnosticUpdateSourceRegistrationService registrationService,
            IAsynchronousOperationListener? listener = null)
            : this(new DiagnosticAnalyzerInfoCache(workspaceAnalyzerPackages, hostAnalyzerAssemblyLoader, hostDiagnosticUpdateSource, primaryWorkspace),
                   hostDiagnosticUpdateSource,
                   registrationService,
                   listener)
        {
        }

        // protected for testing purposes.
        protected DiagnosticAnalyzerService(
            DiagnosticAnalyzerInfoCache analyzerInfoCache,
            AbstractHostDiagnosticUpdateSource? hostDiagnosticUpdateSource,
            IDiagnosticUpdateSourceRegistrationService registrationService,
            IAsynchronousOperationListener? listener = null)
            : this(registrationService)
        {
            AnalyzerInfoCache = analyzerInfoCache;
            _hostDiagnosticUpdateSource = hostDiagnosticUpdateSource;
            Listener = listener ?? AsynchronousOperationListenerProvider.NullListener;
        }

        private static ImmutableArray<HostDiagnosticAnalyzerPackage> GetHostDiagnosticAnalyzerPackage(IHostDiagnosticAnalyzerPackageProvider? diagnosticAnalyzerProviderService)
            => diagnosticAnalyzerProviderService?.GetHostDiagnosticAnalyzerPackages() ?? ImmutableArray<HostDiagnosticAnalyzerPackage>.Empty;

        public void Reanalyze(Workspace workspace, IEnumerable<ProjectId>? projectIds = null, IEnumerable<DocumentId>? documentIds = null, bool highPriority = false)
        {
            var service = workspace.Services.GetService<ISolutionCrawlerService>();
            if (service != null && _map.TryGetValue(workspace, out var analyzer))
            {
                service.Reanalyze(workspace, analyzer, projectIds, documentIds, highPriority);
            }
        }

        public Task<bool> TryAppendDiagnosticsForSpanAsync(Document document, TextSpan range, List<DiagnosticData> diagnostics, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default)
        {
            if (_map.TryGetValue(document.Project.Solution.Workspace, out var analyzer))
            {
                // always make sure that analyzer is called on background thread.
                return Task.Run(() => analyzer.TryAppendDiagnosticsForSpanAsync(document, range, diagnostics, diagnosticId: null, includeSuppressedDiagnostics, blockForData: false, addOperationScope: null, cancellationToken), cancellationToken);
            }

            return SpecializedTasks.False;
        }

        public Task<IEnumerable<DiagnosticData>> GetDiagnosticsForSpanAsync(Document document, TextSpan range, string? diagnosticId = null, bool includeSuppressedDiagnostics = false, Func<string, IDisposable?>? addOperationScope = null, CancellationToken cancellationToken = default)
        {
            if (_map.TryGetValue(document.Project.Solution.Workspace, out var analyzer))
            {
                // always make sure that analyzer is called on background thread.
                return Task.Run(() => analyzer.GetDiagnosticsForSpanAsync(document, range, diagnosticId, includeSuppressedDiagnostics, blockForData: true, addOperationScope, cancellationToken), cancellationToken);
            }

            return SpecializedTasks.EmptyEnumerable<DiagnosticData>();
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

        public async Task ForceAnalyzeAsync(Solution solution, ProjectId? projectId = null, CancellationToken cancellationToken = default)
        {
            if (_map.TryGetValue(solution.Workspace, out var analyzer))
            {
                if (projectId != null)
                {
                    var project = solution.GetProject(projectId);
                    if (project != null)
                    {
                        await analyzer.ForceAnalyzeProjectAsync(project, cancellationToken).ConfigureAwait(false);
                    }
                }
                else
                {
                    var tasks = new Task[solution.ProjectIds.Count];
                    var index = 0;
                    foreach (var project in solution.Projects)
                    {
                        var localProject = project;
                        tasks[index++] = Task.Run(
                            () => analyzer.ForceAnalyzeProjectAsync(project, cancellationToken));
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

        public bool IsCompilationEndAnalyzer(DiagnosticAnalyzer diagnosticAnalyzer, Project project, Compilation compilation)
        {
            if (_map.TryGetValue(project.Solution.Workspace, out var analyzer))
            {
                return analyzer.IsCompilationEndAnalyzer(diagnosticAnalyzer, project, compilation);
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
