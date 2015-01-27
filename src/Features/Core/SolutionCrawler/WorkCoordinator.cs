// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Versions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    internal partial class WorkCoordinatorRegistrationService
    {
        private partial class WorkCoordinator
        {
            private const int MinimumDelayInMS = 50;

            private readonly LogAggregator logAggregator;
            private readonly IAsynchronousOperationListener listener;
            private readonly IOptionService optionService;

            private readonly int correlationId;
            private readonly Workspace workspace;

            private readonly CancellationTokenSource shutdownNotificationSource;
            private readonly CancellationToken shutdownToken;
            private readonly SimpleTaskQueue eventProcessingQueue;

            // points to processor task
            private readonly IncrementalAnalyzerProcessor documentAndProjectWorkerProcessor;
            private readonly SemanticChangeProcessor semanticChangeProcessor;

            public WorkCoordinator(
                 IAsynchronousOperationListener listener,
                 IEnumerable<Lazy<IIncrementalAnalyzerProvider, IncrementalAnalyzerProviderMetadata>> analyzerProviders,
                 int correlationId, Workspace workspace)
            {
                this.logAggregator = new LogAggregator();

                this.listener = listener;
                this.optionService = workspace.Services.GetService<IOptionService>();
                this.optionService.OptionChanged += OnOptionChanged;

                // set up workspace 
                this.correlationId = correlationId;
                this.workspace = workspace;

                // event and worker queues
                this.shutdownNotificationSource = new CancellationTokenSource();
                this.shutdownToken = this.shutdownNotificationSource.Token;

                this.eventProcessingQueue = new SimpleTaskQueue(TaskScheduler.Default);

                var activeFileBackOffTimeSpanInMS = optionService.GetOption(SolutionCrawlerOptions.ActiveFileWorkerBackOffTimeSpanInMS);
                var allFilesWorkerBackOffTimeSpanInMS = optionService.GetOption(SolutionCrawlerOptions.AllFilesWorkerBackOffTimeSpanInMS);
                var entireProjectWorkerBackOffTimeSpanInMS = optionService.GetOption(SolutionCrawlerOptions.EntireProjectWorkerBackOffTimeSpanInMS);
                this.documentAndProjectWorkerProcessor = new IncrementalAnalyzerProcessor(
                    listener, correlationId, workspace, analyzerProviders, activeFileBackOffTimeSpanInMS, allFilesWorkerBackOffTimeSpanInMS, entireProjectWorkerBackOffTimeSpanInMS, shutdownToken);

                var semanticBackOffTimeSpanInMS = optionService.GetOption(SolutionCrawlerOptions.SemanticChangeBackOffTimeSpanInMS);
                var projectBackOffTimeSpanInMS = optionService.GetOption(SolutionCrawlerOptions.ProjectPropagationBackOffTimeSpanInMS);

                this.semanticChangeProcessor = new SemanticChangeProcessor(listener, correlationId, workspace, documentAndProjectWorkerProcessor, semanticBackOffTimeSpanInMS, projectBackOffTimeSpanInMS, shutdownToken);

                // if option is on
                if (optionService.GetOption(SolutionCrawlerOptions.SolutionCrawler))
                {
                    this.workspace.WorkspaceChanged += OnWorkspaceChanged;
                    this.workspace.DocumentOpened += OnDocumentOpened;
                    this.workspace.DocumentClosed += OnDocumentClosed;
                }
            }

            public int CorrelationId
            {
                get { return this.correlationId; }
            }

            public void Shutdown(bool blockingShutdown)
            {
                this.optionService.OptionChanged -= OnOptionChanged;

                // detach from the workspace
                this.workspace.WorkspaceChanged -= OnWorkspaceChanged;
                this.workspace.DocumentOpened -= OnDocumentOpened;
                this.workspace.DocumentClosed -= OnDocumentClosed;

                // cancel any pending blocks
                this.shutdownNotificationSource.Cancel();

                this.documentAndProjectWorkerProcessor.Shutdown();

                SolutionCrawlerLogger.LogWorkCoordiantorShutdown(this.correlationId, this.logAggregator);

                if (blockingShutdown)
                {
                    var shutdownTask = Task.WhenAll(
                        this.eventProcessingQueue.LastScheduledTask,
                        this.documentAndProjectWorkerProcessor.AsyncProcessorTask,
                        this.semanticChangeProcessor.AsyncProcessorTask);

                    shutdownTask.Wait(TimeSpan.FromSeconds(5));

                    if (!shutdownTask.IsCompleted)
                    {
                        SolutionCrawlerLogger.LogWorkCoordiantorShutdownTimeout(this.correlationId);
                    }
                }
            }

            private void OnOptionChanged(object sender, OptionChangedEventArgs e)
            {
                // if solution crawler got turned off or on.
                if (e.Option == SolutionCrawlerOptions.SolutionCrawler)
                {
                    var value = (bool)e.Value;
                    if (value)
                    {
                        this.workspace.WorkspaceChanged += OnWorkspaceChanged;
                        this.workspace.DocumentOpened += OnDocumentOpened;
                        this.workspace.DocumentClosed += OnDocumentClosed;
                    }
                    else
                    {
                        this.workspace.WorkspaceChanged -= OnWorkspaceChanged;
                        this.workspace.DocumentOpened -= OnDocumentOpened;
                        this.workspace.DocumentClosed -= OnDocumentClosed;
                    }

                    SolutionCrawlerLogger.LogOptionChanged(this.correlationId, value);
                    return;
                }

                ReanalyzeOnOptionChange(sender, e);
            }

            private void ReanalyzeOnOptionChange(object sender, OptionChangedEventArgs e)
            {
                // otherwise, let each analyzer decide what they want on option change
                ISet<DocumentId> set = null;
                foreach (var analyzer in this.documentAndProjectWorkerProcessor.Analyzers)
                {
                    if (analyzer.NeedsReanalysisOnOptionChanged(sender, e))
                    {
                        set = set ?? workspace.CurrentSolution.Projects.SelectMany(p => p.DocumentIds).ToSet();
                        this.Reanalyze(analyzer, set);
                    }
                }
            }

            public void Reanalyze(IIncrementalAnalyzer analyzer, IEnumerable<DocumentId> documentIds)
            {
                var asyncToken = this.listener.BeginAsyncOperation("Reanalyze");
                this.eventProcessingQueue.ScheduleTask(
                    () => EnqueueWorkItem(analyzer, documentIds), this.shutdownToken).CompletesAsyncOperation(asyncToken);

                SolutionCrawlerLogger.LogReanalyze(this.correlationId, analyzer, documentIds);
            }

            private void OnWorkspaceChanged(object sender, WorkspaceChangeEventArgs args)
            {
                // guard us from cancellation
                try
                {
                    ProcessEvents(args, this.listener.BeginAsyncOperation("OnWorkspaceChanged"));
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
                return oce.CancellationToken == this.shutdownToken;
            }

            private void ProcessEvents(WorkspaceChangeEventArgs args, IAsyncToken asyncToken)
            {
                SolutionCrawlerLogger.LogWorkspaceEvent(this.logAggregator, (int)args.Kind);

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
                EnqueueWorkItem(e.Document, InvocationReasons.DocumentOpened);
            }

            private void OnDocumentClosed(object sender, DocumentEventArgs e)
            {
                EnqueueWorkItem(e.Document, InvocationReasons.DocumentClosed);
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
                var asyncToken = this.listener.BeginAsyncOperation("OnSolutionAdded");
                this.eventProcessingQueue.ScheduleTask(() =>
                {
                    var semanticVersionTrackingService = solution.Workspace.Services.GetService<ISemanticVersionTrackingService>();
                    if (semanticVersionTrackingService != null)
                    {
                        semanticVersionTrackingService.LoadInitialSemanticVersions(solution);
                    }
                }, this.shutdownToken).CompletesAsyncOperation(asyncToken);
            }

            private void OnProjectAdded(Project project)
            {
                var asyncToken = this.listener.BeginAsyncOperation("OnProjectAdded");
                this.eventProcessingQueue.ScheduleTask(() =>
                {
                    var semanticVersionTrackingService = project.Solution.Workspace.Services.GetService<ISemanticVersionTrackingService>();
                    if (semanticVersionTrackingService != null)
                    {
                        semanticVersionTrackingService.LoadInitialSemanticVersions(project);
                    }
                }, this.shutdownToken).CompletesAsyncOperation(asyncToken);
            }

            private void EnqueueEvent(Solution oldSolution, Solution newSolution, IAsyncToken asyncToken)
            {
                this.eventProcessingQueue.ScheduleTask(
                    () => EnqueueWorkItemAsync(oldSolution, newSolution), this.shutdownToken).CompletesAsyncOperation(asyncToken);
            }

            private void EnqueueEvent(Solution solution, InvocationReasons invocationReasons, IAsyncToken asyncToken)
            {
                var task = this.eventProcessingQueue.ScheduleTask(
                    () => EnqueueWorkItemForSolution(solution, invocationReasons), this.shutdownToken).CompletesAsyncOperation(asyncToken);
            }

            private void EnqueueEvent(Solution oldSolution, Solution newSolution, ProjectId projectId, IAsyncToken asyncToken)
            {
                this.eventProcessingQueue.ScheduleTask(
                    () => EnqueueWorkItemAfterDiffAsync(oldSolution, newSolution, projectId), this.shutdownToken).CompletesAsyncOperation(asyncToken);
            }

            private void EnqueueEvent(Solution solution, ProjectId projectId, InvocationReasons invocationReasons, IAsyncToken asyncToken)
            {
                this.eventProcessingQueue.ScheduleTask(
                    () => EnqueueWorkItemForProject(solution, projectId, invocationReasons), this.shutdownToken).CompletesAsyncOperation(asyncToken);
            }

            private void EnqueueEvent(Solution solution, DocumentId documentId, InvocationReasons invocationReasons, IAsyncToken asyncToken)
            {
                this.eventProcessingQueue.ScheduleTask(
                    () => EnqueueWorkItemForDocument(solution, documentId, invocationReasons), this.shutdownToken).CompletesAsyncOperation(asyncToken);
            }

            private void EnqueueEvent(Solution oldSolution, Solution newSolution, DocumentId documentId, IAsyncToken asyncToken)
            {
                // document changed event is the special one.
                this.eventProcessingQueue.ScheduleTask(
                    () => EnqueueWorkItemAfterDiffAsync(oldSolution, newSolution, documentId), this.shutdownToken).CompletesAsyncOperation(asyncToken);
            }

            private void EnqueueWorkItem(Document document, InvocationReasons invocationReasons, SyntaxNode changedMember = null)
            {
                // we are shutting down
                this.shutdownToken.ThrowIfCancellationRequested();

                var priorityService = document.GetLanguageService<IWorkCoordinatorPriorityService>();

                var currentMember = GetSyntaxPath(changedMember);

                // call to this method is serialized. and only this method does the writing.
                this.documentAndProjectWorkerProcessor.Enqueue(
                    new WorkItem(document.Id, document.Project.Language, invocationReasons,
                    priorityService != null && priorityService.IsLowPriority(document),
                    currentMember, this.listener.BeginAsyncOperation("WorkItem")));

                // enqueue semantic work planner
                if (invocationReasons.Contains(PredefinedInvocationReasons.SemanticChanged))
                {
                    // must use "Document" here so that the snapshot doesn't go away. we need the snapshot to calculate p2p dependency graph later.
                    // due to this, we might hold onto solution (and things kept alive by it) little bit longer than usual.
                    this.semanticChangeProcessor.Enqueue(document, currentMember);
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

            private void EnqueueWorkItem(Project project, InvocationReasons invocationReasons)
            {
                foreach (var documentId in project.DocumentIds)
                {
                    var document = project.GetDocument(documentId);
                    EnqueueWorkItem(document, invocationReasons);
                }
            }

            private void EnqueueWorkItem(IIncrementalAnalyzer analyzer, IEnumerable<DocumentId> documentIds)
            {
                var solution = this.workspace.CurrentSolution;
                foreach (var documentId in documentIds)
                {
                    var document = solution.GetDocument(documentId);
                    if (document == null)
                    {
                        continue;
                    }

                    var priorityService = document.GetLanguageService<IWorkCoordinatorPriorityService>();

                    this.documentAndProjectWorkerProcessor.Enqueue(
                        new WorkItem(documentId, document.Project.Language, InvocationReasons.Reanalyze,
                        priorityService != null && priorityService.IsLowPriority(document),
                        analyzer, this.listener.BeginAsyncOperation("WorkItem")));
                }
            }

            private async Task EnqueueWorkItemAsync(Solution oldSolution, Solution newSolution)
            {
                var solutionChanges = newSolution.GetChanges(oldSolution);

                // TODO: Async version for GetXXX methods?
                foreach (var addedProject in solutionChanges.GetAddedProjects())
                {
                    EnqueueWorkItem(addedProject, InvocationReasons.DocumentAdded);
                }

                foreach (var projectChanges in solutionChanges.GetProjectChanges())
                {
                    await EnqueueWorkItemAsync(projectChanges).ConfigureAwait(continueOnCapturedContext: false);
                }

                foreach (var removedProject in solutionChanges.GetRemovedProjects())
                {
                    EnqueueWorkItem(removedProject, InvocationReasons.DocumentRemoved);
                }
            }

            private async Task EnqueueWorkItemAsync(ProjectChanges projectChanges)
            {
                EnqueueProjectConfigurationChangeWorkItem(projectChanges);

                foreach (var addedDocumentId in projectChanges.GetAddedDocuments())
                {
                    EnqueueWorkItem(projectChanges.NewProject.GetDocument(addedDocumentId), InvocationReasons.DocumentAdded);
                }

                foreach (var changedDocumentId in projectChanges.GetChangedDocuments())
                {
                    await EnqueueWorkItemAsync(projectChanges.OldProject.GetDocument(changedDocumentId), projectChanges.NewProject.GetDocument(changedDocumentId))
                        .ConfigureAwait(continueOnCapturedContext: false);
                }

                foreach (var removedDocumentId in projectChanges.GetRemovedDocuments())
                {
                    EnqueueWorkItem(projectChanges.OldProject.GetDocument(removedDocumentId), InvocationReasons.DocumentRemoved);
                }
            }

            private void EnqueueProjectConfigurationChangeWorkItem(ProjectChanges projectChanges)
            {
                var oldProject = projectChanges.OldProject;
                var newProject = projectChanges.NewProject;

                // TODO: why solution changes return Project not ProjectId but ProjectChanges return DocumentId not Document?
                var projectConfigurationChange = InvocationReasons.Empty;

                if (!object.Equals(oldProject.ParseOptions, newProject.ParseOptions))
                {
                    projectConfigurationChange = projectConfigurationChange.With(InvocationReasons.ProjectParseOptionChanged);
                }

                if (projectChanges.GetAddedMetadataReferences().Any() || projectChanges.GetAddedProjectReferences().Any() || projectChanges.GetAddedAnalyzerReferences().Any() ||
                    projectChanges.GetRemovedMetadataReferences().Any() || projectChanges.GetRemovedProjectReferences().Any() || projectChanges.GetRemovedAnalyzerReferences().Any() ||
                    !object.Equals(oldProject.CompilationOptions, newProject.CompilationOptions))
                {
                    projectConfigurationChange = projectConfigurationChange.With(InvocationReasons.ProjectConfigurationChanged);
                }

                if (!projectConfigurationChange.IsEmpty)
                {
                    EnqueueWorkItem(projectChanges.NewProject, projectConfigurationChange);
                }
            }

            private async Task EnqueueWorkItemAsync(Document oldDocument, Document newDocument)
            {
                var differenceService = newDocument.GetLanguageService<IDocumentDifferenceService>();
                if (differenceService != null)
                {
                    var differenceResult = await differenceService.GetDifferenceAsync(oldDocument, newDocument, this.shutdownToken).ConfigureAwait(false);

                    if (differenceResult != null)
                    {
                        EnqueueWorkItem(newDocument, differenceResult.ChangeType, differenceResult.ChangedMember);
                    }
                }
            }

            private void EnqueueWorkItemForDocument(Solution solution, DocumentId documentId, InvocationReasons invocationReasons)
            {
                var document = solution.GetDocument(documentId);

                EnqueueWorkItem(document, invocationReasons);
            }

            private void EnqueueWorkItemForProject(Solution solution, ProjectId projectId, InvocationReasons invocationReasons)
            {
                var project = solution.GetProject(projectId);

                EnqueueWorkItem(project, invocationReasons);
            }

            private void EnqueueWorkItemForSolution(Solution solution, InvocationReasons invocationReasons)
            {
                foreach (var projectId in solution.ProjectIds)
                {
                    EnqueueWorkItemForProject(solution, projectId, invocationReasons);
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
                var solution = this.workspace.CurrentSolution;
                var list = new List<WorkItem>();

                foreach (var project in solution.Projects)
                {
                    foreach (var document in project.Documents)
                    {
                        list.Add(
                            new WorkItem(document.Id, document.Project.Language, InvocationReasons.DocumentAdded, false, this.listener.BeginAsyncOperation("WorkItem")));
                    }
                }

                this.documentAndProjectWorkerProcessor.WaitUntilCompletion_ForTestingPurposesOnly(workers, list);
            }

            internal void WaitUntilCompletion_ForTestingPurposesOnly()
            {
                this.documentAndProjectWorkerProcessor.WaitUntilCompletion_ForTestingPurposesOnly();
            }
        }
    }
}
