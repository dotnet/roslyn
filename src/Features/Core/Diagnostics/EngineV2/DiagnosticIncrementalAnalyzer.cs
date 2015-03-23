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

            Project project = document.Project;
            VersionStamp projectVersion = await project.GetDependentVersionAsync(cancellationToken).ConfigureAwait(false);
            VersionStamp documentVersion = await document.GetTextVersionAsync(cancellationToken).ConfigureAwait(false);

            SemanticModel documentModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            Compilation compilation = documentModel.Compilation;
            CompilationDescriptor compilationDescriptor = GetCompilationDescriptor(compilation, project, projectVersion, cancellationToken);

            if (!compilationDescriptor.DocumentAnalyzed(documentVersion) && !compilationDescriptor.IsComplete)
            {
                ImmutableArray<Diagnostic> diagnostics = await compilationDescriptor.CompilationWithAnalyzers.GetDiagnosticsFromDocumentAsync(documentModel).ConfigureAwait(false);
                DistributeDiagnostics(project, compilationDescriptor, diagnostics);

                compilationDescriptor.MarkDocumentAnalyzed(documentVersion);
            }
        }

        public async override Task AnalyzeSyntaxAsync(Document document, CancellationToken cancellationToken)
        {
            Project project = document.Project;
            VersionStamp projectVersion = await project.GetDependentVersionAsync(cancellationToken).ConfigureAwait(false);
            VersionStamp documentVersion = await document.GetTextVersionAsync(cancellationToken).ConfigureAwait(false);

            // Come up with a way to run syntax tree analyzers exactly once, not duplicating analysis done for the project.
        }

        public override async Task AnalyzeProjectAsync(Project project, bool semanticsChanged, CancellationToken cancellationToken)
        {
            VersionStamp projectVersion = await project.GetDependentVersionAsync(cancellationToken).ConfigureAwait(false);

            Compilation compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            CompilationDescriptor compilationDescriptor = GetCompilationDescriptor(compilation, project, projectVersion, cancellationToken);

            if (!compilationDescriptor.IsComplete)
            {
                ImmutableArray<Diagnostic> diagnostics = await compilationDescriptor.CompilationWithAnalyzers.GetAllDiagnosticsAsync().ConfigureAwait(false);
                DistributeDiagnostics(project, compilationDescriptor, diagnostics);

                MarkComplete(project, compilationDescriptor);
            }
        }

        private void DistributeDiagnostics(Project project, CompilationDescriptor compilationDescriptor, ImmutableArray<Diagnostic> diagnostics)
        {
            compilationDescriptor.DistributeDiagnostics(diagnostics);
            RaiseEvents(project, diagnostics);
        }

        public override Task NewSolutionSnapshotAsync(Solution solution, CancellationToken cancellationToken)
        {
            return SpecializedTasks.EmptyTask;
        }

        // New above here.

        public override Task DocumentOpenAsync(Document document, CancellationToken cancellationToken)
        {
            return SpecializedTasks.EmptyTask;
        }

        public override Task DocumentResetAsync(Document document, CancellationToken cancellationToken)
        {
            return SpecializedTasks.EmptyTask;
        }

        public override void RemoveDocument(DocumentId documentId)
        {
            _owner.RaiseDiagnosticsUpdated(
                this, new DiagnosticsUpdatedArgs(ValueTuple.Create(this, documentId), Workspace, null, null, null, ImmutableArray<DiagnosticData>.Empty));
        }

        public override void RemoveProject(ProjectId projectId)
        {
            _owner.RaiseDiagnosticsUpdated(
                this, new DiagnosticsUpdatedArgs(ValueTuple.Create(this, projectId), Workspace, null, null, null, ImmutableArray<DiagnosticData>.Empty));
        }
        #endregion

        public override Task<ImmutableArray<DiagnosticData>> GetCachedDiagnosticsAsync(Solution solution, ProjectId projectId = null, DocumentId documentId = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetDiagnosticsAsync(solution, projectId, documentId, cancellationToken);
        }

        public override Task<ImmutableArray<DiagnosticData>> GetSpecificCachedDiagnosticsAsync(Solution solution, object id, CancellationToken cancellationToken)
        {
            return GetSpecificDiagnosticsAsync(solution, id, cancellationToken);
        }

        public override async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(Solution solution, ProjectId projectId = null, DocumentId documentId = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (documentId != null)
            {
                var diagnostics = await GetProjectDiagnosticsAsync(solution.GetProject(projectId), cancellationToken).ConfigureAwait(false);
                return diagnostics.Where(d => d.DocumentId == documentId).ToImmutableArrayOrEmpty();
            }

            if (projectId != null)
            {
                return await GetProjectDiagnosticsAsync(solution.GetProject(projectId), cancellationToken).ConfigureAwait(false);
            }

            var builder = ImmutableArray.CreateBuilder<DiagnosticData>();
            foreach (var project in solution.Projects)
            {
                builder.AddRange(await GetProjectDiagnosticsAsync(project, cancellationToken).ConfigureAwait(false));
            }

            return builder.ToImmutable();
        }

        public override async Task<ImmutableArray<DiagnosticData>> GetSpecificDiagnosticsAsync(Solution solution, object id, CancellationToken cancellationToken)
        {
            if (id is ValueTuple<DiagnosticIncrementalAnalyzer, DocumentId>)
            {
                var key = (ValueTuple<DiagnosticIncrementalAnalyzer, DocumentId>)id;
                return await GetDiagnosticsAsync(solution, key.Item2.ProjectId, key.Item2, cancellationToken).ConfigureAwait(false);
            }

            if (id is ValueTuple<DiagnosticIncrementalAnalyzer, ProjectId>)
            {
                var key = (ValueTuple<DiagnosticIncrementalAnalyzer, ProjectId>)id;
                var diagnostics = await GetDiagnosticsAsync(solution, key.Item2, null, cancellationToken).ConfigureAwait(false);
                return diagnostics.Where(d => d.DocumentId == null).ToImmutableArray();
            }

            return ImmutableArray<DiagnosticData>.Empty;
        }

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

        private class CompilationDescriptors
        {
            public CompilationDescriptor OldCompilation = new CompilationDescriptor(VersionStamp.Default, null);
            public CompilationDescriptor NewCompilation = new CompilationDescriptor(VersionStamp.Default, null);
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

                CompilationDescriptor oldCompilation = descriptors.OldCompilation;
                if (oldCompilation.ProjectVersion == projectVersion)
                {
                    return oldCompilation;
                }

                CompilationDescriptor newCompilation = descriptors.NewCompilation;
                if (newCompilation.ProjectVersion != projectVersion)
                {
                    CompilationWithAnalyzers newerCompilationWithAnalyzers = compilation.WithAnalyzers(Flatten(_hostAnalyzerManager.GetHostDiagnosticAnalyzersPerReference(project.Language).Values), project.AnalyzerOptions, cancellationToken);
                    CompilationDescriptor newerCompilation = new CompilationDescriptor(projectVersion, newerCompilationWithAnalyzers);
                    
                    descriptors.NewCompilation = newerCompilation;
                }

                return newCompilation;
            }
        }

        private void MarkComplete(Project project, CompilationDescriptor compilationDescriptor)
        {
            lock (_compilationDescriptors)
            {
                compilationDescriptor.MarkComplete();
                _compilationDescriptors[project.Id].OldCompilation = compilationDescriptor;
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
            public CompilationWithAnalyzers CompilationWithAnalyzers { get; private set; }
            public ConcurrentDictionary<string, ImmutableArray<Diagnostic>> DiagnosticsPerPaths { get; private set; }
            public bool IsComplete { get; private set; }
            private readonly ConcurrentDictionary<VersionStamp, bool> _analyzedDocuments = new ConcurrentDictionary<VersionStamp, bool>();
            private readonly object UpdateDiagnosticsLock = new object();

            public CompilationDescriptor(VersionStamp projectVersion, CompilationWithAnalyzers compilation)
            {
                this.CompilationWithAnalyzers = compilation;
                this.ProjectVersion = projectVersion;
                this.DiagnosticsPerPaths = new ConcurrentDictionary<string, ImmutableArray<Diagnostic>>();
            }

            public bool DocumentAnalyzed(VersionStamp documentVersion)
            {
                return _analyzedDocuments.ContainsKey(documentVersion);
            }

            public void MarkDocumentAnalyzed(VersionStamp documentVersion)
            {
                _analyzedDocuments[documentVersion] = true;
            }

            public void DistributeDiagnostics(ImmutableArray<Diagnostic> diagnostics)
            {
                lock (this.UpdateDiagnosticsLock)
                {
                    ConcurrentDictionary<string, ImmutableArray<Diagnostic>> diagnosticMap = this.DiagnosticsPerPaths;
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

            public void MarkComplete()
            {
                IsComplete = true;
                this.CompilationWithAnalyzers = null;
            }
        }
    }
}
