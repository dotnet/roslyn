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
        private readonly DiagnosticAnalyzerService _owner;
        private readonly HostAnalyzerManager _hostAnalyzerManager;

        public DiagnosticIncrementalAnalyzer(DiagnosticAnalyzerService owner, int correlationId, Workspace workspace, HostAnalyzerManager hostAnalyzerManager, AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource)
            : base(workspace, hostDiagnosticUpdateSource)
        {
            _correlationId = correlationId;
            _owner = owner;
            _hostAnalyzerManager = hostAnalyzerManager;
        }

        #region IIncrementalAnalyzer
        public override Task AnalyzeDocumentAsync(Document document, SyntaxNode bodyOpt, CancellationToken cancellationToken)
        {
            return SpecializedTasks.EmptyTask;
        }

        public override async Task AnalyzeProjectAsync(Project project, bool semanticsChanged, CancellationToken cancellationToken)
        {
            var diagnostics = await GetDiagnosticsAsync(project.Solution, project.Id, null, cancellationToken).ConfigureAwait(false);

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

        private void RaiseEvents(Project project, ImmutableArray<DiagnosticData> diagnostics)
        {
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
    }
}
