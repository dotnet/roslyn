// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    internal partial class SolutionCrawlerRegistrationService
    {
        private partial class WorkCoordinator
        {
            private readonly Registration _registration;
            private readonly object _gate;

            private readonly LogAggregator _logAggregator;
            private readonly IAsynchronousOperationListener _listener;
            private readonly IOptionService _optionService;
            private readonly IDocumentTrackingService _documentTrackingService;

            private readonly CancellationTokenSource _shutdownNotificationSource;
            private readonly CancellationToken _shutdownToken;
            private readonly SimpleTaskQueue _eventProcessingQueue;

            // points to processor task
            private readonly IncrementalAnalyzerProcessor _documentAndProjectWorkerProcessor;
            private readonly SemanticChangeProcessor _semanticChangeProcessor;

            private Document _lastActiveDocument;

            public WorkCoordinator(
                 IAsynchronousOperationListener listener,
                 IEnumerable<Lazy<IIncrementalAnalyzerProvider, IncrementalAnalyzerProviderMetadata>> analyzerProviders,
                 bool initializeLazily,
                 Registration registration)
            {
                _logAggregator = new LogAggregator();

                _registration = registration;
                _gate = new object();

                _listener = listener;
                _optionService = _registration.GetService<IOptionService>();
                _documentTrackingService = _registration.GetService<IDocumentTrackingService>();

                // event and worker queues
                _shutdownNotificationSource = new CancellationTokenSource();
                _shutdownToken = _shutdownNotificationSource.Token;

                _eventProcessingQueue = new SimpleTaskQueue(TaskScheduler.Default);

                var activeFileBackOffTimeSpanInMS = _optionService.GetOption(InternalSolutionCrawlerOptions.ActiveFileWorkerBackOffTimeSpanInMS);
                var allFilesWorkerBackOffTimeSpanInMS = _optionService.GetOption(InternalSolutionCrawlerOptions.AllFilesWorkerBackOffTimeSpanInMS);
                var entireProjectWorkerBackOffTimeSpanInMS = _optionService.GetOption(InternalSolutionCrawlerOptions.EntireProjectWorkerBackOffTimeSpanInMS);

                _documentAndProjectWorkerProcessor = new IncrementalAnalyzerProcessor(
                    listener, analyzerProviders, initializeLazily, _registration,
                    activeFileBackOffTimeSpanInMS, allFilesWorkerBackOffTimeSpanInMS, entireProjectWorkerBackOffTimeSpanInMS, _shutdownToken);

                var semanticBackOffTimeSpanInMS = _optionService.GetOption(InternalSolutionCrawlerOptions.SemanticChangeBackOffTimeSpanInMS);
                var projectBackOffTimeSpanInMS = _optionService.GetOption(InternalSolutionCrawlerOptions.ProjectPropagationBackOffTimeSpanInMS);

                _semanticChangeProcessor = new SemanticChangeProcessor(listener, _registration, _documentAndProjectWorkerProcessor, semanticBackOffTimeSpanInMS, projectBackOffTimeSpanInMS, _shutdownToken);

                // if option is on
                if (_optionService.GetOption(InternalSolutionCrawlerOptions.SolutionCrawler))
                {
                    _registration.Workspace.WorkspaceChanged += OnWorkspaceChanged;
                    _registration.Workspace.DocumentOpened += OnDocumentOpened;
                    _registration.Workspace.DocumentClosed += OnDocumentClosed;
                }

                // subscribe to option changed event after all required fields are set
                // otherwise, we can get null exception when running OnOptionChanged handler
                _optionService.OptionChanged += OnOptionChanged;

                // subscribe to active document changed event for active file background analysis scope.
                if (_documentTrackingService != null)
                {
                    _lastActiveDocument = _documentTrackingService.GetActiveDocument(_registration.Workspace.CurrentSolution);
                    _documentTrackingService.ActiveDocumentChanged += OnActiveDocumentChanged;
                }
            }

            public int CorrelationId => _registration.CorrelationId;

            public void AddAnalyzer(IIncrementalAnalyzer analyzer, bool highPriorityForActiveFile)
            {
                // add analyzer
                _documentAndProjectWorkerProcessor.AddAnalyzer(analyzer, highPriorityForActiveFile);

                // and ask to re-analyze whole solution for the given analyzer
                var scope = new ReanalyzeScope(_registration.CurrentSolution.Id);
                Reanalyze(analyzer, scope);
            }

            public void Shutdown(bool blockingShutdown)
            {
                _optionService.OptionChanged -= OnOptionChanged;

                if (_documentTrackingService != null)
                {
                    _documentTrackingService.ActiveDocumentChanged -= OnActiveDocumentChanged;
                }

                // detach from the workspace
                _registration.Workspace.WorkspaceChanged -= OnWorkspaceChanged;
                _registration.Workspace.DocumentOpened -= OnDocumentOpened;
                _registration.Workspace.DocumentClosed -= OnDocumentClosed;

                // cancel any pending blocks
                _shutdownNotificationSource.Cancel();

                _documentAndProjectWorkerProcessor.Shutdown();

                SolutionCrawlerLogger.LogWorkCoordinatorShutdown(CorrelationId, _logAggregator);

                if (blockingShutdown)
                {
                    var shutdownTask = Task.WhenAll(
                        _eventProcessingQueue.LastScheduledTask,
                        _documentAndProjectWorkerProcessor.AsyncProcessorTask,
                        _semanticChangeProcessor.AsyncProcessorTask);

                    shutdownTask.Wait(TimeSpan.FromSeconds(5));

                    if (!shutdownTask.IsCompleted)
                    {
                        SolutionCrawlerLogger.LogWorkCoordinatorShutdownTimeout(CorrelationId);
                    }
                }
            }

            private void OnOptionChanged(object sender, OptionChangedEventArgs e)
            {
                // if solution crawler got turned off or on.
                if (e.Option == InternalSolutionCrawlerOptions.SolutionCrawler)
                {
                    var value = (bool)e.Value;
                    if (value)
                    {
                        _registration.Workspace.WorkspaceChanged += OnWorkspaceChanged;
                        _registration.Workspace.DocumentOpened += OnDocumentOpened;
                        _registration.Workspace.DocumentClosed += OnDocumentClosed;
                    }
                    else
                    {
                        _registration.Workspace.WorkspaceChanged -= OnWorkspaceChanged;
                        _registration.Workspace.DocumentOpened -= OnDocumentOpened;
                        _registration.Workspace.DocumentClosed -= OnDocumentClosed;
                    }

                    SolutionCrawlerLogger.LogOptionChanged(CorrelationId, value);
                    return;
                }

                ReanalyzeOnOptionChange(sender, e);
            }

            private void ReanalyzeOnOptionChange(object sender, OptionChangedEventArgs e)
            {
                // get off from option changed event handler since it runs on UI thread
                // getting analyzer can be slow for the very first time since it is lazily initialized
                var asyncToken = _listener.BeginAsyncOperation("ReanalyzeOnOptionChange");

                // Force analyze all analyzers if background analysis scope has changed.
                var forceAnalyze = e.Option == SolutionCrawlerOptions.BackgroundAnalysisScopeOption;

                _eventProcessingQueue.ScheduleTask(() =>
                {
                    // let each analyzer decide what they want on option change
                    foreach (var analyzer in _documentAndProjectWorkerProcessor.Analyzers)
                    {
                        if (forceAnalyze || analyzer.NeedsReanalysisOnOptionChanged(sender, e))
                        {
                            var scope = new ReanalyzeScope(_registration.CurrentSolution.Id);
                            Reanalyze(analyzer, scope);
                        }
                    }
                }, _shutdownToken).CompletesAsyncOperation(asyncToken);
            }

            public void Reanalyze(IIncrementalAnalyzer analyzer, ReanalyzeScope scope, bool highPriority = false)
            {
                var asyncToken = _listener.BeginAsyncOperation("Reanalyze");
                _eventProcessingQueue.ScheduleTask(
                    () => EnqueueWorkItemAsync(analyzer, scope, highPriority), _shutdownToken).CompletesAsyncOperation(asyncToken);

                if (scope.HasMultipleDocuments)
                {
                    // log big reanalysis request from things like fix all, suppress all or option changes
                    // we are not interested in 1 file re-analysis request which can happen from like venus typing
                    var solution = _registration.CurrentSolution;
                    SolutionCrawlerLogger.LogReanalyze(
                        CorrelationId, analyzer, scope.GetDocumentCount(solution), scope.GetLanguagesStringForTelemetry(solution), highPriority);
                }
            }

            private void OnActiveDocumentChanged(object sender, DocumentId activeDocumentId)
            {
                IAsyncToken asyncToken;
                if (SolutionCrawlerOptions.GetBackgroundAnalysisScope(_registration.Workspace.Options) == BackgroundAnalysisScope.ActiveFile &&
                    activeDocumentId != null)
                {
                    var activeDocument = _registration.Workspace.CurrentSolution.GetDocument(activeDocumentId);
                    if (activeDocument != null)
                    {
                        lock (_gate)
                        {
                            if (_lastActiveDocument != null)
                            {
                                asyncToken = _listener.BeginAsyncOperation("OnDocumentClosed");
                                EnqueueEvent(_lastActiveDocument.Project.Solution, _lastActiveDocument.Id, InvocationReasons.DocumentClosed, asyncToken);
                            }

                            _lastActiveDocument = activeDocument;
                        }

                        asyncToken = _listener.BeginAsyncOperation("OnDocumentChanged");
                        EnqueueEvent(activeDocument.Project.Solution, activeDocument.Id, InvocationReasons.DocumentChanged, asyncToken);
                    }
                }
            }

            private void OnWorkspaceChanged(object sender, WorkspaceChangeEventArgs args)
            {
                // guard us from cancellation
                try
                {
                    ProcessEvents(args, _listener.BeginAsyncOperation("OnWorkspaceChanged"));
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
                    if (ae.InnerExceptions.Any(e => !(e is OperationCanceledException)) ||
                        ae.InnerExceptions.Cast<OperationCanceledException>().Any(NotOurShutdownToken))
                    {
                        // We had a cancellation with a different token, so don't eat it
                        throw;
                    }

                    // it is our cancellation, ignore
                }
            }

            private bool NotOurShutdownToken(OperationCanceledException oce)
            {
                return oce.CancellationToken == _shutdownToken;
            }

            private void ProcessEvents(WorkspaceChangeEventArgs args, IAsyncToken asyncToken)
            {
                SolutionCrawlerLogger.LogWorkspaceEvent(_logAggregator, (int)args.Kind);

                // TODO: add telemetry that record how much it takes to process an event (max, min, average and etc)
                switch (args.Kind)
                {
                    case WorkspaceChangeKind.SolutionAdded:
                    case WorkspaceChangeKind.SolutionChanged:
                    case WorkspaceChangeKind.SolutionReloaded:
                    case WorkspaceChangeKind.SolutionRemoved:
                    case WorkspaceChangeKind.SolutionCleared:
                        ProcessSolutionEvent(args, asyncToken);
                        break;
                    case WorkspaceChangeKind.ProjectAdded:
                    case WorkspaceChangeKind.ProjectChanged:
                    case WorkspaceChangeKind.ProjectReloaded:
                    case WorkspaceChangeKind.ProjectRemoved:
                        ProcessProjectEvent(args, asyncToken);
                        break;
                    case WorkspaceChangeKind.DocumentAdded:
                    case WorkspaceChangeKind.DocumentReloaded:
                    case WorkspaceChangeKind.DocumentChanged:
                    case WorkspaceChangeKind.DocumentRemoved:
                    case WorkspaceChangeKind.AdditionalDocumentAdded:
                    case WorkspaceChangeKind.AdditionalDocumentRemoved:
                    case WorkspaceChangeKind.AdditionalDocumentChanged:
                    case WorkspaceChangeKind.AdditionalDocumentReloaded:
                    case WorkspaceChangeKind.AnalyzerConfigDocumentAdded:
                    case WorkspaceChangeKind.AnalyzerConfigDocumentRemoved:
                    case WorkspaceChangeKind.AnalyzerConfigDocumentChanged:
                    case WorkspaceChangeKind.AnalyzerConfigDocumentReloaded:
                        ProcessDocumentEvent(args, asyncToken);
                        break;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(args.Kind);
                }
            }

            private void OnDocumentOpened(object sender, DocumentEventArgs e)
            {
                var asyncToken = _listener.BeginAsyncOperation("OnDocumentOpened");
                _eventProcessingQueue.ScheduleTask(
                    () => EnqueueWorkItemAsync(e.Document, InvocationReasons.DocumentOpened), _shutdownToken).CompletesAsyncOperation(asyncToken);
            }

            private void OnDocumentClosed(object sender, DocumentEventArgs e)
            {
                var asyncToken = _listener.BeginAsyncOperation("OnDocumentClosed");
                _eventProcessingQueue.ScheduleTask(
                    () => EnqueueWorkItemAsync(e.Document, InvocationReasons.DocumentClosed), _shutdownToken).CompletesAsyncOperation(asyncToken);
            }

            private void ProcessDocumentEvent(WorkspaceChangeEventArgs e, IAsyncToken asyncToken)
            {
                switch (e.Kind)
                {
                    case WorkspaceChangeKind.DocumentAdded:
                        EnqueueEvent(e.NewSolution, e.DocumentId, InvocationReasons.DocumentAdded, asyncToken);
                        break;
                    case WorkspaceChangeKind.DocumentRemoved:
                        EnqueueEvent(e.OldSolution, e.DocumentId, InvocationReasons.DocumentRemoved, asyncToken);
                        break;
                    case WorkspaceChangeKind.DocumentReloaded:
                    case WorkspaceChangeKind.DocumentChanged:
                        EnqueueEvent(e.OldSolution, e.NewSolution, e.DocumentId, asyncToken);
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
                        EnqueueEvent(e.NewSolution, e.ProjectId, InvocationReasons.AdditionalDocumentChanged, asyncToken);
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(e.Kind);
                }
            }

            private void ProcessProjectEvent(WorkspaceChangeEventArgs e, IAsyncToken asyncToken)
            {
                switch (e.Kind)
                {
                    case WorkspaceChangeKind.ProjectAdded:
                        EnqueueEvent(e.NewSolution, e.ProjectId, InvocationReasons.DocumentAdded, asyncToken);
                        break;
                    case WorkspaceChangeKind.ProjectRemoved:
                        EnqueueEvent(e.OldSolution, e.ProjectId, InvocationReasons.DocumentRemoved, asyncToken);
                        break;
                    case WorkspaceChangeKind.ProjectChanged:
                    case WorkspaceChangeKind.ProjectReloaded:
                        EnqueueEvent(e.OldSolution, e.NewSolution, e.ProjectId, asyncToken);
                        break;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(e.Kind);
                }
            }

            private void ProcessSolutionEvent(WorkspaceChangeEventArgs e, IAsyncToken asyncToken)
            {
                switch (e.Kind)
                {
                    case WorkspaceChangeKind.SolutionAdded:
                        EnqueueEvent(e.NewSolution, InvocationReasons.DocumentAdded, asyncToken);
                        break;
                    case WorkspaceChangeKind.SolutionRemoved:
                        EnqueueEvent(e.OldSolution, InvocationReasons.SolutionRemoved, asyncToken);
                        break;
                    case WorkspaceChangeKind.SolutionCleared:
                        EnqueueEvent(e.OldSolution, InvocationReasons.DocumentRemoved, asyncToken);
                        break;
                    case WorkspaceChangeKind.SolutionChanged:
                    case WorkspaceChangeKind.SolutionReloaded:
                        EnqueueEvent(e.OldSolution, e.NewSolution, asyncToken);
                        break;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(e.Kind);
                }
            }

            private void EnqueueEvent(Solution oldSolution, Solution newSolution, IAsyncToken asyncToken)
            {
                _eventProcessingQueue.ScheduleTask(
                    () => EnqueueWorkItemAsync(oldSolution, newSolution), _shutdownToken).CompletesAsyncOperation(asyncToken);
            }

            private void EnqueueEvent(Solution solution, InvocationReasons invocationReasons, IAsyncToken asyncToken)
            {
                _eventProcessingQueue.ScheduleTask(
                    () => EnqueueWorkItemForSolutionAsync(solution, invocationReasons), _shutdownToken).CompletesAsyncOperation(asyncToken);
            }

            private void EnqueueEvent(Solution oldSolution, Solution newSolution, ProjectId projectId, IAsyncToken asyncToken)
            {
                _eventProcessingQueue.ScheduleTask(
                    () => EnqueueWorkItemAfterDiffAsync(oldSolution, newSolution, projectId), _shutdownToken).CompletesAsyncOperation(asyncToken);
            }

            private void EnqueueEvent(Solution solution, ProjectId projectId, InvocationReasons invocationReasons, IAsyncToken asyncToken)
            {
                _eventProcessingQueue.ScheduleTask(
                    () => EnqueueWorkItemForProjectAsync(solution, projectId, invocationReasons), _shutdownToken).CompletesAsyncOperation(asyncToken);
            }

            private void EnqueueEvent(Solution solution, DocumentId documentId, InvocationReasons invocationReasons, IAsyncToken asyncToken)
            {
                _eventProcessingQueue.ScheduleTask(
                    () => EnqueueWorkItemForDocumentAsync(solution, documentId, invocationReasons), _shutdownToken).CompletesAsyncOperation(asyncToken);
            }

            private void EnqueueEvent(Solution oldSolution, Solution newSolution, DocumentId documentId, IAsyncToken asyncToken)
            {
                // document changed event is the special one.
                _eventProcessingQueue.ScheduleTask(
                    () => EnqueueWorkItemAfterDiffAsync(oldSolution, newSolution, documentId), _shutdownToken).CompletesAsyncOperation(asyncToken);
            }

            private async Task EnqueueWorkItemAsync(Document document, InvocationReasons invocationReasons, SyntaxNode changedMember = null)
            {
                // we are shutting down
                _shutdownToken.ThrowIfCancellationRequested();

                var priorityService = document.GetLanguageService<IWorkCoordinatorPriorityService>();
                var isLowPriority = priorityService != null && await priorityService.IsLowPriorityAsync(document, _shutdownToken).ConfigureAwait(false);

                var currentMember = GetSyntaxPath(changedMember);

                // call to this method is serialized. and only this method does the writing.
                _documentAndProjectWorkerProcessor.Enqueue(
                    new WorkItem(document.Id, document.Project.Language, invocationReasons,
                    isLowPriority, currentMember, _listener.BeginAsyncOperation("WorkItem")));

                // enqueue semantic work planner
                if (invocationReasons.Contains(PredefinedInvocationReasons.SemanticChanged))
                {
                    // must use "Document" here so that the snapshot doesn't go away. we need the snapshot to calculate p2p dependency graph later.
                    // due to this, we might hold onto solution (and things kept alive by it) little bit longer than usual.
                    _semanticChangeProcessor.Enqueue(document, currentMember);
                }
            }

            private SyntaxPath GetSyntaxPath(SyntaxNode changedMember)
            {
                // using syntax path might be too expansive since it will be created on every keystroke.
                // but currently, we have no other way to track a node between two different tree (even for incrementally parsed one)
                if (changedMember == null)
                {
                    return null;
                }

                return new SyntaxPath(changedMember);
            }

            private async Task EnqueueWorkItemAsync(Project project, InvocationReasons invocationReasons)
            {
                foreach (var documentId in project.DocumentIds)
                {
                    var document = project.GetDocument(documentId);
                    await EnqueueWorkItemAsync(document, invocationReasons).ConfigureAwait(false);
                }
            }

            private async Task EnqueueWorkItemAsync(IIncrementalAnalyzer analyzer, ReanalyzeScope scope, bool highPriority)
            {
                var solution = _registration.CurrentSolution;
                var invocationReasons = highPriority ? InvocationReasons.ReanalyzeHighPriority : InvocationReasons.Reanalyze;

                foreach (var document in scope.GetDocuments(solution))
                {
                    await EnqueueWorkItemAsync(analyzer, document, invocationReasons).ConfigureAwait(false);
                }
            }

            private async Task EnqueueWorkItemAsync(IIncrementalAnalyzer analyzer, Document document, InvocationReasons invocationReasons)
            {
                var priorityService = document.GetLanguageService<IWorkCoordinatorPriorityService>();
                var isLowPriority = priorityService != null && await priorityService.IsLowPriorityAsync(document, _shutdownToken).ConfigureAwait(false);

                _documentAndProjectWorkerProcessor.Enqueue(
                    new WorkItem(document.Id, document.Project.Language, invocationReasons,
                    isLowPriority, analyzer, _listener.BeginAsyncOperation("WorkItem")));
            }

            private async Task EnqueueWorkItemAsync(Solution oldSolution, Solution newSolution)
            {
                var solutionChanges = newSolution.GetChanges(oldSolution);

                // TODO: Async version for GetXXX methods?
                foreach (var addedProject in solutionChanges.GetAddedProjects())
                {
                    await EnqueueWorkItemAsync(addedProject, InvocationReasons.DocumentAdded).ConfigureAwait(false);
                }

                foreach (var projectChanges in solutionChanges.GetProjectChanges())
                {
                    await EnqueueWorkItemAsync(projectChanges).ConfigureAwait(continueOnCapturedContext: false);
                }

                foreach (var removedProject in solutionChanges.GetRemovedProjects())
                {
                    await EnqueueWorkItemAsync(removedProject, InvocationReasons.DocumentRemoved).ConfigureAwait(false);
                }
            }

            private async Task EnqueueWorkItemAsync(ProjectChanges projectChanges)
            {
                await EnqueueProjectConfigurationChangeWorkItemAsync(projectChanges).ConfigureAwait(false);

                foreach (var addedDocumentId in projectChanges.GetAddedDocuments())
                {
                    await EnqueueWorkItemAsync(projectChanges.NewProject.GetDocument(addedDocumentId), InvocationReasons.DocumentAdded).ConfigureAwait(false);
                }

                foreach (var changedDocumentId in projectChanges.GetChangedDocuments())
                {
                    await EnqueueWorkItemAsync(projectChanges.OldProject.GetDocument(changedDocumentId), projectChanges.NewProject.GetDocument(changedDocumentId))
                        .ConfigureAwait(continueOnCapturedContext: false);
                }

                foreach (var removedDocumentId in projectChanges.GetRemovedDocuments())
                {
                    await EnqueueWorkItemAsync(projectChanges.OldProject.GetDocument(removedDocumentId), InvocationReasons.DocumentRemoved).ConfigureAwait(false);
                }
            }

            private async Task EnqueueProjectConfigurationChangeWorkItemAsync(ProjectChanges projectChanges)
            {
                var oldProject = projectChanges.OldProject;
                var newProject = projectChanges.NewProject;

                // TODO: why solution changes return Project not ProjectId but ProjectChanges return DocumentId not Document?
                var projectConfigurationChange = InvocationReasons.Empty;

                if (!object.Equals(oldProject.ParseOptions, newProject.ParseOptions))
                {
                    projectConfigurationChange = projectConfigurationChange.With(InvocationReasons.ProjectParseOptionChanged);
                }

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
                    !object.Equals(oldProject.OutputRefFilePath, newProject.OutputRefFilePath))
                {
                    projectConfigurationChange = projectConfigurationChange.With(InvocationReasons.ProjectConfigurationChanged);
                }

                if (!projectConfigurationChange.IsEmpty)
                {
                    await EnqueueWorkItemAsync(projectChanges.NewProject, projectConfigurationChange).ConfigureAwait(false);
                }
            }

            private async Task EnqueueWorkItemAsync(Document oldDocument, Document newDocument)
            {
                var differenceService = newDocument.GetLanguageService<IDocumentDifferenceService>();

                if (differenceService == null)
                {
                    // For languages that don't use a Roslyn syntax tree, they don't export a document difference service.
                    // The whole document should be considered as changed in that case.
                    await EnqueueWorkItemAsync(newDocument, InvocationReasons.DocumentChanged).ConfigureAwait(false);
                }
                else
                {
                    var differenceResult = await differenceService.GetDifferenceAsync(oldDocument, newDocument, _shutdownToken).ConfigureAwait(false);

                    if (differenceResult != null)
                    {
                        await EnqueueWorkItemAsync(newDocument, differenceResult.ChangeType, differenceResult.ChangedMember).ConfigureAwait(false);
                    }
                }
            }

            private Task EnqueueWorkItemForDocumentAsync(Solution solution, DocumentId documentId, InvocationReasons invocationReasons)
            {
                var document = solution.GetDocument(documentId);

                return EnqueueWorkItemAsync(document, invocationReasons);
            }

            private Task EnqueueWorkItemForProjectAsync(Solution solution, ProjectId projectId, InvocationReasons invocationReasons)
            {
                var project = solution.GetProject(projectId);

                return EnqueueWorkItemAsync(project, invocationReasons);
            }

            private async Task EnqueueWorkItemForSolutionAsync(Solution solution, InvocationReasons invocationReasons)
            {
                foreach (var projectId in solution.ProjectIds)
                {
                    await EnqueueWorkItemForProjectAsync(solution, projectId, invocationReasons).ConfigureAwait(false);
                }
            }

            private async Task EnqueueWorkItemAfterDiffAsync(Solution oldSolution, Solution newSolution, ProjectId projectId)
            {
                var oldProject = oldSolution.GetProject(projectId);
                var newProject = newSolution.GetProject(projectId);

                await EnqueueWorkItemAsync(newProject.GetChanges(oldProject)).ConfigureAwait(continueOnCapturedContext: false);
            }

            private async Task EnqueueWorkItemAfterDiffAsync(Solution oldSolution, Solution newSolution, DocumentId documentId)
            {
                var oldProject = oldSolution.GetProject(documentId.ProjectId);
                var newProject = newSolution.GetProject(documentId.ProjectId);

                await EnqueueWorkItemAsync(oldProject.GetDocument(documentId), newProject.GetDocument(documentId)).ConfigureAwait(continueOnCapturedContext: false);
            }

            internal void WaitUntilCompletion_ForTestingPurposesOnly(ImmutableArray<IIncrementalAnalyzer> workers)
            {
                var solution = _registration.CurrentSolution;
                var list = new List<WorkItem>();

                foreach (var project in solution.Projects)
                {
                    foreach (var document in project.Documents)
                    {
                        list.Add(new WorkItem(document.Id, document.Project.Language, InvocationReasons.DocumentAdded, false, EmptyAsyncToken.Instance));
                    }
                }

                _documentAndProjectWorkerProcessor.WaitUntilCompletion_ForTestingPurposesOnly(workers, list);
            }

            internal void WaitUntilCompletion_ForTestingPurposesOnly()
            {
                _documentAndProjectWorkerProcessor.WaitUntilCompletion_ForTestingPurposesOnly();
            }
        }

        private readonly struct ReanalyzeScope
        {
            private readonly SolutionId _solutionId;
            private readonly ISet<object> _projectOrDocumentIds;

            public ReanalyzeScope(SolutionId solutionId)
            {
                _solutionId = solutionId;
                _projectOrDocumentIds = null;
            }

            public ReanalyzeScope(IEnumerable<ProjectId> projectIds = null, IEnumerable<DocumentId> documentIds = null)
            {
                projectIds ??= SpecializedCollections.EmptyEnumerable<ProjectId>();
                documentIds ??= SpecializedCollections.EmptyEnumerable<DocumentId>();

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
                    pool.Object.UnionWith(solution.State.ProjectStates.Select(kv => kv.Value.Language));
                    return string.Join(",", pool.Object);
                }

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
                    foreach (var projectState in solution.State.ProjectStates)
                    {
                        count += projectState.Value.DocumentIds.Count;
                    }

                    return count;
                }

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

            public IEnumerable<Document> GetDocuments(Solution solution)
            {
                if (_solutionId != null && solution.Id != _solutionId)
                {
                    yield break;
                }

                if (_solutionId != null)
                {
                    foreach (var document in solution.Projects.SelectMany(p => p.Documents))
                    {
                        yield return document;
                    }

                    yield break;
                }

                foreach (var projectOrDocumentId in _projectOrDocumentIds)
                {
                    switch (projectOrDocumentId)
                    {
                        case ProjectId projectId:
                            {
                                var project = solution.GetProject(projectId);
                                if (project != null)
                                {
                                    foreach (var document in project.Documents)
                                    {
                                        yield return document;
                                    }
                                }
                                break;
                            }
                        case DocumentId documentId:
                            {
                                var document = solution.GetDocument(documentId);
                                if (document != null)
                                {
                                    yield return document;
                                }
                                break;
                            }
                    }
                }
            }
        }
    }
}
