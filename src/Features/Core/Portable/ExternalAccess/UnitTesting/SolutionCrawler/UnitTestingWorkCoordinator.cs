// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.SolutionCrawler;

internal partial class UnitTestingSolutionCrawlerRegistrationService
{
    internal sealed partial class UnitTestingWorkCoordinator : IUnitTestingWorkCoordinator
    {
        private readonly UnitTestingRegistration _registration;

        private readonly CountLogAggregator<WorkspaceChangeKind> _logAggregator = new();
        private readonly IAsynchronousOperationListener _listener;
        private readonly Microsoft.CodeAnalysis.SolutionCrawler.ISolutionCrawlerOptionsService? _solutionCrawlerOptionsService;

        private readonly CancellationTokenSource _shutdownNotificationSource = new();
        private readonly CancellationToken _shutdownToken;
        private readonly TaskQueue _eventProcessingQueue;

        // points to processor task
        private readonly UnitTestingIncrementalAnalyzerProcessor _documentAndProjectWorkerProcessor;
        private readonly UnitTestingSemanticChangeProcessor _semanticChangeProcessor;

        public UnitTestingWorkCoordinator(
             IAsynchronousOperationListener listener,
             IEnumerable<Lazy<IUnitTestingIncrementalAnalyzerProvider, UnitTestingIncrementalAnalyzerProviderMetadata>> analyzerProviders,
             UnitTestingRegistration registration)
        {
            _registration = registration;

            _listener = listener;
            _solutionCrawlerOptionsService = _registration.Services.GetService<Microsoft.CodeAnalysis.SolutionCrawler.ISolutionCrawlerOptionsService>();

            // event and worker queues
            _shutdownToken = _shutdownNotificationSource.Token;

            _eventProcessingQueue = new TaskQueue(listener, TaskScheduler.Default);

            var allFilesWorkerBackOffTimeSpan = UnitTestingSolutionCrawlerTimeSpan.AllFilesWorkerBackOff;
            var entireProjectWorkerBackOffTimeSpan = UnitTestingSolutionCrawlerTimeSpan.EntireProjectWorkerBackOff;

            _documentAndProjectWorkerProcessor = new UnitTestingIncrementalAnalyzerProcessor(
                listener,
                analyzerProviders,
                _registration,
                allFilesWorkerBackOffTimeSpan,
                entireProjectWorkerBackOffTimeSpan,
                _shutdownToken);

            var semanticBackOffTimeSpan = UnitTestingSolutionCrawlerTimeSpan.SemanticChangeBackOff;
            var projectBackOffTimeSpan = UnitTestingSolutionCrawlerTimeSpan.ProjectPropagationBackOff;

            _semanticChangeProcessor = new UnitTestingSemanticChangeProcessor(listener, _registration, _documentAndProjectWorkerProcessor, semanticBackOffTimeSpan, projectBackOffTimeSpan, _shutdownToken);
        }

        public UnitTestingRegistration Registration => _registration;
        public int CorrelationId => _registration.CorrelationId;

        public void AddAnalyzer(IUnitTestingIncrementalAnalyzer analyzer)
        {
            // add analyzer
            _documentAndProjectWorkerProcessor.AddAnalyzer(analyzer);

            // and ask to re-analyze whole solution for the given analyzer
            var scope = new UnitTestingReanalyzeScope(_registration.GetSolutionToAnalyze().Id);
            Reanalyze(analyzer, scope);
        }

        public void Reanalyze(IUnitTestingIncrementalAnalyzer analyzer, UnitTestingReanalyzeScope scope)
        {
            _eventProcessingQueue.ScheduleTask("Reanalyze",
                () => EnqueueWorkItemAsync(analyzer, scope), _shutdownToken);

            if (scope.HasMultipleDocuments)
            {
                // log big reanalysis request from things like fix all, suppress all or option changes
                // we are not interested in 1 file re-analysis request which can happen from like venus typing
                var solution = _registration.GetSolutionToAnalyze();
                UnitTestingSolutionCrawlerLogger.LogReanalyze(
                    CorrelationId, analyzer, scope.GetDocumentCount(solution), scope.GetLanguagesStringForTelemetry(solution));
            }
        }

