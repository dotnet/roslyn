// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SourceGeneration;
using Microsoft.CodeAnalysis.SourceGeneratorTelemetry;
using Microsoft.CodeAnalysis.Threading;

namespace Microsoft.VisualStudio.LanguageServices;

/// <summary>
/// Exports a <see cref="ISourceGeneratorTelemetryCollectorWorkspaceService"/> which is watched across all workspaces. This lets us collect
/// statistics for all workspaces (including things like interactive, preview, etc.) so we can get the overall counts to report.
/// </summary>
[Export]
[ExportWorkspaceServiceFactory(typeof(ISourceGeneratorTelemetryCollectorWorkspaceService)), Shared]
internal sealed class VisualStudioSourceGeneratorTelemetryCollectorWorkspaceServiceFactory : IWorkspaceServiceFactory, ISourceGeneratorTelemetryReporterWorkspaceService
{
    /// <summary>
    /// The collector that's used to collect all the telemetry for operations within <see
    /// cref="VisualStudioWorkspace"/>. We'll report this when the solution is closed, so the telemetry is linked to
    /// that.
    /// </summary>
    private readonly SourceGeneratorTelemetryCollectorWorkspaceService _visualStudioWorkspaceInstance = new();

    /// <summary>
    /// The collector used to collect telemetry for any other workspaces that might be created; we'll report this at
    /// the end of the session since nothing here is necessarily linked to a specific solution. The expectation is
    /// this may be empty for many/most sessions, but we don't want a hole in our reporting and discover that the
    /// hard way.
    /// </summary>
    private readonly SourceGeneratorTelemetryCollectorWorkspaceService _otherWorkspacesInstance = new();

    private readonly AsyncBatchingWorkQueue _workQueue;
    private readonly IThreadingContext _threadingContext;

    private readonly Lazy<VisualStudioWorkspace> _visualStudioWorkspace;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public VisualStudioSourceGeneratorTelemetryCollectorWorkspaceServiceFactory(IThreadingContext threadingContext, IAsynchronousOperationListenerProvider listenerProvider, Lazy<VisualStudioWorkspace> visualStudioWorkspace)
    {
        _threadingContext = threadingContext;
        _visualStudioWorkspace = visualStudioWorkspace;

        // We will report telemetry every five minutes, if we have any to report
        _workQueue = new AsyncBatchingWorkQueue(
            TimeSpan.FromMinutes(5),
            SendSourceGeneratorTelemetryAsync,
            listenerProvider.GetListener(FeatureAttribute.Telemetry),
            _threadingContext.DisposalToken);
    }

    public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
    {
        // We will record all generators for the main workspace in one bucket, and any other generators running in other
        // workspaces (interactive, for example) will be put in a different bucket.
        if (workspaceServices.Workspace is VisualStudioWorkspace)
        {
            return _visualStudioWorkspaceInstance;
        }
        else
        {
            return _otherWorkspacesInstance;
        }
    }

    public void QueueReportingOfTelemetry()
    {
        _workQueue.AddWork();
    }

    private async ValueTask SendSourceGeneratorTelemetryAsync(CancellationToken cancellationToken)
    {
        ReportData(FunctionId.SourceGenerator_SolutionInProcStatistics, _visualStudioWorkspaceInstance.FetchKeysAndAndClear());
        ReportData(FunctionId.SourceGenerator_OtherWorkspaceSessionStatistics, _otherWorkspacesInstance.FetchKeysAndAndClear());

        var client = await RemoteHostClient.TryGetClientAsync(_visualStudioWorkspace.Value, cancellationToken).ConfigureAwait(false);
        if (client is not null)
        {
            // We'll still report this telemetry in-process, which gives it the benefit of being scoped under the same telemetry context
            // that is associated with the current open solution. This would allow for better correlation of the telemetry to the size of the solutions, etc.
            var remoteTelemetryData = await client.TryInvokeAsync(static (IRemoteSourceGenerationService service, CancellationToken ct) => service.FetchAndClearTelemetryKeyValuePairsAsync(ct), cancellationToken).ConfigureAwait(false);
            if (remoteTelemetryData.HasValue)
                ReportData(FunctionId.SourceGenerator_SolutionStatistics, remoteTelemetryData.Value);
        }

        void ReportData(FunctionId functionId, ImmutableArray<ImmutableDictionary<string, object?>> data)
        {
            // Don't send empty events if we don't have any data
            if (data.Length != 0)
            {
                foreach (var map in data)
                {
                    Logger.Log(functionId, KeyValueLogMessage.Create(map =>
                    {
                        foreach (var kvp in map)
                            map[kvp.Key] = kvp.Value;
                    }));
                }
            }
        }
    }
}
