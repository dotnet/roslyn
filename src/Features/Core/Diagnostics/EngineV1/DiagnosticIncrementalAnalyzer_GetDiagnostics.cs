// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics.Log;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV1
{
    internal partial class DiagnosticIncrementalAnalyzer
    {
        public override Task<ImmutableArray<DiagnosticData>> GetSpecificCachedDiagnosticsAsync(Solution solution, object id, CancellationToken cancellationToken)
        {
            return new IDECachedDiagnosticGetter(this).GetSpecificDiagnosticsAsync(solution, id, cancellationToken);
        }

        public override Task<ImmutableArray<DiagnosticData>> GetCachedDiagnosticsAsync(Solution solution, ProjectId projectId, DocumentId documentId, CancellationToken cancellationToken)
        {
            return new IDECachedDiagnosticGetter(this).GetDiagnosticsAsync(solution, projectId, documentId, cancellationToken);
        }

        public override Task<ImmutableArray<DiagnosticData>> GetSpecificDiagnosticsAsync(Solution solution, object id, CancellationToken cancellationToken)
        {
            return new IDELatestDiagnosticGetter(this).GetSpecificDiagnosticsAsync(solution, id, cancellationToken);
        }

        public override Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(Solution solution, ProjectId projectId, DocumentId documentId, CancellationToken cancellationToken)
        {
            return new IDELatestDiagnosticGetter(this).GetDiagnosticsAsync(solution, projectId, documentId, cancellationToken);
        }

        public override Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForIdsAsync(Solution solution, ProjectId projectId, DocumentId documentId, ImmutableHashSet<string> diagnosticIds, CancellationToken cancellationToken)
        {
            return new IDELatestDiagnosticGetter(this, diagnosticIds).GetDiagnosticsAsync(solution, projectId, documentId, cancellationToken);
        }

        public override Task<ImmutableArray<DiagnosticData>> GetProjectDiagnosticsForIdsAsync(Solution solution, ProjectId projectId, ImmutableHashSet<string> diagnosticIds, CancellationToken cancellationToken)
        {
            return new IDELatestDiagnosticGetter(this, diagnosticIds).GetProjectDiagnosticsAsync(solution, projectId, cancellationToken);
        }

        private Task ReanalyzeAllDocumentsAsync(Project project, ImmutableHashSet<string> diagnosticIds, CancellationToken cancellationToken)
        {
            return new ReanalysisDiagnosticGetter(this, diagnosticIds).ReanalyzeAllDocuments(project, cancellationToken);
        }

        private abstract class DiagnosticsGetter
        {
            protected readonly DiagnosticIncrementalAnalyzer Owner;

            private ImmutableArray<DiagnosticData>.Builder _builder;

            public DiagnosticsGetter(DiagnosticIncrementalAnalyzer owner)
            {
                this.Owner = owner;
            }

            protected StateManager StateManager
            {
                get { return this.Owner._stateManger; }
            }

            protected abstract Task AppendDiagnosticsFromKeyAsync(Project project, StateType stateType, object documentOrProject, CancellationToken cancellationToken);

            public async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(Solution solution, ProjectId projectId, DocumentId documentId, CancellationToken cancellationToken)
            {
                if (solution == null)
                {
                    return GetDiagnosticData();
                }

                if (documentId != null)
                {
                    await AppendDiagnosticsAsync(solution.GetDocument(documentId), cancellationToken).ConfigureAwait(false);
                    return GetDiagnosticData();
                }

                if (projectId != null)
                {
                    await AppendDiagnosticsAsync(solution.GetProject(projectId), cancellationToken: cancellationToken).ConfigureAwait(false);
                    return GetDiagnosticData();
                }

                await AppendDiagnosticsAsync(solution, cancellationToken: cancellationToken).ConfigureAwait(false);
                return GetDiagnosticData();
            }

            public async Task<ImmutableArray<DiagnosticData>> GetProjectDiagnosticsAsync(Solution solution, ProjectId projectId, CancellationToken cancellationToken)
            {
                if (solution == null)
                {
                    return GetDiagnosticData();
                }

                if (projectId != null)
                {
                    await AppendProjectDiagnosticsAsync(solution.GetProject(projectId), cancellationToken: cancellationToken).ConfigureAwait(false);
                    return GetDiagnosticData();
                }

                await AppendProjectDiagnosticsAsync(solution, cancellationToken).ConfigureAwait(false);
                return GetDiagnosticData();
            }

            private async Task AppendProjectDiagnosticsAsync(Solution solution, CancellationToken cancellationToken)
            {
                if (solution == null)
                {
                    return;
                }

                foreach (var project in solution.Projects)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await AppendProjectDiagnosticsAsync(project, cancellationToken).ConfigureAwait(false);
                }
            }

            private Task AppendProjectDiagnosticsAsync(Project project, CancellationToken cancellationToken)
            {
                if (project == null)
                {
                    return SpecializedTasks.EmptyTask;
                }

                return AppendDiagnosticsFromKeyAsync(project, StateType.Project, project, cancellationToken);
            }

            private async Task AppendDiagnosticsAsync(Solution solution, CancellationToken cancellationToken)
            {
                if (solution == null)
                {
                    return;
                }

                foreach (var project in solution.Projects)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await AppendDiagnosticsAsync(project, cancellationToken).ConfigureAwait(false);
                }
            }

            private async Task AppendDiagnosticsAsync(Project project, CancellationToken cancellationToken)
            {
                if (project == null)
                {
                    return;
                }

                await AppendProjectDiagnosticsAsync(project, cancellationToken).ConfigureAwait(false);

                foreach (var document in project.Documents)
                {
                    await AppendDiagnosticsAsync(document, cancellationToken).ConfigureAwait(false);
                }
            }

            protected async Task AppendDiagnosticsAsync(Document document, CancellationToken cancellationToken)
            {
                if (document == null)
                {
                    return;
                }

                await AppendDiagnosticsFromKeyAsync(document.Project, StateType.Syntax, document, cancellationToken).ConfigureAwait(false);
                await AppendDiagnosticsFromKeyAsync(document.Project, StateType.Document, document, cancellationToken).ConfigureAwait(false);
            }

            protected void AppendDiagnostics(IEnumerable<DiagnosticData> items)
            {
                _builder = _builder ?? ImmutableArray.CreateBuilder<DiagnosticData>();
                _builder.AddRange(items);
            }

            protected ImmutableArray<DiagnosticData> GetDiagnosticData()
            {
                return _builder != null ? _builder.ToImmutable() : ImmutableArray<DiagnosticData>.Empty;
            }

            protected static ProjectId GetProjectId(object key)
            {
                var documentId = key as DocumentId;
                if (documentId != null)
                {
                    return documentId.ProjectId;
                }

                var projectId = key as ProjectId;
                if (projectId != null)
                {
                    return projectId;
                }

                return Contract.FailWithReturn<ProjectId>("Shouldn't reach here");
            }

            protected static object GetProjectOrDocument(Solution solution, object key)
            {
                var documentId = key as DocumentId;
                if (documentId != null)
                {
                    return solution.GetDocument(documentId);
                }

                var projectId = key as ProjectId;
                if (projectId != null)
                {
                    return solution.GetProject(projectId);
                }

                return Contract.FailWithReturn<object>("Shouldn't reach here");
            }
        }

        private class IDECachedDiagnosticGetter : DiagnosticsGetter
        {
            public IDECachedDiagnosticGetter(DiagnosticIncrementalAnalyzer owner) : base(owner)
            {
            }

            public async Task<ImmutableArray<DiagnosticData>> GetSpecificDiagnosticsAsync(Solution solution, object id, CancellationToken cancellationToken)
            {
                if (solution == null)
                {
                    return ImmutableArray<DiagnosticData>.Empty;
                }

                var key = id as ArgumentKey;
                if (key == null)
                {
                    return ImmutableArray<DiagnosticData>.Empty;
                }

                var projectId = GetProjectId(key.Key);
                var project = solution.GetProject(projectId);

                // when we return cached result, make sure we at least return something that exist in current solution
                if (project == null)
                {
                    return ImmutableArray<DiagnosticData>.Empty;
                }

                var stateSet = this.StateManager.GetOrCreateStateSet(project, key.Analyzer);
                if (stateSet == null)
                {
                    return ImmutableArray<DiagnosticData>.Empty;
                }

                var state = stateSet.GetState(key.StateTypeId);
                if (state == null)
                {
                    return ImmutableArray<DiagnosticData>.Empty;
                }

                // for now, it just use wait and get result
                var documentOrProject = GetProjectOrDocument(solution, key.Key);
                if (documentOrProject == null)
                {
                    // Document or project might have been removed from the solution.
                    return ImmutableArray<DiagnosticData>.Empty;
                }

                var existingData = await state.TryGetExistingDataAsync(documentOrProject, cancellationToken).ConfigureAwait(false);
                if (existingData == null)
                {
                    return ImmutableArray<DiagnosticData>.Empty;
                }

                return existingData.Items;
            }

            protected override async Task AppendDiagnosticsFromKeyAsync(Project project, StateType stateType, object documentOrProject, CancellationToken cancellationToken)
            {
                foreach (var stateSet in this.StateManager.GetStateSets(project))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var state = stateSet.GetState(stateType);

                    // for now, it just use wait and get result
                    var existingData = await state.TryGetExistingDataAsync(documentOrProject, cancellationToken).ConfigureAwait(false);
                    if (existingData == null)
                    {
                        continue;
                    }

                    AppendDiagnostics(existingData.Items);
                }
            }
        }

        private abstract class LatestDiagnosticsGetter : DiagnosticsGetter
        {
            protected readonly ImmutableHashSet<string> DiagnosticIds;

            public LatestDiagnosticsGetter(DiagnosticIncrementalAnalyzer owner, ImmutableHashSet<string> diagnosticIds) : base(owner)
            {
                this.DiagnosticIds = diagnosticIds;
            }

            protected abstract Task<AnalysisData> GetSpecificDiagnosticsAsync(Solution solution, DiagnosticAnalyzerDriver analyzerDriver, StateSet stateSet, StateType stateType, VersionArgument versions);
            protected abstract void FilterDiagnostics(AnalysisData analysisData);

            protected override async Task AppendDiagnosticsFromKeyAsync(Project project, StateType stateType, object documentOrProject, CancellationToken cancellationToken)
            {
                Contract.ThrowIfNull(project);
                var solution = GetSolution(documentOrProject);

                var driver = await GetDiagnosticAnalyzerDriverAsync(documentOrProject, cancellationToken).ConfigureAwait(false);
                var versions = await GetVersionsAsync(stateType, documentOrProject, cancellationToken).ConfigureAwait(false);

                foreach (var stateSet in this.StateManager.GetOrCreateStateSets(project))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (driver.IsAnalyzerSuppressed(stateSet.Analyzer) ||
                        !this.Owner.ShouldRunAnalyzerForStateType(driver, stateSet.Analyzer, stateType, this.DiagnosticIds))
                    {
                        continue;
                    }

                    var analysisData = await GetSpecificDiagnosticsAsync(solution, driver, stateSet, stateType, versions).ConfigureAwait(false);
                    FilterDiagnostics(analysisData);
                }
            }

            protected async Task<DiagnosticAnalyzerDriver> GetDiagnosticAnalyzerDriverAsync(object documentOrProject, CancellationToken cancellationToken)
            {
                var document = documentOrProject as Document;
                if (document != null)
                {
                    var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                    return new DiagnosticAnalyzerDriver(document, root.FullSpan, root, this.DiagnosticLogAggregator, this.Owner.HostDiagnosticUpdateSource, cancellationToken);
                }

                var project = documentOrProject as Project;
                if (project != null)
                {
                    return new DiagnosticAnalyzerDriver(project, this.DiagnosticLogAggregator, this.Owner.HostDiagnosticUpdateSource, cancellationToken);
                }

                return Contract.FailWithReturn<DiagnosticAnalyzerDriver>("Can't reach here");
            }

            protected async Task<VersionArgument> GetVersionsAsync(StateType stateType, object documentOrProject, CancellationToken cancellationToken)
            {
                switch (stateType)
                {
                    case StateType.Syntax:
                        {
                            var document = (Document)documentOrProject;
                            var textVersion = await document.GetTextVersionAsync(cancellationToken).ConfigureAwait(false);
                            var syntaxVersion = await document.GetSyntaxVersionAsync(cancellationToken).ConfigureAwait(false);
                            return new VersionArgument(textVersion, syntaxVersion);
                        }

                    case StateType.Document:
                        {
                            var document = (Document)documentOrProject;
                            var textVersion = await document.GetTextVersionAsync(cancellationToken).ConfigureAwait(false);
                            var semanticVersion = await document.Project.GetDependentSemanticVersionAsync(cancellationToken).ConfigureAwait(false);
                            return new VersionArgument(textVersion, semanticVersion);
                        }

                    case StateType.Project:
                        {
                            var project = (Project)documentOrProject;
                            var semanticVersion = await project.GetDependentSemanticVersionAsync(cancellationToken).ConfigureAwait(false);
                            var projectVersion = await project.GetDependentVersionAsync(cancellationToken).ConfigureAwait(false);
                            return new VersionArgument(VersionStamp.Default, semanticVersion, projectVersion);
                        }

                    default:
                        return Contract.FailWithReturn<VersionArgument>("Can't reach here");
                }
            }

            private Solution GetSolution(object value)
            {
                var document = value as Document;
                if (document != null)
                {
                    return document.Project.Solution;
                }

                var project = value as Project;
                if (project != null)
                {
                    return project.Solution;
                }

                return Contract.FailWithReturn<Solution>("Can't reach here");
            }

            private DiagnosticLogAggregator DiagnosticLogAggregator
            {
                get { return this.Owner._diagnosticLogAggregator; }
            }
        }

        private class ReanalysisDiagnosticGetter : LatestDiagnosticsGetter
        {
            public ReanalysisDiagnosticGetter(DiagnosticIncrementalAnalyzer owner, ImmutableHashSet<string> diagnosticIds) : base(owner, diagnosticIds)
            {
            }

            public async Task ReanalyzeAllDocuments(Project project, CancellationToken cancellationToken)
            {
                foreach (var document in project.Documents)
                {
                    await AppendDiagnosticsAsync(document, cancellationToken).ConfigureAwait(false);
                }
            }

            protected override void FilterDiagnostics(AnalysisData analysisData)
            {
                // we don't care about result
                return;
            }

            protected override async Task<AnalysisData> GetSpecificDiagnosticsAsync(
                Solution solution, DiagnosticAnalyzerDriver analyzerDriver, StateSet stateSet, StateType stateType, VersionArgument versions)
            {
                // we don't care about result
                switch (stateType)
                {
                    case StateType.Syntax:
                        await GetSyntaxDiagnosticsAsync(analyzerDriver, stateSet.Analyzer).ConfigureAwait(false);
                        break;
                    case StateType.Document:
                        await GetSemanticDiagnosticsAsync(analyzerDriver, stateSet.Analyzer).ConfigureAwait(false);
                        break;
                    case StateType.Project:
                    default:
                        return Contract.FailWithReturn<AnalysisData>("Can't reach here");
                }

                return AnalysisData.Empty;
            }
        }

        private class IDELatestDiagnosticGetter : LatestDiagnosticsGetter
        {
            public IDELatestDiagnosticGetter(DiagnosticIncrementalAnalyzer owner) : this(owner, null)
            {
            }

            public IDELatestDiagnosticGetter(DiagnosticIncrementalAnalyzer owner, ImmutableHashSet<string> diagnosticIds) : base(owner, diagnosticIds)
            {
            }

            public async Task<ImmutableArray<DiagnosticData>> GetSpecificDiagnosticsAsync(Solution solution, object id, CancellationToken cancellationToken)
            {
                if (solution == null)
                {
                    return ImmutableArray<DiagnosticData>.Empty;
                }

                var key = id as ArgumentKey;
                if (key == null)
                {
                    return ImmutableArray<DiagnosticData>.Empty;
                }

                var documentOrProject = GetProjectOrDocument(solution, key.Key);
                if (documentOrProject == null)
                {
                    // Document or project might have been removed from the solution.
                    return ImmutableArray<DiagnosticData>.Empty;
                }

                var project = solution.GetProject(GetProjectId(key.Key));

                var driver = await GetDiagnosticAnalyzerDriverAsync(documentOrProject, cancellationToken).ConfigureAwait(false);
                var versions = await GetVersionsAsync(key.StateTypeId, documentOrProject, cancellationToken).ConfigureAwait(false);

                var stateSet = this.StateManager.GetOrCreateStateSet(project, key.Analyzer);
                if (stateSet == null)
                {
                    return ImmutableArray<DiagnosticData>.Empty;
                }

                var analysisData = await GetSpecificDiagnosticsAsync(solution, driver, stateSet, key.StateTypeId, versions).ConfigureAwait(false);
                return analysisData.Items;
            }

            protected override void FilterDiagnostics(AnalysisData analysisData)
            {
                AppendDiagnostics(analysisData.Items.Where(d => this.DiagnosticIds == null || this.DiagnosticIds.Contains(d.Id)));
            }

            protected override Task<AnalysisData> GetSpecificDiagnosticsAsync(
                Solution solution, DiagnosticAnalyzerDriver analyzerDriver, StateSet stateSet, StateType stateType, VersionArgument versions)
            {
                switch (stateType)
                {
                    case StateType.Syntax:
                        return this.AnalyzerExecutor.GetSyntaxAnalysisDataAsync(analyzerDriver, stateSet, versions);
                    case StateType.Document:
                        return this.AnalyzerExecutor.GetDocumentAnalysisDataAsync(analyzerDriver, stateSet, versions);
                    case StateType.Project:
                        return this.AnalyzerExecutor.GetProjectAnalysisDataAsync(analyzerDriver, stateSet, versions);
                    default:
                        return Contract.FailWithReturn<Task<AnalysisData>>("Can't reach here");
                }
            }

            protected AnalyzerExecutor AnalyzerExecutor
            {
                get { return this.Owner._executor; }
            }
        }
    }
}