        public void OnWorkspaceChanged(WorkspaceChangeEventArgs args)
        {
            // guard us from cancellation
            try
            {
                ProcessEvent(args, "OnWorkspaceChanged");
            }
            catch (OperationCanceledException oce)
            {
                if (NotOurShutdownToken(oce))
                {
                    throw;
                }

                // it is our cancellation, ignore
            }
            catch (AggregateException ae)
            {
                ae = ae.Flatten();

                // If we had a mix of exceptions, don't eat it
                if (ae.InnerExceptions.Any(e => e is not OperationCanceledException) ||
                    ae.InnerExceptions.Cast<OperationCanceledException>().Any(NotOurShutdownToken))
                {
                    // We had a cancellation with a different token, so don't eat it
                    throw;
                }

                // it is our cancellation, ignore
            }
        }

        private bool NotOurShutdownToken(OperationCanceledException oce)
            => oce.CancellationToken == _shutdownToken;

        private void ProcessEvent(WorkspaceChangeEventArgs args, string eventName)
        {
            UnitTestingSolutionCrawlerLogger.LogWorkspaceEvent(_logAggregator, args.Kind);

            // TODO: add telemetry that record how much it takes to process an event (max, min, average and etc)
            switch (args.Kind)
            {
                case WorkspaceChangeKind.SolutionAdded:
                    EnqueueFullSolutionEvent(args.NewSolution, UnitTestingInvocationReasons.DocumentAdded, eventName);
                    break;

                case WorkspaceChangeKind.SolutionChanged:
                case WorkspaceChangeKind.SolutionReloaded:
                    EnqueueSolutionChangedEvent(args.OldSolution, args.NewSolution, eventName);
                    break;

                case WorkspaceChangeKind.SolutionCleared:
                case WorkspaceChangeKind.SolutionRemoved:
                    // Not used in unit testing crawling
                    break;

                case WorkspaceChangeKind.ProjectAdded:
                    Contract.ThrowIfNull(args.ProjectId);
                    EnqueueFullProjectEvent(args.NewSolution, args.ProjectId, UnitTestingInvocationReasons.DocumentAdded, eventName);
                    break;

                case WorkspaceChangeKind.ProjectChanged:
                case WorkspaceChangeKind.ProjectReloaded:
                    Contract.ThrowIfNull(args.ProjectId);
                    EnqueueProjectChangedEvent(args.OldSolution, args.NewSolution, args.ProjectId, eventName);
                    break;

                case WorkspaceChangeKind.ProjectRemoved:
                    Contract.ThrowIfNull(args.ProjectId);
                    EnqueueFullProjectEvent(args.OldSolution, args.ProjectId, UnitTestingInvocationReasons.DocumentRemoved, eventName);
                    break;

                case WorkspaceChangeKind.DocumentAdded:
                    Contract.ThrowIfNull(args.DocumentId);
                    EnqueueFullDocumentEvent(args.NewSolution, args.DocumentId, UnitTestingInvocationReasons.DocumentAdded, eventName);
                    break;

                case WorkspaceChangeKind.DocumentReloaded:
                case WorkspaceChangeKind.DocumentChanged:
                    Contract.ThrowIfNull(args.DocumentId);
                    EnqueueDocumentChangedEvent(args.OldSolution, args.NewSolution, args.DocumentId, eventName);
                    break;

                case WorkspaceChangeKind.DocumentRemoved:
                    Contract.ThrowIfNull(args.DocumentId);
                    EnqueueFullDocumentEvent(args.OldSolution, args.DocumentId, UnitTestingInvocationReasons.DocumentRemoved, eventName);
                    break;

                case WorkspaceChangeKind.AdditionalDocumentAdded:
                case WorkspaceChangeKind.AdditionalDocumentRemoved:
                case WorkspaceChangeKind.AdditionalDocumentChanged:
                case WorkspaceChangeKind.AdditionalDocumentReloaded:
                case WorkspaceChangeKind.AnalyzerConfigDocumentAdded:
                case WorkspaceChangeKind.AnalyzerConfigDocumentRemoved:
                case WorkspaceChangeKind.AnalyzerConfigDocumentChanged:
                case WorkspaceChangeKind.AnalyzerConfigDocumentReloaded:
                    // If an additional file or .editorconfig has changed we need to reanalyze the entire project.
                    Contract.ThrowIfNull(args.ProjectId);
                    EnqueueFullProjectEvent(args.NewSolution, args.ProjectId, UnitTestingInvocationReasons.AdditionalDocumentChanged, eventName);
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(args.Kind);
            }
        }

