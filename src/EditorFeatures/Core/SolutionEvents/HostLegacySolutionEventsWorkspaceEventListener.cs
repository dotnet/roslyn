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
        private readonly AsyncBatchingWorkQueue<WorkspaceChangeEventArgs> _eventQueue;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public HostLegacySolutionEventsWorkspaceEventListener(
            IGlobalOptionService globalOptions,
            IThreadingContext threadingContext,
            IAsynchronousOperationListenerProvider listenerProvider)
        {
            _globalOptions = globalOptions;
            _threadingContext = threadingContext;
            _eventQueue = new AsyncBatchingWorkQueue<WorkspaceChangeEventArgs>(
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
                _threadingContext.DisposalToken.Register(() =>
                {
                    workspace.WorkspaceChanged -= OnWorkspaceChanged;
                });
            }
        }

        private void OnWorkspaceChanged(object? sender, WorkspaceChangeEventArgs e)
            => _eventQueue.AddWork(e);

        private async ValueTask ProcessWorkspaceChangeEventsAsync(ImmutableSegmentedList<WorkspaceChangeEventArgs> eventArgs, CancellationToken cancellationToken)
        {
            if (events.IsEmpty)
                return;

            var workspace = events[0].OldSolution.Workspace;
            Contract.ThrowIfTrue(events.Any(e => e.OldSolution.Workspace != workspace || e.NewSolution.Workspace != workspace));

            var client = await RemoteHostClient.TryGetClientAsync(workspace, cancellationToken).ConfigureAwait(false);

            if (client is null)
            {
                var aggregationService = workspace.Services.GetRequiredService<ILegacySolutionEventsAggregationService>();

                foreach (var args in events)
                    await aggregationService.OnWorkspaceChangedAsync(ev, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                foreach (var args in events)
                {
                    await client.TryInvokeAsync<IRemoteLegacySolutionEventsAggregationService>(
                        args.OldSolution, args.NewSolution,
                        (service, oldSolutionChecksum, newSolutionChecksum, cancellationToken) =>
                            service.OnWorkspaceChangedAsync(oldSolutionChecksum, newSolutionChecksum, args.Kind, args.ProjectId, args.DocumentId, cancellationToken),
                        cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }
}
