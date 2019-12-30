// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private readonly DiagnosticAnalyzerInfoCache _analyzerInfoCache;
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
            _analyzerInfoCache = analyzerInfoCache;
            _hostDiagnosticUpdateSource = hostDiagnosticUpdateSource;
            Listener = listener ?? AsynchronousOperationListenerProvider.NullListener;
        }

        private static ImmutableArray<HostDiagnosticAnalyzerPackage> GetHostDiagnosticAnalyzerPackage(IHostDiagnosticAnalyzerPackageProvider? diagnosticAnalyzerProviderService)
            => diagnosticAnalyzerProviderService?.GetHostDiagnosticAnalyzerPackages() ?? ImmutableArray<HostDiagnosticAnalyzerPackage>.Empty;

        public ImmutableArray<DiagnosticAnalyzer> GetDiagnosticAnalyzers(Project project)
        {
            var map = _analyzerInfoCache.CreateDiagnosticAnalyzersPerReference(project);
            return map.Values.SelectMany(v => v).ToImmutableArray();
        }

        public ImmutableDictionary<string, ImmutableArray<DiagnosticDescriptor>> CreateDiagnosticDescriptorsPerReference(Project? project)
        {
            if (project == null)
            {
                return ConvertReferenceIdentityToName(_analyzerInfoCache.GetHostDiagnosticDescriptorsPerReference());
            }

            return ConvertReferenceIdentityToName(_analyzerInfoCache.CreateDiagnosticDescriptorsPerReference(project), project);
        }

        private ImmutableDictionary<string, ImmutableArray<DiagnosticDescriptor>> ConvertReferenceIdentityToName(
            ImmutableDictionary<object, ImmutableArray<DiagnosticDescriptor>> descriptorsPerReference, Project? project = null)
        {
            var map = _analyzerInfoCache.CreateAnalyzerReferencesMap(project);

            var builder = ImmutableDictionary.CreateBuilder<string, ImmutableArray<DiagnosticDescriptor>>();

            foreach (var (id, descriptors) in descriptorsPerReference)
            {
                if (!map.TryGetValue(id, out var reference) || reference == null)
                {
                    continue;
                }

                var displayName = GetDisplayName(reference);
                // if there are duplicates, merge descriptors
                if (builder.TryGetValue(displayName, out var existing))
                {
                    builder[displayName] = existing.AddRange(descriptors);
                    continue;
                }

                builder.Add(displayName, descriptors);
            }

            return builder.ToImmutable();
        }

        private static string GetDisplayName(AnalyzerReference reference)
        {
            return reference.Display ?? FeaturesResources.Unknown;
        }

        public ImmutableArray<DiagnosticDescriptor> GetDiagnosticDescriptors(DiagnosticAnalyzer analyzer)
        {
            return _analyzerInfoCache.GetDiagnosticDescriptors(analyzer);
        }

        public bool IsAnalyzerSuppressed(DiagnosticAnalyzer analyzer, Project project)
        {
            return _analyzerInfoCache.IsAnalyzerSuppressed(analyzer, project);
        }

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

        public bool IsCompilerDiagnostic(string language, DiagnosticData diagnostic)
        {
            return _analyzerInfoCache.IsCompilerDiagnostic(language, diagnostic);
        }

        public DiagnosticAnalyzer? GetCompilerDiagnosticAnalyzer(string language)
        {
            return _analyzerInfoCache.GetCompilerDiagnosticAnalyzer(language);
        }

        public bool IsCompilerDiagnosticAnalyzer(string language, DiagnosticAnalyzer analyzer)
        {
            return _analyzerInfoCache.IsCompilerDiagnosticAnalyzer(language, analyzer);
        }

        public bool IsCompilationEndAnalyzer(DiagnosticAnalyzer diagnosticAnalyzer, Project project, Compilation compilation)
        {
            if (_map.TryGetValue(project.Solution.Workspace, out var analyzer))
            {
                return analyzer.IsCompilationEndAnalyzer(diagnosticAnalyzer, project, compilation);
            }

            return false;
        }

        public IEnumerable<AnalyzerReference> GetHostAnalyzerReferences()
        {
            // CreateAnalyzerReferencesMap will return only host analyzer reference map if project is not specified.
            return _analyzerInfoCache.CreateAnalyzerReferencesMap(project: null).Values;
        }

        public bool ContainsDiagnostics(Workspace workspace, ProjectId projectId)
        {
            if (_map.TryGetValue(workspace, out var analyzer))
            {
                return analyzer.ContainsDiagnostics(projectId);
            }

            return false;
        }

        // virtual for testing purposes.
        internal virtual Action<Exception, DiagnosticAnalyzer, Diagnostic> GetOnAnalyzerException(ProjectId projectId, DiagnosticLogAggregator? logAggregator)
        {
            return (ex, analyzer, diagnostic) =>
            {
                // Log telemetry, if analyzer supports telemetry.
                DiagnosticAnalyzerLogger.LogAnalyzerCrashCount(analyzer, ex, logAggregator);

                AnalyzerHelper.OnAnalyzerException_NoTelemetryLogging(analyzer, diagnostic, _hostDiagnosticUpdateSource, projectId);
            };
        }
    }
}