        private void EnqueueSolutionChangedEvent(Solution oldSolution, Solution newSolution, string eventName)
        {
            _eventProcessingQueue.ScheduleTask(
                eventName,
                async () =>
                {
                    var solutionChanges = newSolution.GetChanges(oldSolution);

                    // TODO: Async version for GetXXX methods?
                    foreach (var addedProject in solutionChanges.GetAddedProjects())
                    {
                        await EnqueueFullProjectWorkItemAsync(addedProject, UnitTestingInvocationReasons.DocumentAdded).ConfigureAwait(false);
                    }

                    foreach (var projectChanges in solutionChanges.GetProjectChanges())
                    {
                        await EnqueueWorkItemAsync(projectChanges).ConfigureAwait(continueOnCapturedContext: false);
                    }

                    foreach (var removedProject in solutionChanges.GetRemovedProjects())
                    {
                        await EnqueueFullProjectWorkItemAsync(removedProject, UnitTestingInvocationReasons.DocumentRemoved).ConfigureAwait(false);
                    }
                },
                _shutdownToken);
        }

        private void EnqueueFullSolutionEvent(Solution solution, UnitTestingInvocationReasons invocationReasons, string eventName)
        {
            _eventProcessingQueue.ScheduleTask(
                eventName,
                async () =>
                {
                    foreach (var projectId in solution.ProjectIds)
                    {
                        await EnqueueFullProjectWorkItemAsync(solution.GetRequiredProject(projectId), invocationReasons).ConfigureAwait(false);
                    }
                },
                _shutdownToken);
        }

        private void EnqueueProjectChangedEvent(Solution oldSolution, Solution newSolution, ProjectId projectId, string eventName)
        {
            _eventProcessingQueue.ScheduleTask(
                eventName,
                async () =>
                {
                    var oldProject = oldSolution.GetRequiredProject(projectId);
                    var newProject = newSolution.GetRequiredProject(projectId);

                    await EnqueueWorkItemAsync(newProject.GetChanges(oldProject)).ConfigureAwait(false);
                },
                _shutdownToken);
        }

        private void EnqueueFullProjectEvent(Solution solution, ProjectId projectId, UnitTestingInvocationReasons invocationReasons, string eventName)
        {
            _eventProcessingQueue.ScheduleTask(eventName,
                () => EnqueueFullProjectWorkItemAsync(solution.GetRequiredProject(projectId), invocationReasons), _shutdownToken);
        }

        private void EnqueueFullDocumentEvent(Solution solution, DocumentId documentId, UnitTestingInvocationReasons invocationReasons, string eventName)
        {
            _eventProcessingQueue.ScheduleTask(
                eventName,
                () =>
                {
                    var project = solution.GetRequiredProject(documentId.ProjectId);
                    return EnqueueDocumentWorkItemAsync(project, documentId, document: null, invocationReasons);
                },
                _shutdownToken);
        }

