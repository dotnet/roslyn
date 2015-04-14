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
        // ToDo: What is _correlationId for?
        private readonly int _correlationId;

        private readonly ConcurrentDictionary<ProjectId, CompilationResults> _compilationResults = new ConcurrentDictionary<ProjectId, CompilationResults>();

        public DiagnosticIncrementalAnalyzer(DiagnosticAnalyzerService owner, int correlationId, Workspace workspace, HostAnalyzerManager hostAnalyzerManager, AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource)
            : base(owner, workspace, hostAnalyzerManager, hostDiagnosticUpdateSource)
        {
            _correlationId = correlationId;
        }

        #region IIncrementalAnalyzer
        public async override Task AnalyzeDocumentAsync(Document document, SyntaxNode body, CancellationToken cancellationToken)
        {
            // ToDo: Should there be an exception handler here? Should there be checks for cancellation?

            if (document.SupportsSemanticModel)
            {
                Project project = document.Project;
                VersionStamp projectVersion = await project.GetDependentVersionAsync(cancellationToken).ConfigureAwait(false);

                Compilation compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                CompilationResult compilationResult = GetCompilationResult(compilation, project, projectVersion, cancellationToken);

                if (!compilationResult.DocumentAnalyzed(document.Id) && !compilationResult.CompletionStarted)
                {
                    ImmutableArray<Diagnostic> diagnostics = await compilationResult.AnalyzeDocumentAsync(document, cancellationToken).ConfigureAwait(false);
                    RaiseEvents(project, document, diagnostics);
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
            // ToDo: The intent of the version stamp below is to encode a difference if any document in the project or any reference has changed.
            // Is that what the project version encodes, or does it just encode the configuration of the project?
            // (A quick looks suggests that this is OK.)
            VersionStamp projectVersion = await project.GetDependentVersionAsync(cancellationToken).ConfigureAwait(false);

            Compilation compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            CompilationResult compilationResult = GetCompilationResult(compilation, project, projectVersion, cancellationToken);

            if (!compilationResult.CompletionStarted)
            {
                ImmutableArray<Diagnostic> diagnostics = await compilationResult.AnalyzeProjectAsync(cancellationToken).ConfigureAwait(false);
                RaiseEvents(project, null, diagnostics);
                Complete(project, compilationResult);
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
            lock (_compilationResults)
            {
                CompilationResults results;
                _compilationResults.TryRemove(projectId, out results);
            }

            Owner.RaiseDiagnosticsUpdated(this, new DiagnosticsUpdatedArgs(new AnalyzerData<ProjectId>(this, projectId), Workspace, null, null, null, ImmutableArray<DiagnosticData>.Empty));
        }

        public override void RemoveDocument(DocumentId documentId)
        {
            Owner.RaiseDiagnosticsUpdated(
                this, new DiagnosticsUpdatedArgs(new AnalyzerData<DocumentId>(this, documentId), Workspace, null, null, null, ImmutableArray<DiagnosticData>.Empty));
        }
        #endregion

        public override async Task<ImmutableArray<DiagnosticData>> GetCachedDiagnosticsAsync(Solution solution, ProjectId projectId, DocumentId documentId, CancellationToken cancellationToken)
        {
            return await GetDiagnosticsAsync(CompilationVintage.Completed, solution, projectId, documentId, cancellationToken).ConfigureAwait(false);
        }

        public override async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(Solution solution, ProjectId projectId, DocumentId documentId, CancellationToken cancellationToken)
        {
            return await GetDiagnosticsAsync(CompilationVintage.Current, solution, projectId, documentId, cancellationToken).ConfigureAwait(false);
        }

        private async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(CompilationVintage vintage, Solution solution, ProjectId projectId, DocumentId documentId, CancellationToken cancellationToken)
        {
            if (projectId != null)
            {
                Project project = solution.GetProject(projectId);

                if (project != null)
                {
                    Document document = documentId != null ? project.GetDocument(documentId) : null;
                    VersionStamp projectVersion = vintage == CompilationVintage.Current ? await project.GetDependentVersionAsync(cancellationToken).ConfigureAwait(false) : VersionStamp.Default;
                    CompilationResult compilationResult = GetCompilationResult(project, projectVersion, vintage);
                    if (compilationResult == null ||
                        compilationResult.ProjectVersion.Equals(VersionStamp.Default) ||
                        (documentId == null && !compilationResult.CompletionStarted))
                    {
                        // There has been no request to analyze the project or document, so initiate analysis now.
                        // The cancellation token provided here is actually not necessarily appropriate for
                        // the analysis, because cancelling this diagnostics request should not necessarily
                        // cancel the analysis.
                        if (documentId != null && vintage != CompilationVintage.Completed)
                        {
                            if (document != null)
                            {
                                await AnalyzeDocumentAsync(document, null, cancellationToken).ConfigureAwait(false);
                            }
                        }
                        else
                        {
                            await AnalyzeProjectAsync(project, false, cancellationToken).ConfigureAwait(false);
                        }
                        compilationResult = GetCompilationResult(project, projectVersion, vintage);
                    }

                    if (compilationResult != null && !compilationResult.ProjectVersion.Equals(VersionStamp.Default))
                    {
                        if (documentId != null)
                        {
                            if (document != null)
                            {
                                return GetDiagnosticData(project, await compilationResult.GetDocumentDiagnosticsAsync(document).ConfigureAwait(false)).ToImmutableArrayOrEmpty();
                            }
                        }
                        else
                        {
                            return GetDiagnosticData(project, await compilationResult.GetAllDiagnosticsAsync().ConfigureAwait(false)).ToImmutableArrayOrEmpty();
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

        public override async Task<ImmutableArray<DiagnosticData>> GetSpecificDiagnosticsAsync(Solution solution, object id, CancellationToken cancellationToken)
        {
            return await GetSpecificDiagnosticsAsync(CompilationVintage.Current, solution, id, cancellationToken).ConfigureAwait(false);
        }

        private async Task<ImmutableArray<DiagnosticData>> GetSpecificDiagnosticsAsync(CompilationVintage vintage, Solution solution, object id, CancellationToken cancellationToken)
        {
            AnalyzerData<DocumentId> documentId = id as AnalyzerData<DocumentId>;
            if (documentId != null)
            {
                return await GetDiagnosticsAsync(vintage, solution, documentId.Container.ProjectId, documentId.Container, cancellationToken).ConfigureAwait(false);
            }

            AnalyzerData<ProjectId> projectId = id as AnalyzerData<ProjectId>;
            if (projectId != null)
            {
                ImmutableArray<DiagnosticData> diagnostics = await GetDiagnosticsAsync(vintage, solution, projectId.Container, null, cancellationToken).ConfigureAwait(false);
                return diagnostics.Where(d => d.DocumentId == null).ToImmutableArray();
            }

            return ImmutableArray<DiagnosticData>.Empty;
        }

        public override async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForIdsAsync(Solution solution, ProjectId projectId, DocumentId documentId, ImmutableHashSet<string> diagnosticIds, CancellationToken cancellationToken)
        {
            ImmutableArray<DiagnosticData> diagnostics = await GetDiagnosticsAsync(solution, projectId, documentId, cancellationToken).ConfigureAwait(false);
            if (diagnosticIds == null)
            {
                return diagnostics;
            }

            return diagnostics.Where(d => diagnosticIds.Contains(d.Id)).ToImmutableArrayOrEmpty();
        }

        public override async Task<ImmutableArray<DiagnosticData>> GetProjectDiagnosticsForIdsAsync(Solution solution, ProjectId projectId, ImmutableHashSet<string> diagnosticIds, CancellationToken cancellationToken)
        {
            return await GetDiagnosticsForIdsAsync(solution, projectId, null, diagnosticIds, cancellationToken).ConfigureAwait(false);
        }

        public override async Task<bool> TryAppendDiagnosticsForSpanAsync(Document document, TextSpan range, List<DiagnosticData> result, CancellationToken cancellationToken)
        {
            result.AddRange(await GetDiagnosticsForSpanAsync(document, range, cancellationToken).ConfigureAwait(false));
            return true;
        }

        public override async Task<IEnumerable<DiagnosticData>> GetDiagnosticsForSpanAsync(Document document, TextSpan range, CancellationToken cancellationToken)
        {
            ImmutableArray<DiagnosticData> diagnostics = await GetDiagnosticsAsync(document.Project.Solution, document.Project.Id, document.Id, cancellationToken).ConfigureAwait(false);
            return diagnostics.Where(d => range.IntersectsWith(d.TextSpan));
        }

        public override Task SynchronizeWithBuildAsync(Project project, ImmutableArray<DiagnosticData> diagnostics)
        {
            // Always prefer live diagnostics to diagnostics produced by a build.
            return SpecializedTasks.EmptyTask;
        }

        public override Task SynchronizeWithBuildAsync(Document document, ImmutableArray<DiagnosticData> diagnostics)
        {
            // Always prefer live diagnostics to diagnostics produced by a build.
            return SpecializedTasks.EmptyTask;
        }

        private IEnumerable<DiagnosticData> GetDiagnosticData(Project project, ImmutableArray<Diagnostic> diagnostics)
        {
            foreach (Diagnostic diagnostic in diagnostics)
            {
                if (diagnostic.Location == Location.None)
                {
                    yield return DiagnosticData.Create(project, diagnostic);
                }
                else
                {
                    Document document = project.GetDocument(diagnostic.Location.SourceTree);
                    if (document != null)
                    {
                        yield return DiagnosticData.Create(document, diagnostic);
                    }
                }
            }
        }
        
        private void RaiseEvents(Project project, Document document, ImmutableArray<Diagnostic> rawDiagnostics)
        {
            Solution solution = project.Solution;
            Workspace workspace = solution.Workspace;

            if (rawDiagnostics.Length > 0)
            {
                IEnumerable<DiagnosticData> diagnostics = GetDiagnosticData(project, rawDiagnostics);
                var groups = diagnostics.GroupBy(d => d.DocumentId);

                foreach (var kv in groups)
                {
                    if (kv.Key == null)
                    {
                        Owner.RaiseDiagnosticsUpdated(
                            this, new DiagnosticsUpdatedArgs(
                                new AnalyzerData<ProjectId>(this, project.Id), workspace, solution, project.Id, null, kv.ToImmutableArrayOrEmpty()));
                    }
                    else
                    {
                        Owner.RaiseDiagnosticsUpdated(
                            this, new DiagnosticsUpdatedArgs(
                                new AnalyzerData<DocumentId>(this, kv.Key), workspace, solution, project.Id, kv.Key, kv.ToImmutableArrayOrEmpty()));
                    }
                }
            }
            else
            {
                // There are no diagnostics. Update the document or project with that information.
                if (document != null)
                {
                    Owner.RaiseDiagnosticsUpdated(
                           this, new DiagnosticsUpdatedArgs(
                               new AnalyzerData<DocumentId>(this, document.Id), workspace, solution, project.Id, document.Id, ImmutableArray<DiagnosticData>.Empty));
                }
                else
                {
                    Owner.RaiseDiagnosticsUpdated(
                            this, new DiagnosticsUpdatedArgs(
                                new AnalyzerData<ProjectId>(this, project.Id), workspace, solution, project.Id, null, ImmutableArray<DiagnosticData>.Empty));
                }
            }
        }

        // CompilationResults represents the compilations and associated diagnostics for up to two versions
        // (one complete and one current) of a project.
        private class CompilationResults
        {
            // CompletedCompilation represents the most recent compilation of a project for which analysis came to full completion.
            // It includes all diagnostics but does not hold on to the Compilation object used to create them.
            public CompilationResult CompletedCompilation = new CompilationResult(null, VersionStamp.Default, null);

            // CurrentCompilation represents the most recent compilation of a project, with analysis possibly ongoing.
            // It includes a snapshot of current diagnostics and the Compilation being used to create more.
            public CompilationResult CurrentCompilation = new CompilationResult(null, VersionStamp.Default, null);
        }

        private enum CompilationVintage
        {
            Completed,
            Current
        }

        private CompilationResult GetCompilationResult(Project project, VersionStamp projectVersion, CompilationVintage vintage)
        {
            lock (_compilationResults)
            {
                CompilationResults results;
                if (_compilationResults.TryGetValue(project.Id, out results))
                {
                    if (vintage == CompilationVintage.Completed)
                    {
                        return results.CompletedCompilation;
                    }

                    if (results.CurrentCompilation.ProjectVersion == projectVersion)
                    {
                        return results.CurrentCompilation;
                    }
                }
            }

            return null;
        }

        private CompilationResult GetCompilationResult(Compilation compilation, Project project, VersionStamp projectVersion, CancellationToken cancellationToken)
        {
            lock (_compilationResults)
            {
                CompilationResults results;
                if (!_compilationResults.TryGetValue(project.Id, out results))
                {
                    results = new CompilationResults();
                    _compilationResults[project.Id] = results;
                }

                CompilationResult completedCompilation = results.CompletedCompilation;
                if (completedCompilation.ProjectVersion == projectVersion)
                {
                    return completedCompilation;
                }

                CompilationResult currentCompilation = results.CurrentCompilation;
                if (currentCompilation.ProjectVersion != projectVersion)
                {
                    // The requested project version is not the same as that of the current compilation.
                    // Assume that the requested version is newer.

                    ImmutableArray<DiagnosticAnalyzer> analyzers = Flatten(HostAnalyzerManager.CreateDiagnosticAnalyzersPerReference(project).Values);
                    CompilationWithAnalyzers newCompilationWithAnalyzers = !analyzers.IsEmpty ? compilation.WithAnalyzers(analyzers, project.AnalyzerOptions, cancellationToken) : null;
                    results.CurrentCompilation = new CompilationResult(project, projectVersion, newCompilationWithAnalyzers);
                }

                return results.CurrentCompilation;
            }
        }

        private void Complete(Project project, CompilationResult compilationResult)
        {
            if (compilationResult.CompletedSuccessfully)
            {
                lock (_compilationResults)
                {
                    _compilationResults[project.Id].CompletedCompilation = compilationResult;
                }
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

        // A CompilationResult first computes and then stores diagnostics for a compilation.
        // It does not keep a compilation or project alive after analysis completes.
        private class CompilationResult
        {
            public VersionStamp ProjectVersion { get; private set; }

            private readonly ConcurrentDictionary<DocumentId, ImmutableArray<Diagnostic>> _diagnosticsPerDocuments = new ConcurrentDictionary<DocumentId, ImmutableArray<Diagnostic>>();
            private readonly ConcurrentDictionary<DocumentId, Task> _analyzedDocuments = new ConcurrentDictionary<DocumentId, Task>();
            private readonly object _updateDiagnosticsLock = new object();
            private readonly DocumentId _noDocument;
            private CompilationWithAnalyzers _compilationWithAnalyzers;
            private Project _project;

            private Task _completionTask;
            private bool _completionCanceled;

            public CompilationResult(Project project, VersionStamp projectVersion, CompilationWithAnalyzers compilation)
            {
                _compilationWithAnalyzers = compilation;
                _project = project;
                this.ProjectVersion = projectVersion;

                if (project != null)
                {
                    _noDocument = new DocumentId(project.Id, Guid.Empty, "No document");
                }
            }

            public bool CompletionStarted
            {
                get { return _completionTask != null; }
            }

            public bool DocumentAnalyzed(DocumentId documentId)
            {
                return _analyzedDocuments.ContainsKey(documentId);
            }

            public async Task<ImmutableArray<Diagnostic>> AnalyzeProjectAsync(CancellationToken cancellationToken)
            {
                ImmutableArray<Diagnostic> diagnostics = ImmutableArray<Diagnostic>.Empty;
                if (_compilationWithAnalyzers != null)
                {
                    lock (_updateDiagnosticsLock)
                    {
                        if (!this.CompletionStarted)
                        {
                            _completionTask = Task.Run(async () =>
                            {
                                // If any documents have been separately analyzed, complete analysis of the project
                                // by analyzing each document individually. Otherwise, analyze the project as a whole.
                                if (_analyzedDocuments.Count > 0)
                                {
                                    foreach (Document document in _project.Documents)
                                    {
                                        Task documentTask;
                                        if (_analyzedDocuments.TryGetValue(document.Id, out documentTask))
                                        {
                                            await documentTask.ConfigureAwait(false);
                                        }
                                        else
                                        {
                                            diagnostics = diagnostics.AddRange(await GetDocumentDiagnosticsAsync(document, cancellationToken).ConfigureAwait(false));
                                        }
                                    }
                                }
                                else
                                {
                                    diagnostics = await _compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().ConfigureAwait(false);
                                }

                                DistributeDiagnostics(diagnostics);

                                // Enable the compilation and project to be collected.
                                _compilationWithAnalyzers = null;
                                _project = null;

                                if (cancellationToken.IsCancellationRequested)
                                {
                                    _completionCanceled = true;
                                }

                            }, cancellationToken);
                        }
                    }

                    await _completionTask.ConfigureAwait(false);
                }

                return Flatten(_diagnosticsPerDocuments.Values);
            }

            public async Task<ImmutableArray<Diagnostic>> AnalyzeDocumentAsync(Document document, CancellationToken cancellationToken)
            {
                ImmutableArray<Diagnostic> diagnostics = ImmutableArray<Diagnostic>.Empty;
                if (_compilationWithAnalyzers != null)
                {
                    DocumentId documentId = document.Id;
                    lock (_updateDiagnosticsLock)
                    {
                        if (!DocumentAnalyzed(documentId) && !CompletionStarted)
                        {
                            _analyzedDocuments[documentId] = Task.Run(async () =>
                            {
                                diagnostics = await GetDocumentDiagnosticsAsync(document, cancellationToken).ConfigureAwait(false);
                                DistributeDiagnostics(diagnostics);
                            }, cancellationToken);
                        }
                    }

                    if (DocumentAnalyzed(documentId))
                    {
                        await _analyzedDocuments[documentId].ConfigureAwait(false);
                    }
                }

                return diagnostics;
            }

            private async Task<ImmutableArray<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, CancellationToken cancellationToken)
            {
                SyntaxTree documentSyntax = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                SemanticModel documentModel = _compilationWithAnalyzers.Compilation.GetSemanticModel(documentSyntax);
                return await _compilationWithAnalyzers.GetAnalyzerDiagnosticsFromDocumentAsync(documentModel).ConfigureAwait(false);
            }

            public async Task<ImmutableArray<Diagnostic>> GetAllDiagnosticsAsync()
            {
                if (CompletionStarted)
                {
                    await _completionTask.ConfigureAwait(false);

                    ImmutableArray<Diagnostic>.Builder result = ImmutableArray.CreateBuilder<Diagnostic>();
                    lock (_updateDiagnosticsLock)
                    {
                        foreach (ImmutableArray<Diagnostic> diagnostics in _diagnosticsPerDocuments.Values)
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
                if (_compilationWithAnalyzers != null)
                {
                    if (DocumentAnalyzed(document.Id))
                    {
                        await _analyzedDocuments[document.Id].ConfigureAwait(false);
                    }
                    else if (CompletionStarted)
                    {
                        await _completionTask.ConfigureAwait(false);
                    }
                }

                ImmutableArray<Diagnostic> documentDiagnostics;
                if (_diagnosticsPerDocuments.TryGetValue(document.Id, out documentDiagnostics))
                {
                    // ToDo: filter out non-local diagnostics here. Otherwise, the set of diagnostics returned can vary
                    // depending on how much of a project has been analyzed prior to the request.
                    return documentDiagnostics;
                }

                return ImmutableArray<Diagnostic>.Empty;
            }

            public bool CompletedSuccessfully
            {
                get { return (CompletionStarted && _completionTask.IsCompleted && !_completionCanceled) || _compilationWithAnalyzers == null; }
            }

            // Divide a set of diagnostics into per-document groups.
            private void DistributeDiagnostics(ImmutableArray<Diagnostic> diagnostics)
            {
                lock (_updateDiagnosticsLock)
                {
                    ConcurrentDictionary<DocumentId, ImmutableArray<Diagnostic>> diagnosticMap = _diagnosticsPerDocuments;
                    foreach (Diagnostic diagnostic in diagnostics)
                    {
                        DocumentId diagnosticId = LocationId(diagnostic.Location);
                        ImmutableArray<Diagnostic> diagnosticsPerDocument;
                        if (diagnosticMap.TryGetValue(diagnosticId, out diagnosticsPerDocument))
                        {
                            diagnosticsPerDocument = diagnosticsPerDocument.Add(diagnostic);
                        }
                        else
                        {
                            diagnosticsPerDocument = ImmutableArray.Create(diagnostic);
                        }

                        diagnosticMap[diagnosticId] = diagnosticsPerDocument;
                    }
                }
            }

            private DocumentId LocationId(Location location)
            {
                if (location != null)
                {
                    SyntaxTree tree = location.SourceTree;
                    if (tree != null)
                    {
                        return _project.GetDocumentId(tree) ?? _noDocument;
                    }
                }

                return _noDocument;
            }
        }

        private class AnalyzerData<ContainerId> : IEquatable<AnalyzerData<ContainerId>>, ISupportLiveUpdate
        {
            public DiagnosticIncrementalAnalyzer Analyzer { get; private set; }
            public ContainerId Container { get; private set; }

            public AnalyzerData(DiagnosticIncrementalAnalyzer analyzer, ContainerId container)
            {
                Analyzer = analyzer;
                Container = container;
            }

            public bool Equals(AnalyzerData<ContainerId> other)
            {
                return s_compareAnalyzers.Equals(this.Analyzer, other.Analyzer)
                    && s_compareIds.Equals(this.Container, other.Container);
            }

            public override bool Equals(object obj)
            {
                AnalyzerData<ContainerId> other = obj as AnalyzerData<ContainerId>;
                if (((object)other) != null)
                {
                    return this.Equals(other);
                }

                return false;
            }

            public override int GetHashCode()
            {
                return Hash.Combine(s_compareAnalyzers.GetHashCode(Analyzer), s_compareIds.GetHashCode(Container));
            }

            public static bool operator ==(AnalyzerData<ContainerId> left, AnalyzerData<ContainerId> right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(AnalyzerData<ContainerId> left, AnalyzerData<ContainerId> right)
            {
                return !left.Equals(right);
            }

            private static readonly EqualityComparer<DiagnosticIncrementalAnalyzer> s_compareAnalyzers = EqualityComparer<DiagnosticIncrementalAnalyzer>.Default;
            private static readonly EqualityComparer<ContainerId> s_compareIds = EqualityComparer<ContainerId>.Default;
        }
    }
}
