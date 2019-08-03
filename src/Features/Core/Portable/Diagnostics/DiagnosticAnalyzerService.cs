// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private readonly HostAnalyzerManager _hostAnalyzerManager;
        private readonly AbstractHostDiagnosticUpdateSource _hostDiagnosticUpdateSource;
        private readonly IAsynchronousOperationListener _listener;

        [ImportingConstructor]
        public DiagnosticAnalyzerService(
            IDiagnosticUpdateSourceRegistrationService registrationService,
            IAsynchronousOperationListenerProvider listenerProvider,
            PrimaryWorkspace primaryWorkspace,
            [Import(AllowDefault = true)]IWorkspaceDiagnosticAnalyzerProviderService diagnosticAnalyzerProviderService = null,
            [Import(AllowDefault = true)]AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource = null)
            : this(new Lazy<ImmutableArray<HostDiagnosticAnalyzerPackage>>(() => GetHostDiagnosticAnalyzerPackage(diagnosticAnalyzerProviderService), isThreadSafe: true),
                diagnosticAnalyzerProviderService?.GetAnalyzerAssemblyLoader(),
                hostDiagnosticUpdateSource,
                primaryWorkspace,
                registrationService, listenerProvider.GetListener(FeatureAttribute.DiagnosticService))
        {
            // diagnosticAnalyzerProviderService and hostDiagnosticUpdateSource can only be null in test hardness otherwise, it should
            // never be null
        }

        // protected for testing purposes.
        protected DiagnosticAnalyzerService(
            Lazy<ImmutableArray<HostDiagnosticAnalyzerPackage>> workspaceAnalyzerPackages,
            IAnalyzerAssemblyLoader hostAnalyzerAssemblyLoader,
            AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource,
            PrimaryWorkspace primaryWorkspace,
            IDiagnosticUpdateSourceRegistrationService registrationService,
            IAsynchronousOperationListener listener = null)
            : this(new HostAnalyzerManager(workspaceAnalyzerPackages, hostAnalyzerAssemblyLoader, hostDiagnosticUpdateSource, primaryWorkspace), hostDiagnosticUpdateSource, registrationService, listener)
        {
        }

        public IAsynchronousOperationListener Listener => _listener;

        // protected for testing purposes.
        protected DiagnosticAnalyzerService(
            HostAnalyzerManager hostAnalyzerManager,
            AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource,
            IDiagnosticUpdateSourceRegistrationService registrationService,
            IAsynchronousOperationListener listener = null) : this(registrationService)
        {
            _hostAnalyzerManager = hostAnalyzerManager;
            _hostDiagnosticUpdateSource = hostDiagnosticUpdateSource;
            _listener = listener ?? AsynchronousOperationListenerProvider.NullListener;
        }

        public ImmutableArray<DiagnosticAnalyzer> GetDiagnosticAnalyzers(Project project)
        {
            var map = _hostAnalyzerManager.CreateDiagnosticAnalyzersPerReference(project);
            return map.Values.SelectMany(v => v).ToImmutableArray();
        }

        public ImmutableDictionary<string, ImmutableArray<DiagnosticDescriptor>> CreateDiagnosticDescriptorsPerReference(Project projectOpt)
        {
            if (projectOpt == null)
            {
                return ConvertReferenceIdentityToName(_hostAnalyzerManager.GetHostDiagnosticDescriptorsPerReference());
            }

            return ConvertReferenceIdentityToName(_hostAnalyzerManager.CreateDiagnosticDescriptorsPerReference(projectOpt), projectOpt);
        }

        private ImmutableDictionary<string, ImmutableArray<DiagnosticDescriptor>> ConvertReferenceIdentityToName(
            ImmutableDictionary<object, ImmutableArray<DiagnosticDescriptor>> descriptorsPerReference, Project projectOpt = null)
        {
            var map = _hostAnalyzerManager.CreateAnalyzerReferencesMap(projectOpt);

            var builder = ImmutableDictionary.CreateBuilder<string, ImmutableArray<DiagnosticDescriptor>>();

            foreach (var kv in descriptorsPerReference)
            {
                var id = kv.Key;
                var descriptors = kv.Value;
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
            return _hostAnalyzerManager.GetDiagnosticDescriptors(analyzer);
        }

        public bool IsAnalyzerSuppressed(DiagnosticAnalyzer analyzer, Project project)
        {
            return _hostAnalyzerManager.IsAnalyzerSuppressed(analyzer, project);
        }

        public void Reanalyze(Workspace workspace, IEnumerable<ProjectId> projectIds = null, IEnumerable<DocumentId> documentIds = null, bool highPriority = false)
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
                return Task.Run(() => analyzer.TryAppendDiagnosticsForSpanAsync(document, range, diagnostics, includeSuppressedDiagnostics, cancellationToken), cancellationToken);
            }

            return SpecializedTasks.False;
        }

        public Task<IEnumerable<DiagnosticData>> GetDiagnosticsForSpanAsync(Document document, TextSpan range, string diagnosticIdOpt = null, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default)
        {
            if (_map.TryGetValue(document.Project.Solution.Workspace, out var analyzer))
            {
                // always make sure that analyzer is called on background thread.
                return Task.Run(() => analyzer.GetDiagnosticsForSpanAsync(document, range, includeSuppressedDiagnostics, diagnosticIdOpt, cancellationToken), cancellationToken);
            }

            return SpecializedTasks.EmptyEnumerable<DiagnosticData>();
        }

        public Task<ImmutableArray<DiagnosticData>> GetCachedDiagnosticsAsync(Workspace workspace, ProjectId projectId = null, DocumentId documentId = null, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default)
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

        public Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(Solution solution, ProjectId projectId = null, DocumentId documentId = null, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default)
        {
            if (_map.TryGetValue(solution.Workspace, out var analyzer))
            {
                return analyzer.GetDiagnosticsAsync(solution, projectId, documentId, includeSuppressedDiagnostics, cancellationToken);
            }

            return SpecializedTasks.EmptyImmutableArray<DiagnosticData>();
        }

        public Task<ImmutableArray<DiagnosticData>> GetSpecificDiagnosticsAsync(Solution solution, object id, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default)
        {
            if (_map.TryGetValue(solution.Workspace, out var analyzer))
            {
                return analyzer.GetSpecificDiagnosticsAsync(solution, id, includeSuppressedDiagnostics, cancellationToken);
            }

            return SpecializedTasks.EmptyImmutableArray<DiagnosticData>();
        }

        public Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForIdsAsync(
            Solution solution, ProjectId projectId = null, DocumentId documentId = null, ImmutableHashSet<string> diagnosticIds = null, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default)
        {
            if (_map.TryGetValue(solution.Workspace, out var analyzer))
            {
                return analyzer.GetDiagnosticsForIdsAsync(solution, projectId, documentId, diagnosticIds, includeSuppressedDiagnostics, cancellationToken);
            }

            return SpecializedTasks.EmptyImmutableArray<DiagnosticData>();
        }

        public Task<ImmutableArray<DiagnosticData>> GetProjectDiagnosticsForIdsAsync(
            Solution solution, ProjectId projectId = null, ImmutableHashSet<string> diagnosticIds = null, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default)
        {
            if (_map.TryGetValue(solution.Workspace, out var analyzer))
            {
                return analyzer.GetProjectDiagnosticsForIdsAsync(solution, projectId, diagnosticIds, includeSuppressedDiagnostics, cancellationToken);
            }

            return SpecializedTasks.EmptyImmutableArray<DiagnosticData>();
        }

        public bool IsCompilerDiagnostic(string language, DiagnosticData diagnostic)
        {
            return _hostAnalyzerManager.IsCompilerDiagnostic(language, diagnostic);
        }

        public DiagnosticAnalyzer GetCompilerDiagnosticAnalyzer(string language)
        {
            return _hostAnalyzerManager.GetCompilerDiagnosticAnalyzer(language);
        }

        public bool IsCompilerDiagnosticAnalyzer(string language, DiagnosticAnalyzer analyzer)
        {
            return _hostAnalyzerManager.IsCompilerDiagnosticAnalyzer(language, analyzer);
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
            return _hostAnalyzerManager.CreateAnalyzerReferencesMap(projectOpt: null).Values;
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
        internal virtual Action<Exception, DiagnosticAnalyzer, Diagnostic> GetOnAnalyzerException(ProjectId projectId, DiagnosticLogAggregator logAggregatorOpt)
        {
            return (ex, analyzer, diagnostic) =>
            {
                // Log telemetry, if analyzer supports telemetry.
                DiagnosticAnalyzerLogger.LogAnalyzerCrashCount(analyzer, ex, logAggregatorOpt);

                AnalyzerHelper.OnAnalyzerException_NoTelemetryLogging(ex, analyzer, diagnostic, _hostDiagnosticUpdateSource, projectId);
            };
        }

        private static ImmutableArray<HostDiagnosticAnalyzerPackage> GetHostDiagnosticAnalyzerPackage(IWorkspaceDiagnosticAnalyzerProviderService diagnosticAnalyzerProviderService)
        {
            return (diagnosticAnalyzerProviderService?.GetHostDiagnosticAnalyzerPackages()).ToImmutableArrayOrEmpty();
        }
    }
}