        private void EnqueueDocumentChangedEvent(Solution oldSolution, Solution newSolution, DocumentId documentId, string eventName)
        {
            // document changed event is the special one.
            _eventProcessingQueue.ScheduleTask(
                eventName,
                async () =>
                {
                    var oldProject = oldSolution.GetRequiredProject(documentId.ProjectId);
                    var newProject = newSolution.GetRequiredProject(documentId.ProjectId);

                    await EnqueueChangedDocumentWorkItemAsync(oldProject.GetRequiredDocument(documentId), newProject.GetRequiredDocument(documentId)).ConfigureAwait(false);

                    // If all features are enabled for source generated documents, the solution crawler needs to
                    // include them in incremental analysis.
                    if (_solutionCrawlerOptionsService?.EnableDiagnosticsInSourceGeneratedFiles == true)
                    {
                        // TODO: if this becomes a hot spot, we should be able to expose/access the dictionary
                        // underneath GetSourceGeneratedDocumentsAsync rather than create a new one here.
                        var oldProjectSourceGeneratedDocuments = await oldProject.GetSourceGeneratedDocumentsAsync(_shutdownToken).ConfigureAwait(false);
                        var oldProjectSourceGeneratedDocumentsById = oldProjectSourceGeneratedDocuments.ToDictionary(static document => document.Id);
                        var newProjectSourceGeneratedDocuments = await newProject.GetSourceGeneratedDocumentsAsync(_shutdownToken).ConfigureAwait(false);
                        var newProjectSourceGeneratedDocumentsById = newProjectSourceGeneratedDocuments.ToDictionary(static document => document.Id);

                        foreach (var (oldDocumentId, _) in oldProjectSourceGeneratedDocumentsById)
                        {
                            if (!newProjectSourceGeneratedDocumentsById.ContainsKey(oldDocumentId))
                            {
                                // This source generated document was removed
                                EnqueueFullDocumentEvent(oldSolution, oldDocumentId, UnitTestingInvocationReasons.DocumentRemoved, "OnWorkspaceChanged");
                            }
                        }

                        foreach (var (newDocumentId, newDocument) in newProjectSourceGeneratedDocumentsById)
                        {
                            if (!oldProjectSourceGeneratedDocumentsById.TryGetValue(newDocumentId, out var oldDocument))
                            {
                                // This source generated document was added
                                EnqueueFullDocumentEvent(newSolution, newDocumentId, UnitTestingInvocationReasons.DocumentAdded, "OnWorkspaceChanged");
                            }
                            else
                            {
                                // This source generated document may have changed
                                await EnqueueChangedDocumentWorkItemAsync(oldDocument, newDocument).ConfigureAwait(continueOnCapturedContext: false);
                            }
                        }
                    }
                },
                _shutdownToken);
        }

        private async Task EnqueueDocumentWorkItemAsync(Project project, DocumentId documentId, TextDocument? document, UnitTestingInvocationReasons invocationReasons, SyntaxNode? changedMember = null)
        {
            // we are shutting down
            _shutdownToken.ThrowIfCancellationRequested();

            var priorityService = project.GetLanguageService<IUnitTestingWorkCoordinatorPriorityService>();
            document ??= project.GetTextDocument(documentId);
            var sourceDocument = document as Document;
            var isLowPriority = priorityService != null && sourceDocument != null && await priorityService.IsLowPriorityAsync(sourceDocument, _shutdownToken).ConfigureAwait(false);

            var currentMember = GetSyntaxPath(changedMember);

            // call to this method is serialized. and only this method does the writing.
            _documentAndProjectWorkerProcessor.Enqueue(
                new UnitTestingWorkItem(documentId, project.Language, invocationReasons, isLowPriority, currentMember, _listener.BeginAsyncOperation("WorkItem")));

            // enqueue semantic work planner
            if (invocationReasons.Contains(UnitTestingPredefinedInvocationReasons.SemanticChanged) && sourceDocument != null)
            {
                // must use "Document" here so that the snapshot doesn't go away. we need the snapshot to calculate p2p dependency graph later.
                // due to this, we might hold onto solution (and things kept alive by it) little bit longer than usual.
                _semanticChangeProcessor.Enqueue(project, documentId, sourceDocument, currentMember);
            }
        }

        private static Document GetRequiredDocument(Project project, DocumentId documentId, Document? document)
            => document ?? project.GetRequiredDocument(documentId);

