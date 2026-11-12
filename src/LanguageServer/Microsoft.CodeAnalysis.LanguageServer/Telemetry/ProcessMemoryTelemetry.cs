// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Telemetry;

namespace Microsoft.CodeAnalysis.LanguageServer.Telemetry;

/// <summary>
/// Periodically samples the memory used by this process and logs it on the process-level telemetry session.
/// </summary>
/// <remarks>
/// Memory belongs to the process, not to an LSP client: in daemon mode every connected server shares one runtime, GC
/// heap, and set of loaded assemblies. Each sample therefore includes the number of clients active when it was taken
/// rather than being attributed to a single client session, so that standalone and daemon processes can be compared
/// by memory per active client.
/// </remarks>
internal sealed class ProcessMemoryTelemetry : IDisposable
{
    public static readonly TimeSpan DefaultSampleInterval = TimeSpan.FromMinutes(5);

    internal const string WorkingSetMBPropertyName = "WorkingSetMB";
    internal const string GCCommittedMBPropertyName = "GCCommittedMB";
    internal const string ActiveClientsPropertyName = "ActiveClients";
    internal const string GCModePropertyName = "GCMode";

    private const long BytesPerMegabyte = 1024 * 1024;

    private readonly RoslynTelemetry _telemetry;
    private readonly CancellationTokenSource _cancellationSource = new();

    public ProcessMemoryTelemetry(RoslynTelemetry telemetry, Func<int> getActiveClients, TimeSpan sampleInterval)
    {
        _telemetry = telemetry;

        // Working set is the process total, including native memory such as prefetched metadata images. It also counts
        // pages shared with other processes (such as mapped runtime images), so it overstates the combined cost of
        // several standalone processes by roughly that shared amount per process.
        _ = PeriodicTelemetryLoop.RunAsync(
            sampleInterval,
            () => LogSample(Environment.WorkingSet, GC.GetGCMemoryInfo().TotalCommittedBytes, getActiveClients()),
            _cancellationSource.Token);
    }

    public void Dispose()
    {
        _cancellationSource.Cancel();
        _cancellationSource.Dispose();
    }

    internal void LogSample(long workingSetBytes, long gcCommittedBytes, int activeClients)
    {
        _telemetry.Log(FunctionId.VSCode_LanguageServer_Process_Memory, KeyValueLogMessage.Create(LogType.Trace, m =>
        {
            m[WorkingSetMBPropertyName] = workingSetBytes / BytesPerMegabyte;
            m[GCCommittedMBPropertyName] = gcCommittedBytes / BytesPerMegabyte;
            m[ActiveClientsPropertyName] = activeClients;
            m[GCModePropertyName] = GCSettings.IsServerGC ? "Server" : "Workstation";
        }));
    }
}