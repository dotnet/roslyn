// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Reflection;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    [Export(typeof(IDiagnosticAnalyzerService))]
    [Shared]
    internal partial class DiagnosticAnalyzerService : IDiagnosticAnalyzerService
    {
        private readonly ImmutableArray<AnalyzerReference> _workspaceAnalyzers;
        private readonly Dictionary<DiagnosticAnalyzer, ImmutableArray<DiagnosticDescriptor>> _diagnosticMap;

        [ImportingConstructor]
        public DiagnosticAnalyzerService([Import(AllowDefault = true)]IWorkspaceDiagnosticAnalyzerProviderService diagnosticAnalyzerProviderService = null)
            : this(workspaceAnalyzerAssemblies: diagnosticAnalyzerProviderService != null ?
                  diagnosticAnalyzerProviderService.GetWorkspaceAnalyzerAssemblies() :
                  SpecializedCollections.EmptyEnumerable<string>())
        {
        }

        private DiagnosticAnalyzerService(IEnumerable<string> workspaceAnalyzerAssemblies)
            : this(GetWorkspaceAnalyzers(workspaceAnalyzerAssemblies))
        {
        }

        // internal for testing purposes.
        internal DiagnosticAnalyzerService(ImmutableArray<AnalyzerReference> workspaceAnalyzers) : this()
        {
            _diagnosticMap = new Dictionary<DiagnosticAnalyzer, ImmutableArray<DiagnosticDescriptor>>();
            _workspaceAnalyzers = workspaceAnalyzers.IsDefault ? ImmutableArray<AnalyzerReference>.Empty : workspaceAnalyzers;
            DiagnosticAnalyzerLogger.LogWorkspaceAnalyzers(_workspaceAnalyzers);
        }

        private static ImmutableArray<AnalyzerReference> GetWorkspaceAnalyzers(IEnumerable<string> workspaceAnalyzerAssemblies)
        {
            if (workspaceAnalyzerAssemblies == null || workspaceAnalyzerAssemblies.IsEmpty())
            {
                return ImmutableArray<AnalyzerReference>.Empty;
            }

            // We want to load the analyzer assembly assets in default context.
            // Use Assembly.Load instead of Assembly.LoadFrom to ensure that if the assembly is ngen'ed, then the native image gets loaded.
            Func<string, Assembly> getAssembly = (fullPath) => Assembly.Load(AssemblyName.GetAssemblyName(fullPath));

            var analyzerReferences = new List<AnalyzerReference>();
            foreach (var analyzerAssembly in workspaceAnalyzerAssemblies.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var analyzerReference = new AnalyzerFileReference(analyzerAssembly, getAssembly);
                analyzerReferences.Add(analyzerReference);
            }

            return analyzerReferences.ToImmutableArray();
        }

        public IReadOnlyDictionary<string, IEnumerable<DiagnosticDescriptor>> GetAllDiagnosticDescriptors(Project projectOpt)
        {
            var analyzerMap = GetAllAvailableDiagnosticAnalyzers(projectOpt);

            var descriptorMap = new Dictionary<string, IEnumerable<DiagnosticDescriptor>>();
            foreach (var analyzerPair in analyzerMap)
            {
                IEnumerable<DiagnosticDescriptor> descriptors = SpecializedCollections.EmptyEnumerable<DiagnosticDescriptor>();
                foreach (var analyzer in analyzerPair.Value)
                {
                    if (analyzer != null)
                    {
                        descriptors = descriptors.Concat(GetDiagnosticDescriptors(analyzer));
                    }
                }

                descriptorMap.Add(analyzerPair.Key, descriptors);
            }

            return descriptorMap;
        }

        private IReadOnlyDictionary<string, IEnumerable<DiagnosticAnalyzer>> GetAllDiagnosticAnalyzers()
        {
            var builder = ImmutableDictionary.CreateBuilder<string, IEnumerable<DiagnosticAnalyzer>>();
            foreach (var workspaceAnalyzer in _workspaceAnalyzers)
            {
                var analyzers = workspaceAnalyzer.GetAnalyzersForAllLanguages();
                if (analyzers.Any())
                {
                    builder.Add(workspaceAnalyzer.Display ?? FeaturesResources.Unknown, analyzers);
                }
            }

            return builder.ToImmutable();
        }

        internal ImmutableArray<DiagnosticDescriptor> GetDiagnosticDescriptors(DiagnosticAnalyzer analyzer)
        {
            ImmutableArray<DiagnosticDescriptor> descriptors;

            lock (_diagnosticMap)
            {
                if (!_diagnosticMap.TryGetValue(analyzer, out descriptors))
                {
                    try
                    {
                        descriptors = analyzer.SupportedDiagnostics;
                    }
                    catch when (ShouldHandleAnalyzer(analyzer))
                    {
                        // If the SupportedDiagnostics throws an exception, then we don't want to run the analyzer.
                        descriptors = ImmutableArray<DiagnosticDescriptor>.Empty;
                    }

                    _diagnosticMap.Add(analyzer, descriptors);
                }
            }

            return descriptors;
        }

        public static bool ShouldHandleAnalyzer(DiagnosticAnalyzer analyzer)
        {
            return !(analyzer is IBuiltInAnalyzer || analyzer is DocumentDiagnosticAnalyzer || analyzer is ProjectDiagnosticAnalyzer);
        }

        public void Reanalyze(Workspace workspace, IEnumerable<ProjectId> projectIds = null, IEnumerable<DocumentId> documentIds = null)
        {
            DiagnosticIncrementalAnalyzer analyzer;

            var service = workspace.Services.GetService<ISolutionCrawlerService>();
            if (service != null && _map.TryGetValue(workspace, out analyzer))
            {
                service.Reanalyze(workspace, analyzer, projectIds, documentIds);
            }
        }

        public Task<bool> TryGetDiagnosticsForSpanAsync(Document document, TextSpan range, List<DiagnosticData> diagnostics, CancellationToken cancellationToken)
        {
            DiagnosticIncrementalAnalyzer analyzer;
            if (_map.TryGetValue(document.Project.Solution.Workspace, out analyzer))
            {
                // always make sure that analyzer is called on background thread.
                return Task.Run(async () => await analyzer.TryGetDiagnosticAsync(document, range, diagnostics, cancellationToken).ConfigureAwait(false), cancellationToken);
            }

            return SpecializedTasks.False;
        }

        public Task<IEnumerable<DiagnosticData>> GetDiagnosticsForSpanAsync(Document document, TextSpan range, CancellationToken cancellationToken)
        {
            DiagnosticIncrementalAnalyzer analyzer;
            if (_map.TryGetValue(document.Project.Solution.Workspace, out analyzer))
            {
                // always make sure that analyzer is called on background thread.
                return Task.Run(async () => await analyzer.GetDiagnosticsAsync(document, range, cancellationToken).ConfigureAwait(false), cancellationToken);
            }

            return SpecializedTasks.EmptyEnumerable<DiagnosticData>();
        }

        public Task<ImmutableArray<DiagnosticData>> GetCachedDiagnosticsAsync(Workspace workspace, ProjectId projectId = null, DocumentId documentId = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            DiagnosticIncrementalAnalyzer analyzer;
            if (_map.TryGetValue(workspace, out analyzer))
            {
                return analyzer.GetCachedDiagnosticsAsync(workspace.CurrentSolution, projectId, documentId, cancellationToken);
            }

            return SpecializedTasks.EmptyImmutableArray<DiagnosticData>();
        }

        public Task<ImmutableArray<DiagnosticData>> GetSpecificCachedDiagnosticsAsync(Workspace workspace, object id, CancellationToken cancellationToken)
        {
            DiagnosticIncrementalAnalyzer analyzer;
            if (_map.TryGetValue(workspace, out analyzer))
            {
                return analyzer.GetSpecificCachedDiagnosticsAsync(workspace.CurrentSolution, id, cancellationToken);
            }

            return SpecializedTasks.EmptyImmutableArray<DiagnosticData>();
        }

        public Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(Solution solution, ProjectId projectId = null, DocumentId documentId = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            DiagnosticIncrementalAnalyzer analyzer;
            if (_map.TryGetValue(solution.Workspace, out analyzer))
            {
                return analyzer.GetDiagnosticsAsync(solution, projectId, documentId, cancellationToken);
            }

            return SpecializedTasks.EmptyImmutableArray<DiagnosticData>();
        }

        public Task<ImmutableArray<DiagnosticData>> GetSpecificDiagnosticsAsync(Solution solution, object id, CancellationToken cancellationToken)
        {
            DiagnosticIncrementalAnalyzer analyzer;
            if (_map.TryGetValue(solution.Workspace, out analyzer))
            {
                return analyzer.GetSpecificDiagnosticsAsync(solution, id, cancellationToken);
            }

            return SpecializedTasks.EmptyImmutableArray<DiagnosticData>();
        }

        public Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForIdsAsync(
            Solution solution, ProjectId projectId = null, DocumentId documentId = null, ImmutableHashSet<string> diagnosticIds = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            DiagnosticIncrementalAnalyzer analyzer;
            if (_map.TryGetValue(solution.Workspace, out analyzer))
            {
                return analyzer.GetDiagnosticsForIdsAsync(solution, projectId, documentId, diagnosticIds, cancellationToken);
            }

            return SpecializedTasks.EmptyImmutableArray<DiagnosticData>();
        }

        public Task<ImmutableArray<DiagnosticData>> GetProjectDiagnosticsForIdsAsync(
            Solution solution, ProjectId projectId = null, ImmutableHashSet<string> diagnosticIds = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            DiagnosticIncrementalAnalyzer analyzer;
            if (_map.TryGetValue(solution.Workspace, out analyzer))
            {
                return analyzer.GetProjectDiagnosticsForIdsAsync(solution, projectId, diagnosticIds, cancellationToken);
            }

            return SpecializedTasks.EmptyImmutableArray<DiagnosticData>();
        }

        private IReadOnlyDictionary<string, IEnumerable<DiagnosticAnalyzer>> GetAllAvailableDiagnosticAnalyzers(Project project)
        {
            var analyzerMap = new Dictionary<string, IEnumerable<DiagnosticAnalyzer>>();
            if (project == null)
            {
                var globalAnalyzers = GetAllDiagnosticAnalyzers();
                analyzerMap.AddRange(globalAnalyzers);
            }
            else
            {
                var incrementalAnalyzer = GetOrCreateIncrementalAnalyzerCore(project.Solution.Workspace);
                var projectAnalyzers = incrementalAnalyzer.GetAllDiagnosticAnalyzersAsync(project, CancellationToken.None).WaitAndGetResult(CancellationToken.None);
                analyzerMap.AddRange(projectAnalyzers);
            }

            return analyzerMap;
        }
    }
}