        private static SyntaxPath? GetSyntaxPath(SyntaxNode? changedMember)
        {
            // using syntax path might be too expansive since it will be created on every keystroke.
            // but currently, we have no other way to track a node between two different tree (even for incrementally parsed one)
            if (changedMember == null)
            {
                return null;
            }

            return new SyntaxPath(changedMember);
        }

        private async Task EnqueueFullProjectWorkItemAsync(Project project, UnitTestingInvocationReasons invocationReasons)
        {
            foreach (var documentId in project.DocumentIds)
                await EnqueueDocumentWorkItemAsync(project, documentId, document: null, invocationReasons).ConfigureAwait(false);

            foreach (var documentId in project.AdditionalDocumentIds)
                await EnqueueDocumentWorkItemAsync(project, documentId, document: null, invocationReasons).ConfigureAwait(false);

            foreach (var documentId in project.AnalyzerConfigDocumentIds)
                await EnqueueDocumentWorkItemAsync(project, documentId, document: null, invocationReasons).ConfigureAwait(false);

            // If all features are enabled for source generated documents, the solution crawler needs to
            // include them in incremental analysis.
            if (_solutionCrawlerOptionsService?.EnableDiagnosticsInSourceGeneratedFiles == true)
            {
                foreach (var document in await project.GetSourceGeneratedDocumentsAsync(_shutdownToken).ConfigureAwait(false))
                    await EnqueueDocumentWorkItemAsync(project, document.Id, document, invocationReasons).ConfigureAwait(false);
            }
        }

        private async Task EnqueueWorkItemAsync(IUnitTestingIncrementalAnalyzer analyzer, UnitTestingReanalyzeScope scope)
        {
            var solution = _registration.GetSolutionToAnalyze();
            var invocationReasons =
                UnitTestingInvocationReasons.Reanalyze;

            foreach (var (project, documentId) in scope.GetDocumentIds(solution))
                await EnqueueWorkItemAsync(analyzer, project, documentId, document: null, invocationReasons).ConfigureAwait(false);
        }

        private async Task EnqueueWorkItemAsync(
            IUnitTestingIncrementalAnalyzer analyzer, Project project, DocumentId documentId, Document? document, UnitTestingInvocationReasons invocationReasons)
        {
            var priorityService = project.GetLanguageService<IUnitTestingWorkCoordinatorPriorityService>();
            var isLowPriority = priorityService != null && await priorityService.IsLowPriorityAsync(
                GetRequiredDocument(project, documentId, document), _shutdownToken).ConfigureAwait(false);

            _documentAndProjectWorkerProcessor.Enqueue(
                new UnitTestingWorkItem(documentId, project.Language, invocationReasons,
                    isLowPriority, analyzer, _listener.BeginAsyncOperation("WorkItem")));
        }

        private async Task EnqueueWorkItemAsync(ProjectChanges projectChanges)
        {
            await EnqueueProjectConfigurationChangeWorkItemAsync(projectChanges).ConfigureAwait(false);

            foreach (var addedDocumentId in projectChanges.GetAddedDocuments())
                await EnqueueDocumentWorkItemAsync(projectChanges.NewProject, addedDocumentId, document: null, UnitTestingInvocationReasons.DocumentAdded).ConfigureAwait(false);

            foreach (var changedDocumentId in projectChanges.GetChangedDocuments())
            {
                await EnqueueChangedDocumentWorkItemAsync(projectChanges.OldProject.GetRequiredDocument(changedDocumentId), projectChanges.NewProject.GetRequiredDocument(changedDocumentId))
                    .ConfigureAwait(continueOnCapturedContext: false);
            }

            foreach (var removedDocumentId in projectChanges.GetRemovedDocuments())
                await EnqueueDocumentWorkItemAsync(projectChanges.OldProject, removedDocumentId, document: null, UnitTestingInvocationReasons.DocumentRemoved).ConfigureAwait(false);
        }

