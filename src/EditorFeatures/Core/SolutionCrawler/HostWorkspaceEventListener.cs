// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    internal readonly struct SolutionCrawlerEvent
    {
        public readonly WorkspaceChangeEventArgs? WorkspaceChangeArgs;
        public readonly TextDocumentEventArgs? DocumentOpenArgs;
        public readonly TextDocumentEventArgs? DocumentCloseArgs;

        public SolutionCrawlerEvent(
            WorkspaceChangeEventArgs? workspaceChangeArgs,
            TextDocumentEventArgs? documentOpenArgs,
            TextDocumentEventArgs? documentCloseArgs)
        {
            if (workspaceChangeArgs != null)
            {
                Contract.ThrowIfTrue(workspaceChangeArgs.OldSolution.Workspace != workspaceChangeArgs.NewSolution.Workspace);
            }

            Contract.ThrowIfTrue(workspaceChangeArgs is null && documentOpenArgs is null && documentCloseArgs is null);

            this.WorkspaceChangeArgs = workspaceChangeArgs;
            this.DocumentOpenArgs = documentOpenArgs;
            this.DocumentCloseArgs = documentCloseArgs;
        }

        public Solution Solution => WorkspaceChangeArgs?.OldSolution ?? DocumentOpenArgs?.Document.Project.Solution ?? DocumentCloseArgs!.Document.Project.Solution;
        public Workspace Workspace => Solution.Workspace;
    }

    [ExportEventListener(WellKnownEventListeners.Workspace, WorkspaceKind.Host), Shared]
    internal sealed class HostWorkspaceEventListener : IEventListener<object>
    {
        private readonly IGlobalOptionService _globalOptions;
        private readonly IThreadingContext _threadingContext;
        private readonly ISolutionCrawlerEventsAggregationService _aggregationService;
        private readonly AsyncBatchingWorkQueue<SolutionCrawlerEvent> _eventQueue;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public HostWorkspaceEventListener(
            IGlobalOptionService globalOptions,
            IThreadingContext threadingContext,
            ISolutionCrawlerEventsAggregationService notificationService,
            IAsynchronousOperationListenerProvider listenerProvider)
        {
            _globalOptions = globalOptions;
            _threadingContext = threadingContext;
            _aggregationService = notificationService;
            _eventQueue = new AsyncBatchingWorkQueue<SolutionCrawlerEvent>(
                DelayTimeSpan.Medium,
                ProcessWorkspaceChangeEventsAsync,
                listenerProvider.GetListener(FeatureAttribute.SolutionCrawler),
                _threadingContext.DisposalToken);
        }

        public void StartListening(Workspace workspace, object? serviceOpt)
        {
            if (_globalOptions.GetOption(SolutionCrawlerRegistrationService.EnableSolutionCrawler))
            {
                workspace.WorkspaceChanged += OnWorkspaceChanged;
                workspace.TextDocumentOpened += OnDocumentOpened;
                workspace.TextDocumentClosed += OnDocumentClosed;
                _threadingContext.DisposalToken.Register(() =>
                {
                    workspace.TextDocumentClosed -= OnDocumentClosed;
                    workspace.TextDocumentOpened -= OnDocumentOpened;
                    workspace.WorkspaceChanged -= OnWorkspaceChanged;
                });
            }
        }

        private void OnWorkspaceChanged(object? sender, WorkspaceChangeEventArgs e)
            => _eventQueue.AddWork(new SolutionCrawlerEvent(e, null, null));

        private void OnDocumentOpened(object? sender, TextDocumentEventArgs e)
            => _eventQueue.AddWork(new SolutionCrawlerEvent(null, e, null));

        private void OnDocumentClosed(object? sender, TextDocumentEventArgs e)
            => _eventQueue.AddWork(new SolutionCrawlerEvent(null, null, e));

        private async ValueTask ProcessWorkspaceChangeEventsAsync(ImmutableSegmentedList<SolutionCrawlerEvent> events, CancellationToken cancellationToken)
        {
            if (events.IsEmpty)
                return;

            var workspace = events[0].Workspace;
            Contract.ThrowIfTrue(events.Any(e => e.Workspace != workspace));

            var client = await RemoteHostClient.TryGetClientAsync(workspace, cancellationToken).ConfigureAwait(false);
            await ProcessWorkspaceChangeEventsAsync(client, events, cancellationToken).ConfigureAwait(false);
        }

        private async Task ProcessWorkspaceChangeEventsAsync(
            RemoteHostClient? client,
            ImmutableSegmentedList<SolutionCrawlerEvent> events,
            CancellationToken cancellationToken)
        {
            foreach (var ev in events)
                await ProcessWorkspaceChangeEventAsync(client, ev, cancellationToken).ConfigureAwait(false);
        }

        private async ValueTask ProcessWorkspaceChangeEventAsync(
            RemoteHostClient? client,
            SolutionCrawlerEvent ev,
            CancellationToken cancellationToken)
        {
            if (ev.DocumentOpenArgs != null)
            {
                var openArgs = ev.DocumentOpenArgs;
                await EnqueueFullDocumentEventAsync(client, openArgs.Document.Project.Solution, openArgs.Document.Id, InvocationReasons.DocumentOpened, cancellationToken).ConfigureAwait(false);
            }
            else if (ev.DocumentCloseArgs != null)
            {
                var closeArgs = ev.DocumentCloseArgs;
                await EnqueueFullDocumentEventAsync(client, closeArgs.Document.Project.Solution, closeArgs.Document.Id, InvocationReasons.DocumentClosed, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                var args = ev.WorkspaceChangeArgs;
                Contract.ThrowIfNull(args);
                switch (args.Kind)
                {
                    case WorkspaceChangeKind.SolutionAdded:
                        await EnqueueFullSolutionEventAsync(client, args.NewSolution, InvocationReasons.DocumentAdded, cancellationToken).ConfigureAwait(false);
                        break;

                    case WorkspaceChangeKind.SolutionCleared:
                    case WorkspaceChangeKind.SolutionRemoved:
                        await EnqueueFullSolutionEventAsync(client, args.OldSolution, InvocationReasons.SolutionRemoved, cancellationToken).ConfigureAwait(false);
                        break;

                    case WorkspaceChangeKind.SolutionChanged:
                    case WorkspaceChangeKind.SolutionReloaded:
                        await EnqueueSolutionChangedEventAsync(client, args.OldSolution, args.NewSolution, cancellationToken).ConfigureAwait(false);
                        break;

                    case WorkspaceChangeKind.ProjectAdded:
                        Contract.ThrowIfNull(args.ProjectId);
                        await EnqueueFullProjectEventAsync(client, args.NewSolution, args.ProjectId, InvocationReasons.DocumentAdded, cancellationToken).ConfigureAwait(false);
                        break;

                    case WorkspaceChangeKind.ProjectRemoved:
                        Contract.ThrowIfNull(args.ProjectId);
                        await EnqueueFullProjectEventAsync(client, args.OldSolution, args.ProjectId, InvocationReasons.DocumentRemoved, cancellationToken).ConfigureAwait(false);
                        break;

                    case WorkspaceChangeKind.ProjectChanged:
                    case WorkspaceChangeKind.ProjectReloaded:
                        Contract.ThrowIfNull(args.ProjectId);
                        await EnqueueProjectChangedEventAsync(client, args.OldSolution, args.NewSolution, args.ProjectId, cancellationToken).ConfigureAwait(false);
                        break;

                    case WorkspaceChangeKind.DocumentAdded:
                        Contract.ThrowIfNull(args.DocumentId);
                        await EnqueueFullDocumentEventAsync(client, args.NewSolution, args.DocumentId, InvocationReasons.DocumentAdded, cancellationToken).ConfigureAwait(false);
                        break;

                    case WorkspaceChangeKind.DocumentRemoved:
                        Contract.ThrowIfNull(args.DocumentId);
                        await EnqueueFullDocumentEventAsync(client, args.OldSolution, args.DocumentId, InvocationReasons.DocumentRemoved, cancellationToken).ConfigureAwait(false);
                        break;

                    case WorkspaceChangeKind.DocumentChanged:
                    case WorkspaceChangeKind.DocumentReloaded:
                        Contract.ThrowIfNull(args.DocumentId);
                        await EnqueueDocumentChangedEventAsync(client, args.OldSolution, args.NewSolution, args.DocumentId, cancellationToken).ConfigureAwait(false);
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
                        await EnqueueFullProjectEventAsync(client, args.NewSolution, args.ProjectId, InvocationReasons.AdditionalDocumentChanged, cancellationToken).ConfigureAwait(false);
                        break;

                }
            }
        }

        private async ValueTask EnqueueFullSolutionEventAsync(
            RemoteHostClient? client,
            Solution solution,
            InvocationReasons reasons,
            CancellationToken cancellationToken)
        {
            if (client == null)
            {
                await _aggregationService.OnSolutionEventAsync(solution, reasons, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await client.TryInvokeAsync<IRemoteSolutionCrawlerEventsAggregationService>(
                    solution,
                    (service, solutionChecksum, cancellationToken) => service.OnSolutionEventAsync(solutionChecksum, reasons, cancellationToken),
                    cancellationToken).ConfigureAwait(false);
            }
        }

        private async ValueTask EnqueueFullProjectEventAsync(
            RemoteHostClient? client,
            Solution solution,
            ProjectId projectId,
            InvocationReasons reasons,
            CancellationToken cancellationToken)
        {
            if (client == null)
            {
                await _aggregationService.OnProjectEventAsync(solution, projectId, reasons, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await client.TryInvokeAsync<IRemoteSolutionCrawlerEventsAggregationService>(
                    solution,
                    (service, solutionChecksum, cancellationToken) => service.OnProjectEventAsync(solutionChecksum, projectId, reasons, cancellationToken),
                    cancellationToken).ConfigureAwait(false);
            }
        }

        private async ValueTask EnqueueFullDocumentEventAsync(
            RemoteHostClient? client,
            Solution solution,
            DocumentId documentId,
            InvocationReasons reasons,
            CancellationToken cancellationToken)
        {
            if (client == null)
            {
                await _aggregationService.OnDocumentEventAsync(solution, documentId, reasons, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await client.TryInvokeAsync<IRemoteSolutionCrawlerEventsAggregationService>(
                    solution,
                    (service, solutionChecksum, cancellationToken) => service.OnDocumentEventAsync(solutionChecksum, documentId, reasons, cancellationToken),
                    cancellationToken).ConfigureAwait(false);
            }
        }

        private async ValueTask EnqueueSolutionChangedEventAsync(
            RemoteHostClient? client,
            Solution oldSolution,
            Solution newSolution,
            CancellationToken cancellationToken)
        {
            if (client == null)
            {
                await _aggregationService.OnSolutionChangedAsync(oldSolution, newSolution, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await client.TryInvokeAsync<IRemoteSolutionCrawlerEventsAggregationService>(
                    oldSolution, newSolution,
                    (service, oldSolutionChecksum, newSolutionChecksum, cancellationToken) =>
                        service.OnSolutionChangedAsync(oldSolutionChecksum, newSolutionChecksum, cancellationToken),
                    cancellationToken).ConfigureAwait(false);
            }
        }

        private async ValueTask EnqueueProjectChangedEventAsync(
            RemoteHostClient? client,
            Solution oldSolution,
            Solution newSolution,
            ProjectId projectId,
            CancellationToken cancellationToken)
        {
            if (client == null)
            {
                await _aggregationService.OnProjectChangedAsync(oldSolution, newSolution, projectId, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await client.TryInvokeAsync<IRemoteSolutionCrawlerEventsAggregationService>(
                    oldSolution, newSolution,
                    (service, oldSolutionChecksum, newSolutionChecksum, cancellationToken) =>
                        service.OnProjectChangedAsync(oldSolutionChecksum, newSolutionChecksum, projectId, cancellationToken),
                    cancellationToken).ConfigureAwait(false);
            }
        }

        private async ValueTask EnqueueDocumentChangedEventAsync(
            RemoteHostClient? client,
            Solution oldSolution,
            Solution newSolution,
            DocumentId documentId,
            CancellationToken cancellationToken)
        {
            if (client == null)
            {
                await _aggregationService.OnDocumentChangedAsync(oldSolution, newSolution, documentId, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await client.TryInvokeAsync<IRemoteSolutionCrawlerEventsAggregationService>(
                    oldSolution, newSolution,
                    (service, oldSolutionChecksum, newSolutionChecksum, cancellationToken) =>
                        service.OnDocumentChangedAsync(oldSolutionChecksum, newSolutionChecksum, documentId, cancellationToken),
                    cancellationToken).ConfigureAwait(false);
            }
        }

        //private async Task EnqueueProjectConfigurationChangeWorkItemAsync(
        //    RemoteHostClient client, ProjectChanges projectChanges)
        //{
        //    var oldProject = projectChanges.OldProject;
        //    var newProject = projectChanges.NewProject;

        //    // TODO: why solution changes return Project not ProjectId but ProjectChanges return DocumentId not Document?
        //    var projectConfigurationChange = InvocationReasons.Empty;

        //    if (!object.Equals(oldProject.ParseOptions, newProject.ParseOptions))
        //    {
        //        projectConfigurationChange = projectConfigurationChange.With(InvocationReasons.ProjectParseOptionChanged);
        //    }

        //    if (projectChanges.GetAddedMetadataReferences().Any() ||
        //        projectChanges.GetAddedProjectReferences().Any() ||
        //        projectChanges.GetAddedAnalyzerReferences().Any() ||
        //        projectChanges.GetRemovedMetadataReferences().Any() ||
        //        projectChanges.GetRemovedProjectReferences().Any() ||
        //        projectChanges.GetRemovedAnalyzerReferences().Any() ||
        //        !object.Equals(oldProject.CompilationOptions, newProject.CompilationOptions) ||
        //        !object.Equals(oldProject.AssemblyName, newProject.AssemblyName) ||
        //        !object.Equals(oldProject.Name, newProject.Name) ||
        //        !object.Equals(oldProject.AnalyzerOptions, newProject.AnalyzerOptions) ||
        //        !object.Equals(oldProject.DefaultNamespace, newProject.DefaultNamespace) ||
        //        !object.Equals(oldProject.OutputFilePath, newProject.OutputFilePath) ||
        //        !object.Equals(oldProject.OutputRefFilePath, newProject.OutputRefFilePath) ||
        //        !oldProject.CompilationOutputInfo.Equals(newProject.CompilationOutputInfo) ||
        //        oldProject.State.RunAnalyzers != newProject.State.RunAnalyzers)
        //    {
        //        projectConfigurationChange = projectConfigurationChange.With(InvocationReasons.ProjectConfigurationChanged);
        //    }

        //    if (!projectConfigurationChange.IsEmpty)
        //    {
        //        await EnqueueFullProjectEventAsync(client, projectChanges.NewProject, projectConfigurationChange).ConfigureAwait(false);
        //    }
        //}
    }
}
