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
using Microsoft.CodeAnalysis.SolutionCrawler;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LegacySolutionEvents
{
    [ExportEventListener(WellKnownEventListeners.Workspace, WorkspaceKind.Host), Shared]
    internal sealed partial class HostLegacySolutionEventsWorkspaceEventListener : IEventListener<object>
    {
        private readonly IGlobalOptionService _globalOptions;
        private readonly IThreadingContext _threadingContext;
        private readonly AsyncBatchingWorkQueue<LegacySolutionEvent> _eventQueue;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public HostLegacySolutionEventsWorkspaceEventListener(
            IGlobalOptionService globalOptions,
            IThreadingContext threadingContext,
            IAsynchronousOperationListenerProvider listenerProvider)
        {
            _globalOptions = globalOptions;
            _threadingContext = threadingContext;
            _eventQueue = new AsyncBatchingWorkQueue<LegacySolutionEvent>(
                DelayTimeSpan.Short,
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
            => _eventQueue.AddWork(new LegacySolutionEvent(e, null, null));

        private void OnDocumentOpened(object? sender, TextDocumentEventArgs e)
            => _eventQueue.AddWork(new LegacySolutionEvent(null, e, null));

        private void OnDocumentClosed(object? sender, TextDocumentEventArgs e)
            => _eventQueue.AddWork(new LegacySolutionEvent(null, null, e));

        private async ValueTask ProcessWorkspaceChangeEventsAsync(ImmutableSegmentedList<LegacySolutionEvent> events, CancellationToken cancellationToken)
        {
            if (events.IsEmpty)
                return;

            var workspace = events[0].Workspace;
            Contract.ThrowIfTrue(events.Any(e => e.Workspace != workspace));

            var client = await RemoteHostClient.TryGetClientAsync(workspace, cancellationToken).ConfigureAwait(false);

            if (client is null)
            {
                var aggregationService = workspace.Services.GetRequiredService<ILegacySolutionEventsAggregationService>();

                foreach (var ev in events)
                    await ProcessEventAsync(aggregationService, ev, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                foreach (var ev in events)
                    await ProcessEventAsync(client, ev, cancellationToken).ConfigureAwait(false);
            }
        }

        private static async ValueTask ProcessEventAsync(ILegacySolutionEventsAggregationService aggregationService, LegacySolutionEvent ev, CancellationToken cancellationToken)
        {
            var descriptor = new HostLegacyWorkspaceDescriptor(ev.Workspace);

            if (ev.DocumentOpenArgs != null)
            {
                await aggregationService.OnTextDocumentOpenedAsync(descriptor, ev.DocumentOpenArgs, cancellationToken).ConfigureAwait(false);
            }
            else if (ev.DocumentCloseArgs != null)
            {
                await aggregationService.OnTextDocumentOpenedAsync(descriptor, ev.DocumentCloseArgs, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                Contract.ThrowIfNull(ev.WorkspaceChangeArgs);
                await aggregationService.OnWorkspaceChangedEventAsync(descriptor, ev.WorkspaceChangeArgs, cancellationToken).ConfigureAwait(false);
            }
        }

        private static async ValueTask ProcessEventAsync(RemoteHostClient client, LegacySolutionEvent ev, CancellationToken cancellationToken)
        {
            if (ev.DocumentOpenArgs != null)
            {
                var document = ev.DocumentOpenArgs.Document;
                await client.TryInvokeAsync<IRemoteLegacySolutionEventsAggregationService>(
                    document.Project.Solution,
                    (service, solutionChecksum, cancellationToken) => service.OnTextDocumentOpenedAsync(solutionChecksum, document.Id, cancellationToken),
                    cancellationToken).ConfigureAwait(false);
            }
            else if (ev.DocumentCloseArgs != null)
            {
                var document = ev.DocumentCloseArgs.Document;
                await client.TryInvokeAsync<IRemoteLegacySolutionEventsAggregationService>(
                    document.Project.Solution,
                    (service, solutionChecksum, cancellationToken) => service.OnTextDocumentClosedAsync(solutionChecksum, document.Id, cancellationToken),
                    cancellationToken).ConfigureAwait(false);
            }
            else
            {
                Contract.ThrowIfNull(ev.WorkspaceChangeArgs);
                var args = ev.WorkspaceChangeArgs;

                await client.TryInvokeAsync<IRemoteLegacySolutionEventsAggregationService>(
                    args.OldSolution, args.NewSolution,
                    (service, oldSolutionChecksum, newSolutionChecksum, cancellationToken) =>
                        service.OnWorkspaceChangedEventAsync(oldSolutionChecksum, newSolutionChecksum, args.Kind, args.ProjectId, args.DocumentId, cancellationToken),
                    cancellationToken).ConfigureAwait(false);
            }
        }

        private sealed class HostLegacyWorkspaceDescriptor : ILegacyWorkspaceDescriptor
        {
            private readonly Workspace _workspace;

            public HostLegacyWorkspaceDescriptor(Workspace workspace)
            {
                _workspace = workspace;
            }

            public string? WorkspaceKind => _workspace.Kind;

            public SolutionServices SolutionServices => _workspace.Services.SolutionServices;

            public Solution CurrentSolution => _workspace.CurrentSolution;
        }
    }
}