        private async Task EnqueueProjectConfigurationChangeWorkItemAsync(ProjectChanges projectChanges)
        {
            var oldProject = projectChanges.OldProject;
            var newProject = projectChanges.NewProject;

            // TODO: why solution changes return Project not ProjectId but ProjectChanges return DocumentId not Document?
            var projectConfigurationChange = UnitTestingInvocationReasons.Empty;

            if (projectChanges.GetAddedMetadataReferences().Any() ||
                projectChanges.GetAddedProjectReferences().Any() ||
                projectChanges.GetAddedAnalyzerReferences().Any() ||
                projectChanges.GetRemovedMetadataReferences().Any() ||
                projectChanges.GetRemovedProjectReferences().Any() ||
                projectChanges.GetRemovedAnalyzerReferences().Any() ||
                !object.Equals(oldProject.CompilationOptions, newProject.CompilationOptions) ||
                !object.Equals(oldProject.AssemblyName, newProject.AssemblyName) ||
                !object.Equals(oldProject.Name, newProject.Name) ||
                !object.Equals(oldProject.AnalyzerOptions, newProject.AnalyzerOptions) ||
                !object.Equals(oldProject.DefaultNamespace, newProject.DefaultNamespace) ||
                !object.Equals(oldProject.OutputFilePath, newProject.OutputFilePath) ||
                !object.Equals(oldProject.OutputRefFilePath, newProject.OutputRefFilePath) ||
                !oldProject.CompilationOutputInfo.Equals(newProject.CompilationOutputInfo) ||
                oldProject.State.RunAnalyzers != newProject.State.RunAnalyzers)
            {
                projectConfigurationChange = projectConfigurationChange.With(UnitTestingInvocationReasons.ProjectConfigurationChanged);
            }

            if (!projectConfigurationChange.IsEmpty)
            {
                await EnqueueFullProjectWorkItemAsync(projectChanges.NewProject, projectConfigurationChange).ConfigureAwait(false);
            }
        }

        private async Task EnqueueChangedDocumentWorkItemAsync(Document oldDocument, Document newDocument)
        {
            var differenceService = newDocument.GetLanguageService<IUnitTestingDocumentDifferenceService>();

            if (differenceService == null)
            {
                // For languages that don't use a Roslyn syntax tree, they don't export a document difference service.
                // The whole document should be considered as changed in that case.
                await EnqueueDocumentWorkItemAsync(newDocument.Project, newDocument.Id, newDocument, UnitTestingInvocationReasons.DocumentChanged).ConfigureAwait(false);
            }
            else
            {
                var differenceResult = differenceService.GetDifference(oldDocument, newDocument, _shutdownToken);

                if (differenceResult != null)
                    await EnqueueDocumentWorkItemAsync(newDocument.Project, newDocument.Id, newDocument, differenceResult.ChangeType, differenceResult.ChangedMember).ConfigureAwait(false);
            }
        }

        internal TestAccessor GetTestAccessor()
        {
            return new TestAccessor(this);
        }

        internal readonly struct TestAccessor
        {
            private readonly UnitTestingWorkCoordinator _workCoordinator;

            internal TestAccessor(UnitTestingWorkCoordinator workCoordinator)
            {
                _workCoordinator = workCoordinator;
            }

            internal void WaitUntilCompletion(ImmutableArray<IUnitTestingIncrementalAnalyzer> workers)
            {
                var solution = _workCoordinator._registration.GetSolutionToAnalyze();
                var list = new List<UnitTestingWorkItem>();

                foreach (var project in solution.Projects)
                {
                    foreach (var document in project.Documents)
                    {
                        list.Add(new UnitTestingWorkItem(document.Id, document.Project.Language, UnitTestingInvocationReasons.DocumentAdded, isLowPriority: false, activeMember: null, EmptyAsyncToken.Instance));
                    }
                }

                _workCoordinator._documentAndProjectWorkerProcessor.GetTestAccessor().WaitUntilCompletion(workers, list);
            }

            internal void WaitUntilCompletion()
                => _workCoordinator._documentAndProjectWorkerProcessor.GetTestAccessor().WaitUntilCompletion();
        }
    }

