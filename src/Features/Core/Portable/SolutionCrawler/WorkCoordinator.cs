﻿// Licensed to the .NET Foundation under one or more agreements.
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
            private readonly IDocumentTrackingService? _documentTrackingService;

            private readonly CancellationTokenSource _shutdownNotificationSource;
            private readonly CancellationToken _shutdownToken;
            private readonly TaskQueue _eventProcessingQueue;

            // points to processor task
            private readonly IncrementalAnalyzerProcessor _documentAndProjectWorkerProcessor;
            private readonly SemanticChangeProcessor _semanticChangeProcessor;

            private Document? _lastActiveDocument;

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
                _documentTrackingService = _registration.Workspace.Services.GetService<IDocumentTrackingService>();

                // event and worker queues
                _shutdownNotificationSource = new CancellationTokenSource();
                _shutdownToken = _shutdownNotificationSource.Token;

                _eventProcessingQueue = new TaskQueue(listener, TaskScheduler.Default);

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
                // if solution crawler got turned off or on.
                if (e.Option == InternalSolutionCrawlerOptions.SolutionCrawler)
                {
                    Contract.ThrowIfNull(e.Value);

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

                if (!_optionService.GetOption(InternalSolutionCrawlerOptions.SolutionCrawler))
                {
                    // Bail out if solution crawler is disabled.
                    return;
                }

                ReanalyzeOnOptionChange(sender, e);
            }

            private void ReanalyzeOnOptionChange(object? sender, OptionChangedEventArgs e)
            {
                // get off from option changed event handler since it runs on UI thread
                // getting analyzer can be slow for the very first time since it is lazily initialized
                _eventProcessingQueue.ScheduleTask(nameof(ReanalyzeOnOptionChange), () =>
                {
                    // Force analyze all analyzers if background analysis scope has changed.
                    var forceAnalyze = e.Option == SolutionCrawlerOptions.BackgroundAnalysisScopeOption;

                    // let each analyzer decide what they want on option change
                    foreach (var analyzer in _documentAndProjectWorkerProcessor.Analyzers)
                    {
                        if (forceAnalyze || analyzer.NeedsReanalysisOnOptionChanged(sender, e))
                        {
                            var scope = new ReanalyzeScope(_registration.CurrentSolution.Id);
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
                    var solution = _registration.CurrentSolution;
                    SolutionCrawlerLogger.LogReanalyze(
                        CorrelationId, analyzer, scope.GetDocumentCount(solution), scope.GetLanguagesStringForTelemetry(solution), highPriority);
                }
            }

            private void OnActiveDocumentChanged(object? sender, DocumentId activeDocumentId)
            {
                var solution = _registration.Workspace.CurrentSolution;

                // Check if we are only performing backgroung analysis for active file.
                if (activeDocumentId != null)
                {
                    // Change to active document needs to trigger following events in active file analysis scope:
                    //  1. Request analysis for newly active file, similar to a newly opened file.
                    //  2. Clear analysis data for prior active file, similar to a closed file.
                    // Note that if 'activeDocumentId' is null, i.e. user navigated to a non-source file,
                    // we are treating it as a no-op here.
                    // As soon as user switches to a source document, we will perform the appropriate analysis callbacks
                    // on the next active document changed event.
                    var activeDocument = solution.GetDocument(activeDocumentId);
                    if (activeDocument != null &&
                        SolutionCrawlerOptions.GetBackgroundAnalysisScope(activeDocument.Project) == BackgroundAnalysisScope.ActiveFile)
                    {
                        lock (_gate)
                        {
                            if (_lastActiveDocument != null)
                            {
                                EnqueueEvent(_lastActiveDocument.Project.Solution, _lastActiveDocument.Id, InvocationReasons.DocumentClosed, "OnDocumentClosed");
                            }

                            _lastActiveDocument = activeDocument;
                        }

                        EnqueueEvent(activeDocument.Project.Solution, activeDocument.Id, InvocationReasons.DocumentOpened, "OnDocumentOpened");
                    }
                }
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
                => oce.CancellationToken == _shutdownToken;

            private void ProcessEvent(WorkspaceChangeEventArgs args, string eventName)
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
                        ProcessSolutionEvent(args, eventName);
                        break;
                    case WorkspaceChangeKind.ProjectAdded:
                    case WorkspaceChangeKind.ProjectChanged:
                    case WorkspaceChangeKind.ProjectReloaded:
                    case WorkspaceChangeKind.ProjectRemoved:
                        ProcessProjectEvent(args, eventName);
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
                        ProcessDocumentEvent(args, eventName);
                        break;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(args.Kind);
                }
            }

            private void OnDocumentOpened(object? sender, DocumentEventArgs e)
            {
                _eventProcessingQueue.ScheduleTask("OnDocumentOpened",
                    () => EnqueueWorkItemAsync(e.Document, InvocationReasons.DocumentOpened), _shutdownToken);
            }

            private void OnDocumentClosed(object? sender, DocumentEventArgs e)
            {
                _eventProcessingQueue.ScheduleTask("OnDocumentClosed",
                    () => EnqueueWorkItemAsync(e.Document, InvocationReasons.DocumentClosed), _shutdownToken);
            }

            private void ProcessDocumentEvent(WorkspaceChangeEventArgs e, string eventName)
            {
                switch (e.Kind)
                {
                    case WorkspaceChangeKind.DocumentAdded:
                        Contract.ThrowIfNull(e.DocumentId);
                        EnqueueEvent(e.NewSolution, e.DocumentId, InvocationReasons.DocumentAdded, eventName);
                        break;

                    case WorkspaceChangeKind.DocumentRemoved:
                        Contract.ThrowIfNull(e.DocumentId);
                        EnqueueEvent(e.OldSolution, e.DocumentId, InvocationReasons.DocumentRemoved, eventName);
                        break;

                    case WorkspaceChangeKind.DocumentReloaded:
                    case WorkspaceChangeKind.DocumentChanged:
                        Contract.ThrowIfNull(e.DocumentId);
                        EnqueueEvent(e.OldSolution, e.NewSolution, e.DocumentId, eventName);
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
                        Contract.ThrowIfNull(e.ProjectId);
                        EnqueueEvent(e.NewSolution, e.ProjectId, InvocationReasons.AdditionalDocumentChanged, eventName);
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(e.Kind);
                }
            }

            private void ProcessProjectEvent(WorkspaceChangeEventArgs e, string eventName)
            {
                switch (e.Kind)
                {
                    case WorkspaceChangeKind.ProjectAdded:
                        Contract.ThrowIfNull(e.ProjectId);
                        EnqueueEvent(e.NewSolution, e.ProjectId, InvocationReasons.DocumentAdded, eventName);
                        break;

                    case WorkspaceChangeKind.ProjectRemoved:
                        Contract.ThrowIfNull(e.ProjectId);
                        EnqueueEvent(e.OldSolution, e.ProjectId, InvocationReasons.DocumentRemoved, eventName);
                        break;

                    case WorkspaceChangeKind.ProjectChanged:
                    case WorkspaceChangeKind.ProjectReloaded:
                        Contract.ThrowIfNull(e.ProjectId);
                        EnqueueEvent(e.OldSolution, e.NewSolution, e.ProjectId, eventName);
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(e.Kind);
                }
            }

            private void ProcessSolutionEvent(WorkspaceChangeEventArgs e, string eventName)
            {
                switch (e.Kind)
                {
                    case WorkspaceChangeKind.SolutionAdded:
                        EnqueueEvent(e.NewSolution, InvocationReasons.DocumentAdded, eventName);
                        break;

                    case WorkspaceChangeKind.SolutionRemoved:
                        EnqueueEvent(e.OldSolution, InvocationReasons.SolutionRemoved, eventName);
                        break;

                    case WorkspaceChangeKind.SolutionCleared:
                        EnqueueEvent(e.OldSolution, InvocationReasons.DocumentRemoved, eventName);
                        break;

                    case WorkspaceChangeKind.SolutionChanged:
                    case WorkspaceChangeKind.SolutionReloaded:
                        EnqueueEvent(e.OldSolution, e.NewSolution, eventName);
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(e.Kind);
                }
            }

            private void EnqueueEvent(Solution oldSolution, Solution newSolution, string eventName)
            {
                _eventProcessingQueue.ScheduleTask(eventName,
                    () => EnqueueWorkItemAsync(oldSolution, newSolution), _shutdownToken);
            }

            private void EnqueueEvent(Solution solution, InvocationReasons invocationReasons, string eventName)
            {
                _eventProcessingQueue.ScheduleTask(eventName,
                    () => EnqueueWorkItemForSolutionAsync(solution, invocationReasons), _shutdownToken);
            }

            private void EnqueueEvent(Solution oldSolution, Solution newSolution, ProjectId projectId, string eventName)
            {
                _eventProcessingQueue.ScheduleTask(eventName,
                    () => EnqueueWorkItemAfterDiffAsync(oldSolution, newSolution, projectId), _shutdownToken);
            }

            private void EnqueueEvent(Solution solution, ProjectId projectId, InvocationReasons invocationReasons, string eventName)
            {
                _eventProcessingQueue.ScheduleTask(eventName,
                    () => EnqueueWorkItemForProjectAsync(solution, projectId, invocationReasons), _shutdownToken);
            }

            private void EnqueueEvent(Solution solution, DocumentId documentId, InvocationReasons invocationReasons, string eventName)
            {
                _eventProcessingQueue.ScheduleTask(eventName,
                    () => EnqueueWorkItemForDocumentAsync(solution, documentId, invocationReasons), _shutdownToken);
            }

            private void EnqueueEvent(Solution oldSolution, Solution newSolution, DocumentId documentId, string eventName)
            {
                // document changed event is the special one.
                _eventProcessingQueue.ScheduleTask(eventName,
                    () => EnqueueWorkItemAfterDiffAsync(oldSolution, newSolution, documentId), _shutdownToken);
            }

            private async Task EnqueueWorkItemAsync(Document document, InvocationReasons invocationReasons, SyntaxNode? changedMember = null)
            {
                // we are shutting down
                _shutdownToken.ThrowIfCancellationRequested();

                var priorityService = document.GetLanguageService<IWorkCoordinatorPriorityService>();
                var isLowPriority = priorityService != null && await priorityService.IsLowPriorityAsync(document, _shutdownToken).ConfigureAwait(false);

                var currentMember = GetSyntaxPath(changedMember);

                // call to this method is serialized. and only this method does the writing.
                _documentAndProjectWorkerProcessor.Enqueue(
                    new WorkItem(document.Id, document.Project.Language, invocationReasons, isLowPriority, currentMember, _listener.BeginAsyncOperation("WorkItem")));

                // enqueue semantic work planner
                if (invocationReasons.Contains(PredefinedInvocationReasons.SemanticChanged))
                {
                    // must use "Document" here so that the snapshot doesn't go away. we need the snapshot to calculate p2p dependency graph later.
                    // due to this, we might hold onto solution (and things kept alive by it) little bit longer than usual.
                    _semanticChangeProcessor.Enqueue(document, currentMember);
                }
            }

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

            private async Task EnqueueWorkItemAsync(Project project, InvocationReasons invocationReasons)
            {
                foreach (var documentId in project.DocumentIds)
                {
                    var document = project.GetRequiredDocument(documentId);
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
                    await EnqueueWorkItemAsync(projectChanges.NewProject.GetRequiredDocument(addedDocumentId), InvocationReasons.DocumentAdded).ConfigureAwait(false);
                }

                foreach (var changedDocumentId in projectChanges.GetChangedDocuments())
                {
                    await EnqueueWorkItemAsync(projectChanges.OldProject.GetRequiredDocument(changedDocumentId), projectChanges.NewProject.GetRequiredDocument(changedDocumentId))
                        .ConfigureAwait(continueOnCapturedContext: false);
                }

                foreach (var removedDocumentId in projectChanges.GetRemovedDocuments())
                {
                    await EnqueueWorkItemAsync(projectChanges.OldProject.GetRequiredDocument(removedDocumentId), InvocationReasons.DocumentRemoved).ConfigureAwait(false);
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
                    !object.Equals(oldProject.OutputRefFilePath, newProject.OutputRefFilePath) ||
                    !oldProject.CompilationOutputInfo.Equals(newProject.CompilationOutputInfo) ||
                    oldProject.State.RunAnalyzers != newProject.State.RunAnalyzers)
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
                var document = solution.GetRequiredDocument(documentId);

                return EnqueueWorkItemAsync(document, invocationReasons);
            }

            private Task EnqueueWorkItemForProjectAsync(Solution solution, ProjectId projectId, InvocationReasons invocationReasons)
            {
                var project = solution.GetRequiredProject(projectId);

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
                var oldProject = oldSolution.GetRequiredProject(projectId);
                var newProject = newSolution.GetRequiredProject(projectId);

                await EnqueueWorkItemAsync(newProject.GetChanges(oldProject)).ConfigureAwait(continueOnCapturedContext: false);
            }

            private async Task EnqueueWorkItemAfterDiffAsync(Solution oldSolution, Solution newSolution, DocumentId documentId)
            {
                var oldProject = oldSolution.GetRequiredProject(documentId.ProjectId);
                var newProject = newSolution.GetRequiredProject(documentId.ProjectId);

                await EnqueueWorkItemAsync(oldProject.GetRequiredDocument(documentId), newProject.GetRequiredDocument(documentId)).ConfigureAwait(continueOnCapturedContext: false);
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
                    var solution = _workCoordinator._registration.CurrentSolution;
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
                        count += projectState.Value.DocumentIds.Count;
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
