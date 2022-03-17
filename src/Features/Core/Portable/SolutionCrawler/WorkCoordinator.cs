// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    internal partial class SolutionCrawlerRegistrationService
    {
        internal sealed partial class WorkCoordinator
        {
            private readonly Registration _registration;
            private readonly object _gate;

            private readonly LogAggregator _logAggregator;
            private readonly IAsynchronousOperationListener _listener;
            private readonly IOptionService _optionService;
            private readonly IDocumentTrackingService _documentTrackingService;
            private readonly ISyntaxTreeConfigurationService? _syntaxTreeConfigurationService;

            private readonly CancellationTokenSource _shutdownNotificationSource;
            private readonly CancellationToken _shutdownToken;
            private readonly TaskQueue _eventProcessingQueue;

            // points to processor task
            private readonly IncrementalAnalyzerProcessor _documentAndProjectWorkerProcessor;
            private readonly SemanticChangeProcessor _semanticChangeProcessor;

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
                _optionService = _registration.Workspace.Services.GetRequiredService<IOptionService>();
                _documentTrackingService = _registration.Workspace.Services.GetRequiredService<IDocumentTrackingService>();
                _syntaxTreeConfigurationService = _registration.Workspace.Services.GetService<ISyntaxTreeConfigurationService>();

                // event and worker queues
                _shutdownNotificationSource = new CancellationTokenSource();
                _shutdownToken = _shutdownNotificationSource.Token;

                _eventProcessingQueue = new TaskQueue(listener, TaskScheduler.Default);

                var activeFileBackOffTimeSpan = SolutionCrawlerTimeSpan.ActiveFileWorkerBackOff;
                var allFilesWorkerBackOffTimeSpan = SolutionCrawlerTimeSpan.AllFilesWorkerBackOff;
                var entireProjectWorkerBackOffTimeSpan = SolutionCrawlerTimeSpan.EntireProjectWorkerBackOff;

                _documentAndProjectWorkerProcessor = new IncrementalAnalyzerProcessor(
                    listener, analyzerProviders, initializeLazily, _registration,
                    activeFileBackOffTimeSpan, allFilesWorkerBackOffTimeSpan, entireProjectWorkerBackOffTimeSpan, _shutdownToken);

                var semanticBackOffTimeSpan = SolutionCrawlerTimeSpan.SemanticChangeBackOff;
                var projectBackOffTimeSpan = SolutionCrawlerTimeSpan.ProjectPropagationBackOff;

                _semanticChangeProcessor = new SemanticChangeProcessor(listener, _registration, _documentAndProjectWorkerProcessor, semanticBackOffTimeSpan, projectBackOffTimeSpan, _shutdownToken);

                _registration.Workspace.WorkspaceChanged += OnWorkspaceChanged;
                _registration.Workspace.DocumentOpened += OnDocumentOpened;
                _registration.Workspace.DocumentClosed += OnDocumentClosed;

                // subscribe to active document changed event for active file background analysis scope.
                _documentTrackingService.ActiveDocumentChanged += OnActiveDocumentSwitched;

                // subscribe to option changed event after all required fields are set
                // otherwise, we can get null exception when running OnOptionChanged handler
                _optionService.OptionChanged += OnOptionChanged;
            }

            public int CorrelationId => _registration.CorrelationId;

            public void AddAnalyzer(IIncrementalAnalyzer analyzer, bool highPriorityForActiveFile)
            {
                // add analyzer
                _documentAndProjectWorkerProcessor.AddAnalyzer(analyzer, highPriorityForActiveFile);

                // and ask to re-analyze whole solution for the given analyzer
                var scope = new ReanalyzeScope(_registration.GetSolutionToAnalyze().Id);
                Reanalyze(analyzer, scope);
            }

            public void Shutdown(bool blockingShutdown)
            {
                _optionService.OptionChanged -= OnOptionChanged;
                _documentTrackingService.ActiveDocumentChanged -= OnActiveDocumentSwitched;

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

                    try
                    {
                        shutdownTask.Wait(TimeSpan.FromSeconds(5));
                    }
                    catch (AggregateException ex)
                    {
                        ex.Handle(e => e is OperationCanceledException);
                    }

                    if (!shutdownTask.IsCompleted)
                    {
                        SolutionCrawlerLogger.LogWorkCoordinatorShutdownTimeout(CorrelationId);
                    }
                }
            }

            private void OnOptionChanged(object? sender, OptionChangedEventArgs e)
            {
                ReanalyzeOnOptionChange(sender, e);
            }

            private void ReanalyzeOnOptionChange(object? sender, OptionChangedEventArgs e)
            {
                // get off from option changed event handler since it runs on UI thread
                // getting analyzer can be slow for the very first time since it is lazily initialized
                _eventProcessingQueue.ScheduleTask(nameof(ReanalyzeOnOptionChange), () =>
                {
                    // let each analyzer decide what they want on option change
                    foreach (var analyzer in _documentAndProjectWorkerProcessor.Analyzers)
                    {
                        if (analyzer.NeedsReanalysisOnOptionChanged(sender, e))
                        {
                            var scope = new ReanalyzeScope(_registration.GetSolutionToAnalyze().Id);
                            Reanalyze(analyzer, scope);
                        }
                    }
                }, _shutdownToken);
            }

            public void Reanalyze(IIncrementalAnalyzer analyzer, ReanalyzeScope scope, bool highPriority = false)
            {
                _eventProcessingQueue.ScheduleTask("Reanalyze",
                    () => EnqueueWorkItemAsync(analyzer, scope, highPriority), _shutdownToken);

                if (scope.HasMultipleDocuments)
                {
                    // log big reanalysis request from things like fix all, suppress all or option changes
                    // we are not interested in 1 file re-analysis request which can happen from like venus typing
                    var solution = _registration.GetSolutionToAnalyze();
                    SolutionCrawlerLogger.LogReanalyze(
                        CorrelationId, analyzer, scope.GetDocumentCount(solution), scope.GetLanguagesStringForTelemetry(solution), highPriority);
                }
            }

            private void OnActiveDocumentSwitched(object? sender, DocumentId? activeDocumentId)
            {
                if (activeDocumentId == null)
                    return;

                var solution = _registration.GetSolutionToAnalyze();
                EnqueueFullDocumentEvent(solution, activeDocumentId, InvocationReasons.ActiveDocumentSwitched, eventName: nameof(OnActiveDocumentSwitched));
            }

            private void OnWorkspaceChanged(object? sender, WorkspaceChangeEventArgs args)
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
                SolutionCrawlerLogger.LogWorkspaceEvent(_logAggregator, (int)args.Kind);

                // TODO: add telemetry that record how much it takes to process an event (max, min, average and etc)
                switch (args.Kind)
                {
                    case WorkspaceChangeKind.SolutionAdded:
                        EnqueueFullSolutionEvent(args.NewSolution, InvocationReasons.DocumentAdded, eventName);
                        break;

                    case WorkspaceChangeKind.SolutionChanged:
                    case WorkspaceChangeKind.SolutionReloaded:
                        EnqueueSolutionChangedEvent(args.OldSolution, args.NewSolution, eventName);
                        break;

                    case WorkspaceChangeKind.SolutionRemoved:
                        EnqueueFullSolutionEvent(args.OldSolution, InvocationReasons.SolutionRemoved, eventName);
                        break;

                    case WorkspaceChangeKind.SolutionCleared:
                        EnqueueFullSolutionEvent(args.OldSolution, InvocationReasons.DocumentRemoved, eventName);
                        break;

                    case WorkspaceChangeKind.ProjectAdded:
                        Contract.ThrowIfNull(args.ProjectId);
                        EnqueueFullProjectEvent(args.NewSolution, args.ProjectId, InvocationReasons.DocumentAdded, eventName);
                        break;

                    case WorkspaceChangeKind.ProjectChanged:
                    case WorkspaceChangeKind.ProjectReloaded:
                        Contract.ThrowIfNull(args.ProjectId);
                        EnqueueProjectChangedEvent(args.OldSolution, args.NewSolution, args.ProjectId, eventName);
                        break;

                    case WorkspaceChangeKind.ProjectRemoved:
                        Contract.ThrowIfNull(args.ProjectId);
                        EnqueueFullProjectEvent(args.OldSolution, args.ProjectId, InvocationReasons.DocumentRemoved, eventName);
                        break;

                    case WorkspaceChangeKind.DocumentAdded:
                        Contract.ThrowIfNull(args.DocumentId);
                        EnqueueFullDocumentEvent(args.NewSolution, args.DocumentId, InvocationReasons.DocumentAdded, eventName);
                        break;

                    case WorkspaceChangeKind.DocumentReloaded:
                    case WorkspaceChangeKind.DocumentChanged:
                        Contract.ThrowIfNull(args.DocumentId);
                        EnqueueDocumentChangedEvent(args.OldSolution, args.NewSolution, args.DocumentId, eventName);
                        break;

                    case WorkspaceChangeKind.DocumentRemoved:
                        Contract.ThrowIfNull(args.DocumentId);
                        EnqueueFullDocumentEvent(args.OldSolution, args.DocumentId, InvocationReasons.DocumentRemoved, eventName);
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
                        EnqueueFullProjectEvent(args.NewSolution, args.ProjectId, InvocationReasons.AdditionalDocumentChanged, eventName);
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(args.Kind);
                }
            }

            private void OnDocumentOpened(object? sender, DocumentEventArgs e)
            {
                _eventProcessingQueue.ScheduleTask("OnDocumentOpened",
                    () => EnqueueDocumentWorkItemAsync(e.Document.Project, e.Document.Id, e.Document, InvocationReasons.DocumentOpened), _shutdownToken);
            }

            private void OnDocumentClosed(object? sender, DocumentEventArgs e)
            {
                _eventProcessingQueue.ScheduleTask("OnDocumentClosed",
                    () => EnqueueDocumentWorkItemAsync(e.Document.Project, e.Document.Id, e.Document, InvocationReasons.DocumentClosed), _shutdownToken);
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
                            await EnqueueFullProjectWorkItemAsync(addedProject, InvocationReasons.DocumentAdded).ConfigureAwait(false);
                        }

                        foreach (var projectChanges in solutionChanges.GetProjectChanges())
                        {
                            await EnqueueWorkItemAsync(projectChanges).ConfigureAwait(continueOnCapturedContext: false);
                        }

                        foreach (var removedProject in solutionChanges.GetRemovedProjects())
                        {
                            await EnqueueFullProjectWorkItemAsync(removedProject, InvocationReasons.DocumentRemoved).ConfigureAwait(false);
                        }
                    },
                    _shutdownToken);
            }

            private void EnqueueFullSolutionEvent(Solution solution, InvocationReasons invocationReasons, string eventName)
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

            private void EnqueueFullProjectEvent(Solution solution, ProjectId projectId, InvocationReasons invocationReasons, string eventName)
            {
                _eventProcessingQueue.ScheduleTask(eventName,
                    () => EnqueueFullProjectWorkItemAsync(solution.GetRequiredProject(projectId), invocationReasons), _shutdownToken);
            }

            private void EnqueueFullDocumentEvent(Solution solution, DocumentId documentId, InvocationReasons invocationReasons, string eventName)
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
                        if (_syntaxTreeConfigurationService is { EnableOpeningSourceGeneratedFilesInWorkspace: true })
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
                                    EnqueueFullDocumentEvent(oldSolution, oldDocumentId, InvocationReasons.DocumentRemoved, "OnWorkspaceChanged");
                                }
                            }

                            foreach (var (newDocumentId, newDocument) in newProjectSourceGeneratedDocumentsById)
                            {
                                if (!oldProjectSourceGeneratedDocumentsById.TryGetValue(newDocumentId, out var oldDocument))
                                {
                                    // This source generated document was added
                                    EnqueueFullDocumentEvent(newSolution, newDocumentId, InvocationReasons.DocumentAdded, "OnWorkspaceChanged");
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

            private async Task EnqueueDocumentWorkItemAsync(Project project, DocumentId documentId, Document? document, InvocationReasons invocationReasons, SyntaxNode? changedMember = null)
            {
                // we are shutting down
                _shutdownToken.ThrowIfCancellationRequested();

                var priorityService = project.GetLanguageService<IWorkCoordinatorPriorityService>();
                document ??= project.GetDocument(documentId);
                var isLowPriority = priorityService != null && document != null && await priorityService.IsLowPriorityAsync(document, _shutdownToken).ConfigureAwait(false);

                var currentMember = GetSyntaxPath(changedMember);

                // call to this method is serialized. and only this method does the writing.
                _documentAndProjectWorkerProcessor.Enqueue(
                    new WorkItem(documentId, project.Language, invocationReasons, isLowPriority, currentMember, _listener.BeginAsyncOperation("WorkItem")));

                // enqueue semantic work planner
                if (invocationReasons.Contains(PredefinedInvocationReasons.SemanticChanged))
                {
                    // must use "Document" here so that the snapshot doesn't go away. we need the snapshot to calculate p2p dependency graph later.
                    // due to this, we might hold onto solution (and things kept alive by it) little bit longer than usual.
                    _semanticChangeProcessor.Enqueue(project, documentId, document, currentMember);
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

            private async Task EnqueueFullProjectWorkItemAsync(Project project, InvocationReasons invocationReasons)
            {
                foreach (var documentId in project.DocumentIds)
                    await EnqueueDocumentWorkItemAsync(project, documentId, document: null, invocationReasons).ConfigureAwait(false);

                foreach (var documentId in project.AdditionalDocumentIds)
                    await EnqueueDocumentWorkItemAsync(project, documentId, document: null, invocationReasons).ConfigureAwait(false);

                foreach (var documentId in project.AnalyzerConfigDocumentIds)
                    await EnqueueDocumentWorkItemAsync(project, documentId, document: null, invocationReasons).ConfigureAwait(false);

                // If all features are enabled for source generated documents, the solution crawler needs to
                // include them in incremental analysis.
                if (_syntaxTreeConfigurationService is { EnableOpeningSourceGeneratedFilesInWorkspace: true })
                {
                    foreach (var document in await project.GetSourceGeneratedDocumentsAsync(_shutdownToken).ConfigureAwait(false))
                        await EnqueueDocumentWorkItemAsync(project, document.Id, document, invocationReasons).ConfigureAwait(false);
                }
            }

            private async Task EnqueueWorkItemAsync(IIncrementalAnalyzer analyzer, ReanalyzeScope scope, bool highPriority)
            {
                var solution = _registration.GetSolutionToAnalyze();
                var invocationReasons = highPriority ? InvocationReasons.ReanalyzeHighPriority : InvocationReasons.Reanalyze;

                foreach (var (project, documentId) in scope.GetDocumentIds(solution))
                    await EnqueueWorkItemAsync(analyzer, project, documentId, document: null, invocationReasons).ConfigureAwait(false);
            }

            private async Task EnqueueWorkItemAsync(
                IIncrementalAnalyzer analyzer, Project project, DocumentId documentId, Document? document, InvocationReasons invocationReasons)
            {
                var priorityService = project.GetLanguageService<IWorkCoordinatorPriorityService>();
                var isLowPriority = priorityService != null && await priorityService.IsLowPriorityAsync(
                    GetRequiredDocument(project, documentId, document), _shutdownToken).ConfigureAwait(false);

                _documentAndProjectWorkerProcessor.Enqueue(
                    new WorkItem(documentId, project.Language, invocationReasons,
                        isLowPriority, analyzer, _listener.BeginAsyncOperation("WorkItem")));
            }

            private async Task EnqueueWorkItemAsync(ProjectChanges projectChanges)
            {
                await EnqueueProjectConfigurationChangeWorkItemAsync(projectChanges).ConfigureAwait(false);

                foreach (var addedDocumentId in projectChanges.GetAddedDocuments())
                    await EnqueueDocumentWorkItemAsync(projectChanges.NewProject, addedDocumentId, document: null, InvocationReasons.DocumentAdded).ConfigureAwait(false);

                foreach (var changedDocumentId in projectChanges.GetChangedDocuments())
                {
                    await EnqueueChangedDocumentWorkItemAsync(projectChanges.OldProject.GetRequiredDocument(changedDocumentId), projectChanges.NewProject.GetRequiredDocument(changedDocumentId))
                        .ConfigureAwait(continueOnCapturedContext: false);
                }

                foreach (var removedDocumentId in projectChanges.GetRemovedDocuments())
                    await EnqueueDocumentWorkItemAsync(projectChanges.OldProject, removedDocumentId, document: null, InvocationReasons.DocumentRemoved).ConfigureAwait(false);
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
                    !object.Equals(oldProject.OutputRefFilePath, newProject.OutputRefFilePath) ||
                    !oldProject.CompilationOutputInfo.Equals(newProject.CompilationOutputInfo) ||
                    oldProject.State.RunAnalyzers != newProject.State.RunAnalyzers)
                {
                    projectConfigurationChange = projectConfigurationChange.With(InvocationReasons.ProjectConfigurationChanged);
                }

                if (!projectConfigurationChange.IsEmpty)
                {
                    await EnqueueFullProjectWorkItemAsync(projectChanges.NewProject, projectConfigurationChange).ConfigureAwait(false);
                }
            }

            private async Task EnqueueChangedDocumentWorkItemAsync(Document oldDocument, Document newDocument)
            {
                var differenceService = newDocument.GetLanguageService<IDocumentDifferenceService>();

                if (differenceService == null)
                {
                    // For languages that don't use a Roslyn syntax tree, they don't export a document difference service.
                    // The whole document should be considered as changed in that case.
                    await EnqueueDocumentWorkItemAsync(newDocument.Project, newDocument.Id, newDocument, InvocationReasons.DocumentChanged).ConfigureAwait(false);
                }
                else
                {
                    var differenceResult = await differenceService.GetDifferenceAsync(oldDocument, newDocument, _shutdownToken).ConfigureAwait(false);

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
                private readonly WorkCoordinator _workCoordinator;

                internal TestAccessor(WorkCoordinator workCoordinator)
                {
                    _workCoordinator = workCoordinator;
                }

                internal void WaitUntilCompletion(ImmutableArray<IIncrementalAnalyzer> workers)
                {
                    var solution = _workCoordinator._registration.GetSolutionToAnalyze();
                    var list = new List<WorkItem>();

                    foreach (var project in solution.Projects)
                    {
                        foreach (var document in project.Documents)
                        {
                            list.Add(new WorkItem(document.Id, document.Project.Language, InvocationReasons.DocumentAdded, isLowPriority: false, activeMember: null, EmptyAsyncToken.Instance));
                        }
                    }

                    _workCoordinator._documentAndProjectWorkerProcessor.GetTestAccessor().WaitUntilCompletion(workers, list);
                }

                internal void WaitUntilCompletion()
                    => _workCoordinator._documentAndProjectWorkerProcessor.GetTestAccessor().WaitUntilCompletion();
            }
        }

        internal readonly struct ReanalyzeScope
        {
            private readonly SolutionId? _solutionId;
            private readonly ISet<object>? _projectOrDocumentIds;

            public ReanalyzeScope(SolutionId solutionId)
            {
                _solutionId = solutionId;
                _projectOrDocumentIds = null;
            }

            public ReanalyzeScope(IEnumerable<ProjectId>? projectIds = null, IEnumerable<DocumentId>? documentIds = null)
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
                    foreach (var projectState in solution.State.ProjectStates)
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
}
