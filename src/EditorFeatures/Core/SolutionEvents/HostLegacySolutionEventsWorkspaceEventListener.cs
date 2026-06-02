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
using Microsoft.CodeAnalysis.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LegacySolutionEvents;

/// <summary>
/// Event listener that hears about workspaces and exists solely to let unit testing continue to work using their own
/// fork of solution crawler.  Importantly, this is always active until the point that we can get unit testing to move
/// to an entirely differently (ideally 'pull') model for test discovery.
/// </summary>
[ExportEventListener(WellKnownEventListeners.Workspace, WorkspaceKind.Host), Shared]
internal sealed partial class HostLegacySolutionEventsWorkspaceEventListener : IEventListener
{
    private readonly IGlobalOptionService _globalOptions;
    private readonly IThreadingContext _threadingContext;
    private readonly AsyncBatchingWorkQueue<WorkspaceChangeEventArgs> _eventQueue;

    private WorkspaceEventRegistration? _workspaceChangedDisposer;

    private bool? _processSourceGeneratedDocuments;

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
            listenerProvider.GetListener(FeatureAttribute.SolutionCrawlerUnitTesting),
            _threadingContext.DisposalToken);
    }

    public void StartListening(Workspace workspace)
    {
        // We only support this option to disable crawling in internal speedometer and ddrit perf runs to lower noise.
        // It is not exposed to the user.
        if (_globalOptions.GetOption(SolutionCrawlerRegistrationService.EnableSolutionCrawler))
        {
            // Fetch whether we're processing source-generated files or not. We latch whatever we first read, to avoid any cases where changing the option might cause
            // inconsistent analysis.
            _processSourceGeneratedDocuments ??= _globalOptions.GetOption(SolutionCrawlerRegistrationService.ProcessRoslynSourceGeneratedFiles);
            _workspaceChangedDisposer = workspace.RegisterWorkspaceChangedHandler(OnWorkspaceChanged);
        }
    }

    public void StopListening(Workspace workspace)
    {
        if (_globalOptions.GetOption(SolutionCrawlerRegistrationService.EnableSolutionCrawler))
        {
            _workspaceChangedDisposer?.Dispose();
            _workspaceChangedDisposer = null;
        }
    }

    private void OnWorkspaceChanged(WorkspaceChangeEventArgs e)
    {
        // Legacy workspace events exist solely to let unit testing continue to work using their own fork of solution
        // crawler.  As such, they only need events for the project types they care about.  Specifically, that is only
        // for VB and C#.  This is relevant as well as we don't sync any other project types to OOP.  So sending 
        // notifications about other projects that don't even exist on the other side isn't helpful.

        var projectId = e.ProjectId ?? e.DocumentId?.ProjectId;
        if (projectId != null)
        {
            var project = e.OldSolution.GetProject(projectId) ?? e.NewSolution.GetProject(projectId);
            if (project != null && !RemoteSupportedLanguages.IsSupported(project.Language))
                return;
        }

        _eventQueue.AddWork(e);
    }

    private async ValueTask ProcessWorkspaceChangeEventsAsync(ImmutableSegmentedList<WorkspaceChangeEventArgs> events, CancellationToken cancellationToken)
    {
        if (events.IsEmpty)
            return;

        var workspace = events[0].OldSolution.Workspace;
        Contract.ThrowIfTrue(events.Any(e => e.OldSolution.Workspace != workspace || e.NewSolution.Workspace != workspace));

        // We should have initialized this before subscribing to the workspace changed events
        Contract.ThrowIfFalse(_processSourceGeneratedDocuments.HasValue);

        var client = await RemoteHostClient.TryGetClientAsync(workspace, cancellationToken).ConfigureAwait(false);

        if (client is null)
        {
            var aggregationService = workspace.Services.GetRequiredService<ILegacySolutionEventsAggregationService>();
            var shouldReport = aggregationService.ShouldReportChanges(workspace.Services.SolutionServices);
            if (!shouldReport)
                return;

            foreach (var args in events)
                await aggregationService.OnWorkspaceChangedAsync(args, _processSourceGeneratedDocuments.Value, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            // Notifying OOP of workspace events can be expensive (there may be a lot of them, and they involve
            // syncing over entire solution snapshots).  As such, do not bother to do this if the remote side says
            // that it's not interested in the events.  This will happen, for example, when the unittesting
            // Test-Explorer window has not been shown yet, and so the unit testing system will not have registered
            // an incremental analyzer with us.
            var shouldReport = await client.TryInvokeAsync<IRemoteLegacySolutionEventsAggregationService, bool>(
                (service, cancellationToken) => service.ShouldReportChangesAsync(cancellationToken),
                cancellationToken).ConfigureAwait(false);
            if (!shouldReport.HasValue || !shouldReport.Value)
                return;

            foreach (var args in events)
            {
                await client.TryInvokeAsync<IRemoteLegacySolutionEventsAggregationService>(
                    args.OldSolution, args.NewSolution,
                    (service, oldSolutionChecksum, newSolutionChecksum, cancellationToken) =>
                        service.OnWorkspaceChangedAsync(oldSolutionChecksum, newSolutionChecksum, args.Kind, args.ProjectId, args.DocumentId, _processSourceGeneratedDocuments.Value, cancellationToken),
                    cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
