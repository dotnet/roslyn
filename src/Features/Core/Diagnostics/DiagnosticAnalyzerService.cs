// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
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
        private readonly HostAnalyzerManager _hostAnalyzerManager;
        private readonly AbstractHostDiagnosticUpdateSource _hostDiagnosticUpdateSource;
        private readonly IAsynchronousOperationListener _listener;

        [ImportingConstructor]
        public DiagnosticAnalyzerService(
            [ImportMany] IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners,
            [Import(AllowDefault = true)]IWorkspaceDiagnosticAnalyzerProviderService diagnosticAnalyzerProviderService = null,
            [Import(AllowDefault = true)]AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource = null)
            : this(workspaceAnalyzerAssemblies: diagnosticAnalyzerProviderService != null ?
                   diagnosticAnalyzerProviderService.GetWorkspaceAnalyzerAssemblies() :
                   SpecializedCollections.EmptyEnumerable<string>(),
                  hostDiagnosticUpdateSource: hostDiagnosticUpdateSource)
        {
            _listener = new AggregateAsynchronousOperationListener(asyncListeners, FeatureAttribute.DiagnosticService);
        }

        public IAsynchronousOperationListener Listener => _listener;

        private DiagnosticAnalyzerService(IEnumerable<string> workspaceAnalyzerAssemblies, AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource) : this()
        {
            _hostAnalyzerManager = new HostAnalyzerManager(workspaceAnalyzerAssemblies, hostDiagnosticUpdateSource);
            _hostDiagnosticUpdateSource = hostDiagnosticUpdateSource;
        }

        // internal for testing purposes.
        internal DiagnosticAnalyzerService(ImmutableArray<AnalyzerReference> workspaceAnalyzers, AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource = null) : this()
        {
            _hostAnalyzerManager = new HostAnalyzerManager(workspaceAnalyzers, hostDiagnosticUpdateSource);
            _hostDiagnosticUpdateSource = hostDiagnosticUpdateSource;
        }

        public ImmutableDictionary<string, ImmutableArray<DiagnosticDescriptor>> GetDiagnosticDescriptors(Project projectOpt)
        {
            if (projectOpt == null)
            {
                return _hostAnalyzerManager.GetHostDiagnosticDescriptorsPerReference();
            }

            return _hostAnalyzerManager.CreateDiagnosticDescriptorsPerReference(projectOpt);
        }

        public ImmutableArray<DiagnosticDescriptor> GetDiagnosticDescriptors(DiagnosticAnalyzer analyzer)
        {
            return _hostAnalyzerManager.GetDiagnosticDescriptors(analyzer);
        }

        public void Reanalyze(Workspace workspace, IEnumerable<ProjectId> projectIds = null, IEnumerable<DocumentId> documentIds = null)
        {
            BaseDiagnosticIncrementalAnalyzer analyzer;

            var service = workspace.Services.GetService<ISolutionCrawlerService>();
            if (service != null && _map.TryGetValue(workspace, out analyzer))
            {
                service.Reanalyze(workspace, analyzer, projectIds, documentIds);
            }
        }

        public Task<bool> TryAppendDiagnosticsForSpanAsync(Document document, TextSpan range, List<DiagnosticData> diagnostics, CancellationToken cancellationToken)
        {
            BaseDiagnosticIncrementalAnalyzer analyzer;
            if (_map.TryGetValue(document.Project.Solution.Workspace, out analyzer))
            {
                // always make sure that analyzer is called on background thread.
                return Task.Run(async () => await analyzer.TryAppendDiagnosticsForSpanAsync(document, range, diagnostics, cancellationToken).ConfigureAwait(false), cancellationToken);
            }

            return SpecializedTasks.False;
        }

        public Task<IEnumerable<DiagnosticData>> GetDiagnosticsForSpanAsync(Document document, TextSpan range, CancellationToken cancellationToken)
        {
            BaseDiagnosticIncrementalAnalyzer analyzer;
            if (_map.TryGetValue(document.Project.Solution.Workspace, out analyzer))
            {
                // always make sure that analyzer is called on background thread.
                return Task.Run(async () => await analyzer.GetDiagnosticsForSpanAsync(document, range, cancellationToken).ConfigureAwait(false), cancellationToken);
            }

            return SpecializedTasks.EmptyEnumerable<DiagnosticData>();
        }

        public Task<ImmutableArray<DiagnosticData>> GetCachedDiagnosticsAsync(Workspace workspace, ProjectId projectId = null, DocumentId documentId = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            BaseDiagnosticIncrementalAnalyzer analyzer;
            if (_map.TryGetValue(workspace, out analyzer))
            {
                return analyzer.GetCachedDiagnosticsAsync(workspace.CurrentSolution, projectId, documentId, cancellationToken);
            }

            return SpecializedTasks.EmptyImmutableArray<DiagnosticData>();
        }

        public Task<ImmutableArray<DiagnosticData>> GetSpecificCachedDiagnosticsAsync(Workspace workspace, object id, CancellationToken cancellationToken)
        {
            BaseDiagnosticIncrementalAnalyzer analyzer;
            if (_map.TryGetValue(workspace, out analyzer))
            {
                return analyzer.GetSpecificCachedDiagnosticsAsync(workspace.CurrentSolution, id, cancellationToken);
            }

            return SpecializedTasks.EmptyImmutableArray<DiagnosticData>();
        }

        public Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(Solution solution, ProjectId projectId = null, DocumentId documentId = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            BaseDiagnosticIncrementalAnalyzer analyzer;
            if (_map.TryGetValue(solution.Workspace, out analyzer))
            {
                return analyzer.GetDiagnosticsAsync(solution, projectId, documentId, cancellationToken);
            }

            return SpecializedTasks.EmptyImmutableArray<DiagnosticData>();
        }

        public Task<ImmutableArray<DiagnosticData>> GetSpecificDiagnosticsAsync(Solution solution, object id, CancellationToken cancellationToken)
        {
            BaseDiagnosticIncrementalAnalyzer analyzer;
            if (_map.TryGetValue(solution.Workspace, out analyzer))
            {
                return analyzer.GetSpecificDiagnosticsAsync(solution, id, cancellationToken);
            }

            return SpecializedTasks.EmptyImmutableArray<DiagnosticData>();
        }

        public Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForIdsAsync(
            Solution solution, ProjectId projectId = null, DocumentId documentId = null, ImmutableHashSet<string> diagnosticIds = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            BaseDiagnosticIncrementalAnalyzer analyzer;
            if (_map.TryGetValue(solution.Workspace, out analyzer))
            {
                return analyzer.GetDiagnosticsForIdsAsync(solution, projectId, documentId, diagnosticIds, cancellationToken);
            }

            return SpecializedTasks.EmptyImmutableArray<DiagnosticData>();
        }

        public Task<ImmutableArray<DiagnosticData>> GetProjectDiagnosticsForIdsAsync(
            Solution solution, ProjectId projectId = null, ImmutableHashSet<string> diagnosticIds = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            BaseDiagnosticIncrementalAnalyzer analyzer;
            if (_map.TryGetValue(solution.Workspace, out analyzer))
            {
                return analyzer.GetProjectDiagnosticsForIdsAsync(solution, projectId, diagnosticIds, cancellationToken);
            }

            return SpecializedTasks.EmptyImmutableArray<DiagnosticData>();
        }
    }
}
