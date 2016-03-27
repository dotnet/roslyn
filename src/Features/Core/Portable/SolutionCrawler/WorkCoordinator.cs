// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Versions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    internal partial class SolutionCrawlerRegistrationService
    {
        private partial class WorkCoordinator
        {
            private const int MinimumDelayInMS = 50;

            private readonly Registration _registration;

            private readonly LogAggregator _logAggregator;
            private readonly IAsynchronousOperationListener _listener;
            private readonly IOptionService _optionService;

            private readonly CancellationTokenSource _shutdownNotificationSource;
            private readonly CancellationToken _shutdownToken;
            private readonly SimpleTaskQueue _eventProcessingQueue;

            // points to processor task
            private readonly IncrementalAnalyzerProcessor _documentAndProjectWorkerProcessor;
            private readonly SemanticChangeProcessor _semanticChangeProcessor;

            public WorkCoordinator(
                 IAsynchronousOperationListener listener,
                 IEnumerable<Lazy<IIncrementalAnalyzerProvider, IncrementalAnalyzerProviderMetadata>> analyzerProviders,
                 Registration registration)
            {
                _logAggregator = new LogAggregator();

                _registration = registration;

                _listener = listener;
                _optionService = _registration.GetService<IOptionService>();
                _optionService.OptionChanged += OnOptionChanged;

                // event and worker queues
                _shutdownNotificationSource = new CancellationTokenSource();
                _shutdownToken = _shutdownNotificationSource.Token;

                _eventProcessingQueue = new SimpleTaskQueue(TaskScheduler.Default);

                var activeFileBackOffTimeSpanInMS = _optionService.GetOption(InternalSolutionCrawlerOptions.ActiveFileWorkerBackOffTimeSpanInMS);
                var allFilesWorkerBackOffTimeSpanInMS = _optionService.GetOption(InternalSolutionCrawlerOptions.AllFilesWorkerBackOffTimeSpanInMS);
                var entireProjectWorkerBackOffTimeSpanInMS = _optionService.GetOption(InternalSolutionCrawlerOptions.EntireProjectWorkerBackOffTimeSpanInMS);

                _documentAndProjectWorkerProcessor = new IncrementalAnalyzerProcessor(
                    listener, analyzerProviders, _registration,
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
            }

            public int CorrelationId
            {
                get { return _registration.CorrelationId; }
            }

            public void Shutdown(bool blockingShutdown)
            {
                _optionService.OptionChanged -= OnOptionChanged;

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

                // TODO: remove this once prototype is done
                //       it is here just because it was convenient to add per workspace option change monitoring 
                //       for incremental analyzer
                if (e.Option == Diagnostics.InternalDiagnosticsOptions.UseDiagnosticEngineV2)
                {
                    _documentAndProjectWorkerProcessor.ChangeDiagnosticsEngine((bool)e.Value);
                }

                ReanalyzeOnOptionChange(sender, e);
            }

            private void ReanalyzeOnOptionChange(object sender, OptionChangedEventArgs e)
            {
                // otherwise, let each analyzer decide what they want on option change
                ISet<DocumentId> set = null;
                foreach (var analyzer in _documentAndProjectWorkerProcessor.Analyzers)
                {
                    if (analyzer.NeedsReanalysisOnOptionChanged(sender, e))
                    {
                        set = set ?? _registration.CurrentSolution.Projects.SelectMany(p => p.DocumentIds).ToSet();
                        this.Reanalyze(analyzer, set);
                    }
                }
            }

            public void Reanalyze(IIncrementalAnalyzer analyzer, IEnumerable<DocumentId> documentIds, bool highPriority = false)
            {
                var asyncToken = _listener.BeginAsyncOperation("Reanalyze");
                _eventProcessingQueue.ScheduleTask(
                    () => EnqueueWorkItemAsync(analyzer, documentIds, highPriority), _shutdownToken).CompletesAsyncOperation(asyncToken);

                SolutionCrawlerLogger.LogReanalyze(CorrelationId, analyzer, documentIds, highPriority);
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
                        ProcessDocumentEvent(args, asyncToken);
                        break;
                    default:
                        throw ExceptionUtilities.Unreachable;
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
                        // If an additional file has changed we need to reanalyze the entire project.
                        EnqueueEvent(e.NewSolution, e.ProjectId, InvocationReasons.AdditionalDocumentChanged, asyncToken);
                        break;

                    default:
                        throw ExceptionUtilities.Unreachable;
                }
            }

            private void ProcessProjectEvent(WorkspaceChangeEventArgs e, IAsyncToken asyncToken)
            {
                switch (e.Kind)
                {
                    case WorkspaceChangeKind.ProjectAdded:
                        OnProjectAdded(e.NewSolution.GetProject(e.ProjectId));
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
                        throw ExceptionUtilities.Unreachable;
                }
            }

            private void ProcessSolutionEvent(WorkspaceChangeEventArgs e, IAsyncToken asyncToken)
            {
                switch (e.Kind)
                {
                    case WorkspaceChangeKind.SolutionAdded:
                        OnSolutionAdded(e.NewSolution);
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
                        throw ExceptionUtilities.Unreachable;
                }
            }

            private void OnSolutionAdded(Solution solution)
            {
                // first make sure full solution analysis is on.
                _optionService.SetOptions(solution.Workspace.Options.WithChangedOption(RuntimeOptions.FullSolutionAnalysis, true));

                var asyncToken = _listener.BeginAsyncOperation("OnSolutionAdded");
                _eventProcessingQueue.ScheduleTask(() =>
                {
                    var semanticVersionTrackingService = solution.Workspace.Services.GetService<ISemanticVersionTrackingService>();
                    if (semanticVersionTrackingService != null)
                    {
                        semanticVersionTrackingService.LoadInitialSemanticVersions(solution);
                    }
                }, _shutdownToken).CompletesAsyncOperation(asyncToken);
            }

            private void OnProjectAdded(Project project)
            {
                var asyncToken = _listener.BeginAsyncOperation("OnProjectAdded");
                _eventProcessingQueue.ScheduleTask(() =>
                {
                    var semanticVersionTrackingService = project.Solution.Workspace.Services.GetService<ISemanticVersionTrackingService>();
                    if (semanticVersionTrackingService != null)
                    {
                        semanticVersionTrackingService.LoadInitialSemanticVersions(project);
                    }
                }, _shutdownToken).CompletesAsyncOperation(asyncToken);
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

            private async Task EnqueueWorkItemAsync(IIncrementalAnalyzer analyzer, IEnumerable<DocumentId> documentIds, bool highPriority)
            {
                var solution = _registration.CurrentSolution;
                foreach (var documentId in documentIds)
                {
                    var document = solution.GetDocument(documentId);
                    if (document == null)
                    {
                        continue;
                    }

                    var priorityService = document.GetLanguageService<IWorkCoordinatorPriorityService>();
                    var isLowPriority = priorityService != null && await priorityService.IsLowPriorityAsync(document, _shutdownToken).ConfigureAwait(false);

                    var invocationReasons = highPriority ? InvocationReasons.ReanalyzeHighPriority : InvocationReasons.Reanalyze;

                    _documentAndProjectWorkerProcessor.Enqueue(
                        new WorkItem(documentId, document.Project.Language, invocationReasons,
                        isLowPriority, analyzer, _listener.BeginAsyncOperation("WorkItem")));
                }
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
                    !object.Equals(oldProject.AnalyzerOptions, newProject.AnalyzerOptions))
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
                if (differenceService != null)
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
    }
}
