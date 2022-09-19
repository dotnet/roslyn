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
        private readonly AsyncBatchingWorkQueue<SolutionCrawlerEvent> _eventQueue;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public HostWorkspaceEventListener(
            IGlobalOptionService globalOptions,
            IThreadingContext threadingContext,
            IAsynchronousOperationListenerProvider listenerProvider)
        {
            _globalOptions = globalOptions;
            _threadingContext = threadingContext;

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

        private void OnWorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
            => _eventQueue.AddWork(new SolutionCrawlerEvent(e, null, null));

        private void OnDocumentOpened(object sender, TextDocumentEventArgs e)
            => _eventQueue.AddWork(new SolutionCrawlerEvent(null, e, null));

        private void OnDocumentClosed(object sender, TextDocumentEventArgs e)
            => _eventQueue.AddWork(new SolutionCrawlerEvent(null, null, e));

        private async ValueTask ProcessWorkspaceChangeEventsAsync(ImmutableSegmentedList<SolutionCrawlerEvent> events, CancellationToken cancellationToken)
        {
            if (events.IsEmpty)
                return;

            var workspace = events[0].Workspace;
            Contract.ThrowIfTrue(events.Any(e => e.Workspace != workspace));

            var client = await RemoteHostClient.TryGetClientAsync(workspace, cancellationToken).ConfigureAwait(false);
            if (client != null)
            {
                await ProcessWorkspaceChangeEventsAsync(client, events, cancellationToken).ConfigureAwait(false);
                return;
            }
        }

        private async Task ProcessWorkspaceChangeEventsAsync(
            RemoteHostClient client,
            ImmutableSegmentedList<SolutionCrawlerEvent> events,
            CancellationToken cancellationToken)
        {
            foreach (var ev in events)
            {
                await ProcessWorkspaceChangeEventAsync(client, ev, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task ProcessWorkspaceChangeEventAsync(
            RemoteHostClient client,
            SolutionCrawlerEvent ev,
            CancellationToken cancellationToken)
        {
            if (ev.DocumentOpenArgs != null)
            {
                var openArgs = ev.DocumentOpenArgs;
                await client.TryInvokeAsync<IRemoteSolutionCrawlerEventsService>(
                    openArgs.Document.Project.Solution,
                    (service, solutionChecksum, cancellationToken) => service.OnDocumentOpenedAsync(solutionChecksum, openArgs.Document.Id, cancellationToken),
                    cancellationToken).ConfigureAwait(false);
            }
            else if (ev.DocumentCloseArgs != null)
            {
                var closeArgs = ev.DocumentOpenArgs;
                await client.TryInvokeAsync<IRemoteSolutionCrawlerEventsService>(
                    closeArgs.Document.Project.Solution,
                    (service, solutionChecksum, cancellationToken) => service.OnDocumentClosedAsync(solutionChecksum, closeArgs.Document.Id, cancellationToken),
                    cancellationToken).ConfigureAwait(false);
            }
            else
            {
                var args = ev.WorkspaceChangeArgs;
                Contract.ThrowIfNull(args);
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
                        EnqueueFullSolutionEvent(args.OldSolution, InvocationReasons.SolutionRemoved, eventName);
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

                }
            }
        }
    }
}