    internal readonly struct UnitTestingReanalyzeScope
    {
        private readonly SolutionId? _solutionId;
        private readonly ISet<object>? _projectOrDocumentIds;

        public UnitTestingReanalyzeScope(SolutionId solutionId)
        {
            _solutionId = solutionId;
            _projectOrDocumentIds = null;
        }

        public UnitTestingReanalyzeScope(IEnumerable<ProjectId>? projectIds = null, IEnumerable<DocumentId>? documentIds = null)
        {
            projectIds ??= [];
            documentIds ??= [];

            _solutionId = null;
            _projectOrDocumentIds = new HashSet<object>(projectIds);

            foreach (var documentId in documentIds)
            {
                if (_projectOrDocumentIds.Contains(documentId.ProjectId))
                {
                    continue;
                }

                _projectOrDocumentIds.Add(documentId);
            }
        }

        public bool HasMultipleDocuments => _solutionId != null || _projectOrDocumentIds?.Count > 1;

        public string GetLanguagesStringForTelemetry(Solution solution)
        {
            if (_solutionId != null && solution.Id != _solutionId)
            {
                // return empty if given solution is not 
                // same as solution this scope is created for
                return string.Empty;
            }

            using var pool = SharedPools.Default<HashSet<string>>().GetPooledObject();
            if (_solutionId != null)
            {
                pool.Object.UnionWith(solution.SolutionState.ProjectStates.Select(kv => kv.Value.Language));
                return string.Join(",", pool.Object);
            }

            Contract.ThrowIfNull(_projectOrDocumentIds);

            foreach (var projectOrDocumentId in _projectOrDocumentIds)
            {
                switch (projectOrDocumentId)
                {
                    case ProjectId projectId:
                        var project = solution.GetProject(projectId);
                        if (project != null)
                        {
                            pool.Object.Add(project.Language);
                        }

                        break;
                    case DocumentId documentId:
                        var document = solution.GetDocument(documentId);
                        if (document != null)
                        {
                            pool.Object.Add(document.Project.Language);
                        }

                        break;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(projectOrDocumentId);
                }
            }

            return string.Join(",", pool.Object);
        }

        public int GetDocumentCount(Solution solution)
        {
            if (_solutionId != null && solution.Id != _solutionId)
            {
                return 0;
            }

            var count = 0;
            if (_solutionId != null)
            {
                foreach (var projectState in solution.SolutionState.ProjectStates)
                {
                    count += projectState.Value.DocumentStates.Count;
                }

                return count;
            }

            Contract.ThrowIfNull(_projectOrDocumentIds);

            foreach (var projectOrDocumentId in _projectOrDocumentIds)
            {
                switch (projectOrDocumentId)
                {
                    case ProjectId projectId:
                        var project = solution.GetProject(projectId);
                        if (project != null)
                        {
                            count += project.DocumentIds.Count;
                        }

                        break;
                    case DocumentId documentId:
                        count++;
                        break;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(projectOrDocumentId);
                }
            }

            return count;
        }

        public IEnumerable<(Project project, DocumentId documentId)> GetDocumentIds(Solution solution)
        {
            if (_solutionId != null && solution.Id != _solutionId)
            {
                yield break;
            }

            if (_solutionId != null)
            {
                foreach (var project in solution.Projects)
                {
                    foreach (var documentId in project.DocumentIds)
                        yield return (project, documentId);
                }

                yield break;
            }

            Contract.ThrowIfNull(_projectOrDocumentIds);

            foreach (var projectOrDocumentId in _projectOrDocumentIds)
            {
                switch (projectOrDocumentId)
                {
                    case ProjectId projectId:
                        {
                            var project = solution.GetProject(projectId);
                            if (project != null)
                            {
                                foreach (var documentId in project.DocumentIds)
                                    yield return (project, documentId);
                            }

                            break;
                        }
                    case DocumentId documentId:
                        {
                            var project = solution.GetProject(documentId.ProjectId);
                            if (project != null)
                            {
                                // ReanalyzeScopes are created and held in a queue before they are processed later; it's possible the document
                                // that we queued for is no longer present.
                                if (project.ContainsDocument(documentId))
                                    yield return (project, documentId);
                            }

                            break;
                        }
                }
            }
        }
    }
}
