// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2
{
    internal class DiagnosticIncrementalAnalyzer : BaseDiagnosticIncrementalAnalyzer
    {
        // What is _correlationId for?
        private readonly int _correlationId;
        private readonly DiagnosticAnalyzerService _owner;
        private readonly HostAnalyzerManager _hostAnalyzerManager;

        private readonly ConcurrentDictionary<ProjectId, CompilationDescriptors> _compilationDescriptors = new ConcurrentDictionary<ProjectId, CompilationDescriptors>();

        public DiagnosticIncrementalAnalyzer(DiagnosticAnalyzerService owner, int correlationId, Workspace workspace, HostAnalyzerManager hostAnalyzerManager, AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource)
            : base(workspace, hostDiagnosticUpdateSource)
        {
            _correlationId = correlationId;
            _owner = owner;
            _hostAnalyzerManager = hostAnalyzerManager;
        }

        #region IIncrementalAnalyzer
        public async override Task AnalyzeDocumentAsync(Document document, SyntaxNode bodyOpt, CancellationToken cancellationToken)
        {
            // ToDo: Should there be an exception handler here? Should there be checks for cancellation?

            if (document.SupportsSemanticModel)
            {
                Project project = document.Project;
                VersionStamp projectVersion = await project.GetDependentVersionAsync(cancellationToken).ConfigureAwait(false);
                DocumentId documentId = document.Id;

                SemanticModel documentModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                Compilation compilation = documentModel.Compilation;
                CompilationDescriptor compilationDescriptor = GetCompilationDescriptor(compilation, project, projectVersion, cancellationToken);

                if (!compilationDescriptor.DocumentAnalyzed(documentId) && !compilationDescriptor.CompletionStarted)
                {
                    ImmutableArray<Diagnostic> diagnostics = await compilationDescriptor.AnalyzeDocumentAsync(documentId, documentModel, cancellationToken).ConfigureAwait(false);
                    RaiseEvents(project, diagnostics);
                }
            }
        }

        public override Task AnalyzeSyntaxAsync(Document document, CancellationToken cancellationToken)
        {
            // Nothing happens here. The syntax tree analyzers get run as part of AnalyzeDocumentAsync.
            return SpecializedTasks.EmptyTask;
        }

        public override async Task AnalyzeProjectAsync(Project project, bool semanticsChanged, CancellationToken cancellationToken)
        {
            VersionStamp projectVersion = await project.GetDependentVersionAsync(cancellationToken).ConfigureAwait(false);

            Compilation compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            CompilationDescriptor compilationDescriptor = GetCompilationDescriptor(compilation, project, projectVersion, cancellationToken);

            if (!compilationDescriptor.CompletionStarted)
            {
                ImmutableArray<Diagnostic> diagnostics = await compilationDescriptor.AnalyzeProjectAsync(cancellationToken).ConfigureAwait(false);
                RaiseEvents(project, diagnostics);
                MarkComplete(project, compilationDescriptor);
            }
        }

        public override Task NewSolutionSnapshotAsync(Solution solution, CancellationToken cancellationToken)
        {
            return SpecializedTasks.EmptyTask;
        }

        public override Task DocumentOpenAsync(Document document, CancellationToken cancellationToken)
        {
            // Opening a file now has no effect on its analysis.
            return SpecializedTasks.EmptyTask;
        }

        public override Task DocumentResetAsync(Document document, CancellationToken cancellationToken)
        {
            // Closing a file now has no effect on its analysis.
            return SpecializedTasks.EmptyTask;
        }

        public override void RemoveProject(ProjectId projectId)
        {
            lock (_compilationDescriptors)
            {
                CompilationDescriptors descriptors;
                _compilationDescriptors.TryRemove(projectId, out descriptors);
            }

            _owner.RaiseDiagnosticsUpdated(this, new DiagnosticsUpdatedArgs(ValueTuple.Create(this, projectId), Workspace, null, null, null, ImmutableArray<DiagnosticData>.Empty));
        }

        public override void RemoveDocument(DocumentId documentId)
        {
            _owner.RaiseDiagnosticsUpdated(
                this, new DiagnosticsUpdatedArgs(ValueTuple.Create(this, documentId), Workspace, null, null, null, ImmutableArray<DiagnosticData>.Empty));
        }
        #endregion

        public override async Task<ImmutableArray<DiagnosticData>> GetCachedDiagnosticsAsync(Solution solution, ProjectId projectId, DocumentId documentId, CancellationToken cancellationToken)
        {
            return await GetDiagnosticsAsync(CompilationVintage.Completed, solution, projectId, documentId, cancellationToken).ConfigureAwait(false);
        }

        private async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(CompilationVintage vintage, Solution solution, ProjectId projectId, DocumentId documentId, CancellationToken cancellationToken)
        {
            if (projectId != null)
            {
                Project project = solution.GetProject(projectId);

                if (project != null)
                {
                    CompilationDescriptor compilationDescriptor = GetCompilationDescriptor(project, vintage);
                    if (compilationDescriptor != null && !compilationDescriptor.ProjectVersion.Equals(VersionStamp.Default))
                    {
                        if (documentId != null)
                        {
                            Document document = project.GetDocument(documentId);
                            if (document != null)
                            {
                                return GetDiagnosticData(project, await compilationDescriptor.GetDocumentDiagnosticsAsync(document).ConfigureAwait(false)).ToImmutableArrayOrEmpty();
                            }
                        }
                        else
                        {
                            return GetDiagnosticData(project, await compilationDescriptor.GetAllDiagnosticsAsync().ConfigureAwait(false)).ToImmutableArrayOrEmpty();
                        }
                    }
                }
            }
            else
            {
                ImmutableArray<DiagnosticData>.Builder diagnosticsBuilder = ImmutableArray.CreateBuilder<DiagnosticData>();
                foreach (ProjectId solutionProjectId in solution.ProjectIds)
                {
                    diagnosticsBuilder.AddRange(await GetDiagnosticsAsync(vintage, solution, solutionProjectId, null, cancellationToken).ConfigureAwait(false));
                }

                return diagnosticsBuilder.ToImmutable();
            }

            return ImmutableArray<DiagnosticData>.Empty;
        }

        public override Task<ImmutableArray<DiagnosticData>> GetSpecificCachedDiagnosticsAsync(Solution solution, object id, CancellationToken cancellationToken)
        {
            return GetSpecificDiagnosticsAsync(CompilationVintage.Completed, solution, id, cancellationToken);
        }

        private async Task<ImmutableArray<DiagnosticData>> GetSpecificDiagnosticsAsync(CompilationVintage vintage, Solution solution, object id, CancellationToken cancellationToken)
        {
            if (id is ValueTuple<DiagnosticIncrementalAnalyzer, DocumentId>)
            {
                var key = (ValueTuple<DiagnosticIncrementalAnalyzer, DocumentId>)id;
                return await GetDiagnosticsAsync(vintage, solution, key.Item2.ProjectId, key.Item2, cancellationToken).ConfigureAwait(false);
            }

            if (id is ValueTuple<DiagnosticIncrementalAnalyzer, ProjectId>)
            {
                var key = (ValueTuple<DiagnosticIncrementalAnalyzer, ProjectId>)id;
                ImmutableArray<DiagnosticData> diagnostics = await GetDiagnosticsAsync(vintage, solution, key.Item2, null, cancellationToken).ConfigureAwait(false);
                return diagnostics.Where(d => d.DocumentId == null).ToImmutableArray();
            }

            return ImmutableArray<DiagnosticData>.Empty;
        }
        
        public override async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(Solution solution, ProjectId projectId, DocumentId documentId, CancellationToken cancellationToken)
        {
            return await GetDiagnosticsAsync(CompilationVintage.Current, solution, projectId, documentId, cancellationToken).ConfigureAwait(false);
        }

        public override async Task<ImmutableArray<DiagnosticData>> GetSpecificDiagnosticsAsync(Solution solution, object id, CancellationToken cancellationToken)
        {
            return await GetSpecificDiagnosticsAsync(CompilationVintage.Current, solution, id, cancellationToken).ConfigureAwait(false);
        }

        // New above here.

        public override async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForIdsAsync(Solution solution, ProjectId projectId = null, DocumentId documentId = null, ImmutableHashSet<string> diagnosticIds = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var diagnostics = await GetDiagnosticsAsync(solution, projectId, documentId, cancellationToken).ConfigureAwait(false);
            return diagnostics.Where(d => diagnosticIds.Contains(d.Id)).ToImmutableArrayOrEmpty();
        }

        public override async Task<ImmutableArray<DiagnosticData>> GetProjectDiagnosticsForIdsAsync(Solution solution, ProjectId projectId = null, ImmutableHashSet<string> diagnosticIds = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var diagnostics = await GetDiagnosticsForIdsAsync(solution, projectId, null, diagnosticIds, cancellationToken).ConfigureAwait(false);
            return diagnostics.Where(d => d.DocumentId == null).ToImmutableArray();
        }

        public override async Task<bool> TryAppendDiagnosticsForSpanAsync(Document document, TextSpan range, List<DiagnosticData> result, CancellationToken cancellationToken)
        {
            result.AddRange(await GetDiagnosticsForSpanAsync(document, range, cancellationToken).ConfigureAwait(false));
            return true;
        }

        public override async Task<IEnumerable<DiagnosticData>> GetDiagnosticsForSpanAsync(Document document, TextSpan range, CancellationToken cancellationToken)
        {
            var diagnostics = await GetDiagnosticsAsync(document.Project.Solution, document.Project.Id, document.Id, cancellationToken).ConfigureAwait(false);
            return diagnostics.Where(d => range.IntersectsWith(d.TextSpan));
        }

        private async Task<ImmutableArray<DiagnosticData>> GetProjectDiagnosticsAsync(Project project, CancellationToken cancellationToken)
        {
            if (project == null)
            {
                return ImmutableArray<DiagnosticData>.Empty;
            }

            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

            var analyzers = _hostAnalyzerManager.CreateDiagnosticAnalyzers(project);

            var compilationWithAnalyzer = compilation.WithAnalyzers(analyzers, project.AnalyzerOptions, cancellationToken);

            // REVIEW: this API is a bit strange. 
            //         if getting diagnostic is cancelled, it has to create new compilation and do everything from scretch again?
            var dxs = GetDiagnosticData(project, await compilationWithAnalyzer.GetAnalyzerDiagnosticsAsync().ConfigureAwait(false)).ToImmutableArrayOrEmpty();

            return dxs;
        }

        private IEnumerable<DiagnosticData> GetDiagnosticData(Project project, ImmutableArray<Diagnostic> diagnostics)
        {
            foreach (var diagnostic in diagnostics)
            {
                if (diagnostic.Location == Location.None)
                {
                    yield return DiagnosticData.Create(project, diagnostic);
                    continue;
                }

                var document = project.GetDocument(diagnostic.Location.SourceTree);
                if (document == null)
                {
                    continue;
                }

                yield return DiagnosticData.Create(document, diagnostic);
            }
        }

        private void RaiseEvents(Project project, ImmutableArray<Diagnostic> rawDiagnostics)
        {
            var diagnostics = GetDiagnosticData(project, rawDiagnostics);
            var groups = diagnostics.GroupBy(d => d.DocumentId);

            var solution = project.Solution;
            var workspace = solution.Workspace;

            foreach (var kv in groups)
            {
                if (kv.Key == null)
                {
                    _owner.RaiseDiagnosticsUpdated(
                        this, new DiagnosticsUpdatedArgs(
                            ValueTuple.Create(this, project.Id), workspace, solution, project.Id, null, kv.ToImmutableArrayOrEmpty()));
                    continue;
                }

                _owner.RaiseDiagnosticsUpdated(
                    this, new DiagnosticsUpdatedArgs(
                        ValueTuple.Create(this, kv.Key), workspace, solution, project.Id, kv.Key, kv.ToImmutableArrayOrEmpty()));
            }
        }

        // New below here.

        // CompilationDescriptors represents the compilations and associated diagnostics for up to two versions
        // (one complete and one current) of a project.
        private class CompilationDescriptors
        {
            // CompletedCompilation represents the most recent compilation of a project for which analysis came to full completion.
            // It includes all diagnostics but does not hold on to the Compilation object used to create them.
            public CompilationDescriptor CompletedCompilation = new CompilationDescriptor(VersionStamp.Default, null);

            // CurrentCompilation represents the most recent compilation of a project, with analysis possibly ongoing.
            // It includes a snapshot of current diagnostics and the Compilation being used to create more.
            public CompilationDescriptor CurrentCompilation = new CompilationDescriptor(VersionStamp.Default, null);
        }

        private enum CompilationVintage
        {
            Completed,
            Current
        }

        private CompilationDescriptor GetCompilationDescriptor(Project project, CompilationVintage vintage)
        {
            lock (_compilationDescriptors)
            {
                CompilationDescriptors descriptors;
                if (_compilationDescriptors.TryGetValue(project.Id, out descriptors))
                {
                    return vintage == CompilationVintage.Completed ? descriptors.CompletedCompilation : descriptors.CurrentCompilation;
                }
            }

            return null;
        }

        private CompilationDescriptor GetCompilationDescriptor(Compilation compilation, Project project, VersionStamp projectVersion, CancellationToken cancellationToken)
        {
            lock (_compilationDescriptors)
            {
                CompilationDescriptors descriptors;
                if (!_compilationDescriptors.TryGetValue(project.Id, out descriptors))
                {
                    descriptors = new CompilationDescriptors();
                    _compilationDescriptors[project.Id] = descriptors;
                }

                CompilationDescriptor completedCompilation = descriptors.CompletedCompilation;
                if (completedCompilation.ProjectVersion == projectVersion)
                {
                    return completedCompilation;
                }

                CompilationDescriptor currentCompilation = descriptors.CurrentCompilation;
                if (currentCompilation.ProjectVersion != projectVersion)
                {
                    // The requested project version is not the same as that of the current compilation.
                    // Assume that the requested version is newer.

                    CompilationWithAnalyzers newCompilationWithAnalyzers = compilation.WithAnalyzers(Flatten(_hostAnalyzerManager.GetHostDiagnosticAnalyzersPerReference(project.Language).Values), project.AnalyzerOptions, cancellationToken);
                    CompilationDescriptor newCompilation = new CompilationDescriptor(projectVersion, newCompilationWithAnalyzers);
                    
                    descriptors.CurrentCompilation = newCompilation;
                }

                return descriptors.CurrentCompilation;
            }
        }

        private void MarkComplete(Project project, CompilationDescriptor compilationDescriptor)
        {
            lock (_compilationDescriptors)
            {
                _compilationDescriptors[project.Id].CompletedCompilation = compilationDescriptor;
            }
        }

        private static ImmutableArray<T> Flatten<T>(IEnumerable<ImmutableArray<T>> arrays)
        {
            ImmutableArray<T>.Builder builder = ImmutableArray.CreateBuilder<T>();
            foreach (ImmutableArray<T> array in arrays)
            {
                builder.AddRange(array);
            }

            return builder.ToImmutable();
        }

        private static string LocationPath(Location location)
        {
            if (location != null)
            {
                SyntaxTree tree = location.SourceTree;
                if (tree != null)
                {
                    return tree.FilePath;
                }
            }

            return "";
        }

        private class CompilationDescriptor
        {
            public VersionStamp ProjectVersion { get; private set; }

            private readonly ConcurrentDictionary<string, ImmutableArray<Diagnostic>> _diagnosticsPerPaths = new ConcurrentDictionary<string, ImmutableArray<Diagnostic>>();
            private readonly ConcurrentDictionary<DocumentId, Task> _analyzedDocuments = new ConcurrentDictionary<DocumentId, Task>();
            private readonly object _updateDiagnosticsLock = new object();
            private CompilationWithAnalyzers _compilationWithAnalyzers;

            private Task _completionTask;

            public CompilationDescriptor(VersionStamp projectVersion, CompilationWithAnalyzers compilation)
            {
                _compilationWithAnalyzers = compilation;
                this.ProjectVersion = projectVersion;
            }

            public bool CompletionStarted
            {
                get { return _completionTask != null; }
            }

            public bool DocumentAnalyzed(DocumentId documentId)
            {
                return _analyzedDocuments.ContainsKey(documentId);
            }

            public void DistributeDiagnostics(ImmutableArray<Diagnostic> diagnostics)
            {
                lock (_updateDiagnosticsLock)
                {
                    ConcurrentDictionary<string, ImmutableArray<Diagnostic>> diagnosticMap = _diagnosticsPerPaths;
                    foreach (Diagnostic diagnostic in diagnostics)
                    {
                        string diagnosticPath = LocationPath(diagnostic.Location);
                        ImmutableArray<Diagnostic> diagnosticsPerPath;
                        if (diagnosticMap.TryGetValue(diagnosticPath, out diagnosticsPerPath))
                        {
                            diagnosticsPerPath = diagnosticsPerPath.Add(diagnostic);
                        }
                        else
                        {
                            diagnosticsPerPath = ImmutableArray.Create(diagnostic);
                        }

                        diagnosticMap[diagnosticPath] = diagnosticsPerPath;
                    }
                }
            }

            public async Task<ImmutableArray<Diagnostic>> AnalyzeProjectAsync(CancellationToken cancellationToken)
            {
                ImmutableArray<Diagnostic> diagnostics = ImmutableArray<Diagnostic>.Empty;
                lock (_updateDiagnosticsLock)
                {
                    if (!this.CompletionStarted)
                    {
                        _completionTask = Task.Run(async () =>
                        {
                            diagnostics = await _compilationWithAnalyzers.GetAllDiagnosticsAsync().ConfigureAwait(false);

                            // Enable the compilation to be collected.
                            _compilationWithAnalyzers = null;

                            DistributeDiagnostics(diagnostics);
                        }, cancellationToken);
                    }
                }

                await _completionTask.ConfigureAwait(false);
                return diagnostics;
            }

            public async Task<ImmutableArray<Diagnostic>> AnalyzeDocumentAsync(DocumentId documentId, SemanticModel documentModel, CancellationToken cancellationToken)
            {
                ImmutableArray<Diagnostic> diagnostics = ImmutableArray<Diagnostic>.Empty;

                lock (_updateDiagnosticsLock)
                {
                    if (!DocumentAnalyzed(documentId) && !CompletionStarted)
                    {
                        _analyzedDocuments[documentId] = Task.Run(async () =>
                        {
                            diagnostics = await _compilationWithAnalyzers.GetDiagnosticsFromDocumentAsync(documentModel).ConfigureAwait(false);
                            DistributeDiagnostics(diagnostics);
                        }, cancellationToken);
                    }
                }

                if (DocumentAnalyzed(documentId))
                {
                    await _analyzedDocuments[documentId].ConfigureAwait(false);
                }

                return diagnostics;
            }

            public async Task<ImmutableArray<Diagnostic>> GetAllDiagnosticsAsync()
            {
                if (CompletionStarted)
                {
                    await _completionTask.ConfigureAwait(false);

                    ImmutableArray<Diagnostic>.Builder result = ImmutableArray.CreateBuilder<Diagnostic>();
                    lock (_updateDiagnosticsLock)
                    {
                        foreach (ImmutableArray<Diagnostic> diagnostics in _diagnosticsPerPaths.Values)
                        {
                            result.AddRange(diagnostics);
                        }
                    }

                    return result.ToImmutable();
                }

                return ImmutableArray<Diagnostic>.Empty;
            }

            public async Task<ImmutableArray<Diagnostic>> GetDocumentDiagnosticsAsync(Document document)
            {
                if (DocumentAnalyzed(document.Id))
                {
                    await _analyzedDocuments[document.Id].ConfigureAwait(false);
                }
                else if (CompletionStarted)
                {
                    await _completionTask.ConfigureAwait(false);
                }

                ImmutableArray<Diagnostic> documentDiagnostics;
                if (_diagnosticsPerPaths.TryGetValue(document.FilePath, out documentDiagnostics))
                {
                    return documentDiagnostics;
                }

                return ImmutableArray<Diagnostic>.Empty;
            }
        }
    }
}
