// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
        private readonly int _correlationId;

        public DiagnosticIncrementalAnalyzer(DiagnosticAnalyzerService owner, int correlationId, Workspace workspace, HostAnalyzerManager hostAnalyzerManager, AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource)
            : base(owner, workspace, hostAnalyzerManager, hostDiagnosticUpdateSource)
        {
            _correlationId = correlationId;
        }

        #region IIncrementalAnalyzer
        public override Task AnalyzeDocumentAsync(Document document, SyntaxNode bodyOpt, CancellationToken cancellationToken)
        {
            return SpecializedTasks.EmptyTask;
        }

        public override async Task AnalyzeProjectAsync(Project project, bool semanticsChanged, CancellationToken cancellationToken)
        {
            var diagnostics = await GetDiagnosticsAsync(project.Solution, project.Id, null, includeSuppressedDiagnostics: true, cancellationToken: cancellationToken).ConfigureAwait(false);

            RaiseEvents(project, diagnostics);
        }

        public override Task AnalyzeSyntaxAsync(Document document, CancellationToken cancellationToken)
        {
            return SpecializedTasks.EmptyTask;
        }

        public override Task DocumentOpenAsync(Document document, CancellationToken cancellationToken)
        {
            return SpecializedTasks.EmptyTask;
        }

        public override Task DocumentCloseAsync(Document document, CancellationToken cancellationToken)
        {
            return SpecializedTasks.EmptyTask;
        }

        public override Task DocumentResetAsync(Document document, CancellationToken cancellationToken)
        {
            return SpecializedTasks.EmptyTask;
        }

        public override Task NewSolutionSnapshotAsync(Solution solution, CancellationToken cancellationToken)
        {
            return SpecializedTasks.EmptyTask;
        }

        public override void RemoveDocument(DocumentId documentId)
        {
            Owner.RaiseDiagnosticsUpdated(this, DiagnosticsUpdatedArgs.DiagnosticsRemoved(
                ValueTuple.Create(this, documentId), Workspace, null, null, null));
        }

        public override void RemoveProject(ProjectId projectId)
        {
            Owner.RaiseDiagnosticsUpdated(this, DiagnosticsUpdatedArgs.DiagnosticsRemoved(
                ValueTuple.Create(this, projectId), Workspace, null, null, null));
        }
        #endregion

        public override Task<ImmutableArray<DiagnosticData>> GetCachedDiagnosticsAsync(Solution solution, ProjectId projectId = null, DocumentId documentId = null, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetDiagnosticsAsync(solution, projectId, documentId, includeSuppressedDiagnostics, cancellationToken);
        }

        public override Task<ImmutableArray<DiagnosticData>> GetSpecificCachedDiagnosticsAsync(Solution solution, object id, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetSpecificDiagnosticsAsync(solution, id, includeSuppressedDiagnostics, cancellationToken);
        }

        public override async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(Solution solution, ProjectId projectId = null, DocumentId documentId = null, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (documentId != null)
            {
                var diagnostics = await GetProjectDiagnosticsAsync(solution.GetProject(projectId), includeSuppressedDiagnostics, cancellationToken).ConfigureAwait(false);
                return diagnostics.Where(d => d.DocumentId == documentId).ToImmutableArrayOrEmpty();
            }

            if (projectId != null)
            {
                return await GetProjectDiagnosticsAsync(solution.GetProject(projectId), includeSuppressedDiagnostics, cancellationToken).ConfigureAwait(false);
            }

            var builder = ImmutableArray.CreateBuilder<DiagnosticData>();
            foreach (var project in solution.Projects)
            {
                builder.AddRange(await GetProjectDiagnosticsAsync(project, includeSuppressedDiagnostics, cancellationToken).ConfigureAwait(false));
            }

            return builder.ToImmutable();
        }

        public override async Task<ImmutableArray<DiagnosticData>> GetSpecificDiagnosticsAsync(Solution solution, object id, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (id is ValueTuple<DiagnosticIncrementalAnalyzer, DocumentId>)
            {
                var key = (ValueTuple<DiagnosticIncrementalAnalyzer, DocumentId>)id;
                return await GetDiagnosticsAsync(solution, key.Item2.ProjectId, key.Item2, includeSuppressedDiagnostics, cancellationToken).ConfigureAwait(false);
            }

            if (id is ValueTuple<DiagnosticIncrementalAnalyzer, ProjectId>)
            {
                var key = (ValueTuple<DiagnosticIncrementalAnalyzer, ProjectId>)id;
                var diagnostics = await GetDiagnosticsAsync(solution, key.Item2, null, includeSuppressedDiagnostics, cancellationToken).ConfigureAwait(false);
                return diagnostics.Where(d => d.DocumentId == null).ToImmutableArray();
            }

            return ImmutableArray<DiagnosticData>.Empty;
        }

        public override async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForIdsAsync(Solution solution, ProjectId projectId = null, DocumentId documentId = null, ImmutableHashSet<string> diagnosticIds = null, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            var diagnostics = await GetDiagnosticsAsync(solution, projectId, documentId, includeSuppressedDiagnostics, cancellationToken).ConfigureAwait(false);
            return diagnostics.Where(d => diagnosticIds.Contains(d.Id)).ToImmutableArrayOrEmpty();
        }

        public override async Task<ImmutableArray<DiagnosticData>> GetProjectDiagnosticsForIdsAsync(Solution solution, ProjectId projectId = null, ImmutableHashSet<string> diagnosticIds = null, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            var diagnostics = await GetDiagnosticsForIdsAsync(solution, projectId, null, diagnosticIds, includeSuppressedDiagnostics, cancellationToken).ConfigureAwait(false);
            return diagnostics.Where(d => d.DocumentId == null).ToImmutableArray();
        }

        public override async Task<bool> TryAppendDiagnosticsForSpanAsync(Document document, TextSpan range, List<DiagnosticData> result, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            result.AddRange(await GetDiagnosticsForSpanAsync(document, range, includeSuppressedDiagnostics, cancellationToken).ConfigureAwait(false));
            return true;
        }

        public override async Task<IEnumerable<DiagnosticData>> GetDiagnosticsForSpanAsync(Document document, TextSpan range, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            var diagnostics = await GetDiagnosticsAsync(document.Project.Solution, document.Project.Id, document.Id, includeSuppressedDiagnostics, cancellationToken).ConfigureAwait(false);
            return diagnostics.Where(d => range.IntersectsWith(d.TextSpan));
        }

        private async Task<ImmutableArray<DiagnosticData>> GetProjectDiagnosticsAsync(Project project, bool includeSuppressedDiagnostics, CancellationToken cancellationToken)
        {
            if (project == null)
            {
                return ImmutableArray<DiagnosticData>.Empty;
            }

            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

            var analyzers = HostAnalyzerManager.CreateDiagnosticAnalyzers(project);

            var compilationWithAnalyzer = compilation.WithAnalyzers(analyzers, project.AnalyzerOptions, cancellationToken);

            // REVIEW: this API is a bit strange. 
            //         if getting diagnostic is cancelled, it has to create new compilation and do everything from scratch again?
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

        public override Task SynchronizeWithBuildAsync(DiagnosticAnalyzerService.BatchUpdateToken token, Project project, ImmutableArray<DiagnosticData> diagnostics)
        {
            // V2 engine doesn't do anything. 
            // it means live error always win over build errors. build errors that can't be reported by live analyzer
            // are already taken cared by engine
            return SpecializedTasks.EmptyTask;
        }

        public override Task SynchronizeWithBuildAsync(DiagnosticAnalyzerService.BatchUpdateToken token, Document document, ImmutableArray<DiagnosticData> diagnostics)
        {
            // V2 engine doesn't do anything. 
            // it means live error always win over build errors. build errors that can't be reported by live analyzer
            // are already taken cared by engine
            return SpecializedTasks.EmptyTask;
        }

        private void RaiseEvents(Project project, ImmutableArray<DiagnosticData> diagnostics)
        {
            var groups = diagnostics.GroupBy(d => d.DocumentId);

            var solution = project.Solution;
            var workspace = solution.Workspace;

            foreach (var kv in groups)
            {
                if (kv.Key == null)
                {
                    Owner.RaiseDiagnosticsUpdated(
                        this, DiagnosticsUpdatedArgs.DiagnosticsCreated(
                            ValueTuple.Create(this, project.Id), workspace, solution, project.Id, null, kv.ToImmutableArrayOrEmpty()));
                    continue;
                }

                Owner.RaiseDiagnosticsUpdated(
                    this, DiagnosticsUpdatedArgs.DiagnosticsCreated(
                        ValueTuple.Create(this, kv.Key), workspace, solution, project.Id, kv.Key, kv.ToImmutableArrayOrEmpty()));
            }
        }

        public override bool ContainsDiagnostics(Workspace workspace, ProjectId projectId)
        {
            // for now, it always return false;
            return false;
        }
    }
}
