// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Globalization;
using System.Runtime;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.LanguageServer.Telemetry;

/// <summary>
/// Periodically samples the memory used by this process and records it on the process-level telemetry session.
/// </summary>
/// <remarks>
/// Memory belongs to the process, not to an LSP client: in daemon mode every connected server shares one runtime, GC
/// heap, and set of loaded assemblies. Samples are therefore tagged with the number of clients active at the time
/// they were taken rather than attributed to a single client session, so that standalone and daemon processes can be
/// compared by memory per active client.
/// </remarks>
internal sealed class ProcessMemoryTelemetry : IDisposable
{
    public static readonly TimeSpan DefaultSampleInterval = TimeSpan.FromMinutes(5);

    // Values are recorded in megabytes because the VS telemetry histogram buckets top out at 10000.
    internal const string PrivateMBMetricName = "privateMB";
    internal const string WorkingSetMBMetricName = "workingSetMB";
    internal const string GCCommittedMBMetricName = "gcCommittedMB";
    internal const string GCHeapMBMetricName = "gcHeapMB";
    internal const string ActiveClientsMetricName = "activeClients";

    internal const string ActiveClientsTagName = "activeClients";
    internal const string GCModeTagName = "gcMode";

    /// <summary>
    /// Active client counts at or above this value share one tag value to keep tag cardinality bounded.
    /// </summary>
    private const int MaxActiveClientsBucket = 5;
    private const long BytesPerMegabyte = 1024 * 1024;

    private readonly RoslynTelemetry _telemetry;
    private readonly Func<int> _getActiveClients;
    private readonly CancellationTokenSource _cancellationSource = new();

    private long _sampledPeakPrivateBytes;
    private long _sampledPeakWorkingSetBytes;

    public ProcessMemoryTelemetry(RoslynTelemetry telemetry, Func<int> getActiveClients, TimeSpan sampleInterval)
    {
        _telemetry = telemetry;
        _getActiveClients = getActiveClients;

        _ = SampleLoopAsync(sampleInterval, _cancellationSource.Token);
    }

    public static string GCMode => GCSettings.IsServerGC ? "Server" : "Workstation";

    public void Dispose()
    {
        _cancellationSource.Cancel();
        _cancellationSource.Dispose();
    }

    /// <summary>
    /// Returns the peak memory usage of the process in megabytes, combining OS-tracked peaks where available with the
    /// peaks observed by sampling.
    /// </summary>
    public (long? PeakPrivateMB, long PeakWorkingSetMB) GetPeaks()
    {
        var snapshot = ProcessMemorySnapshot.Capture();
        UpdateSampledPeaks(snapshot);

        long? peakPrivateBytes = snapshot.PrivateBytes is null
            ? null
            : Math.Max(Interlocked.Read(ref _sampledPeakPrivateBytes), snapshot.PeakPrivateBytes ?? 0);
        var peakWorkingSetBytes = Math.Max(Interlocked.Read(ref _sampledPeakWorkingSetBytes), snapshot.PeakWorkingSetBytes);

        return (peakPrivateBytes is { } bytes ? ToMegabytes(bytes) : null, ToMegabytes(peakWorkingSetBytes));
    }

    private async Task SampleLoopAsync(TimeSpan sampleInterval, CancellationToken cancellationToken)
    {
        while (true)
        {
            try
            {
                await Task.Delay(sampleInterval, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            try
            {
                RecordSample(ProcessMemorySnapshot.Capture(), _getActiveClients());
            }
            catch (Exception e) when (FatalError.ReportAndCatch(e))
            {
                // Keep sampling: one failed sample must not stop every later one.
            }
        }
    }

    internal void RecordSample(ProcessMemorySnapshot snapshot, int activeClients)
    {
        UpdateSampledPeaks(snapshot);

        const FunctionId functionId = FunctionId.VSCode_LanguageServer_Process_Memory;
        KeyValuePair<string, object?> activeClientsTag = new(ActiveClientsTagName, GetActiveClientsBucket(activeClients));
        KeyValuePair<string, object?> gcModeTag = new(GCModeTagName, GCMode);

        if (snapshot.PrivateBytes is { } privateBytes)
            _telemetry.Record(functionId, PrivateMBMetricName, ToMegabytes(privateBytes), activeClientsTag, gcModeTag);

        _telemetry.Record(functionId, WorkingSetMBMetricName, ToMegabytes(snapshot.WorkingSetBytes), activeClientsTag, gcModeTag);
        _telemetry.Record(functionId, GCCommittedMBMetricName, ToMegabytes(snapshot.GCCommittedBytes), activeClientsTag, gcModeTag);
        _telemetry.Record(functionId, GCHeapMBMetricName, ToMegabytes(snapshot.GCHeapBytes), activeClientsTag, gcModeTag);

        // Recorded exactly (unlike the bucketed tag) so that summing it over samples yields client time.
        _telemetry.Record(functionId, ActiveClientsMetricName, activeClients, activeClientsTag, gcModeTag);
    }

    internal static string GetActiveClientsBucket(int activeClients)
        => activeClients >= MaxActiveClientsBucket
            ? MaxActiveClientsBucket.ToString(CultureInfo.InvariantCulture) + "+"
            : activeClients.ToString(CultureInfo.InvariantCulture);

    private void UpdateSampledPeaks(ProcessMemorySnapshot snapshot)
    {
        if (snapshot.PrivateBytes is { } privateBytes)
            InterlockedMax(ref _sampledPeakPrivateBytes, privateBytes);

        InterlockedMax(ref _sampledPeakWorkingSetBytes, snapshot.WorkingSetBytes);

        static void InterlockedMax(ref long location, long value)
        {
            var current = Interlocked.Read(ref location);
            while (value > current)
            {
                var previous = Interlocked.CompareExchange(ref location, value, current);
                if (previous == current)
                    return;

                current = previous;
            }
        }
    }

    private static long ToMegabytes(long bytes)
        => bytes / BytesPerMegabyte;
}

/// <summary>
/// A point-in-time view of the memory used by the current process.
/// </summary>
/// <param name="PrivateBytes">
/// Memory that is private to this process, including native allocations but excluding file-backed pages (such as
/// mapped assemblies) that can be shared with other processes. <see langword="null"/> when the platform does not
/// provide a reliable measure.
/// </param>
/// <param name="PeakPrivateBytes">The OS-tracked peak of <paramref name="PrivateBytes"/>, when the OS provides one.</param>
/// <param name="WorkingSetBytes">Resident memory, including shared pages.</param>
/// <param name="PeakWorkingSetBytes">The OS-tracked peak of <paramref name="WorkingSetBytes"/>.</param>
/// <param name="GCCommittedBytes">Memory committed by the GC as of the last collection.</param>
/// <param name="GCHeapBytes">The size of the managed heap as of the last collection.</param>
internal readonly record struct ProcessMemorySnapshot(
    long? PrivateBytes,
    long? PeakPrivateBytes,
    long WorkingSetBytes,
    long PeakWorkingSetBytes,
    long GCCommittedBytes,
    long GCHeapBytes)
{
    public static ProcessMemorySnapshot Capture()
    {
        using var process = Process.GetCurrentProcess();
        var gcMemoryInfo = GC.GetGCMemoryInfo();

        long? privateBytes;
        long? peakPrivateBytes = null;
        if (OperatingSystem.IsWindows())
        {
            // Private commit. PagedMemorySize64 maps to the pagefile-backed (private) commit charge, whose peak the OS tracks.
            privateBytes = process.PrivateMemorySize64;
            peakPrivateBytes = process.PeakPagedMemorySize64;
        }
        else if (OperatingSystem.IsLinux())
        {
            // PrivateMemorySize64 is VmData on Linux, which includes reserved but untouched address space (such as GC
            // reservations), so it is not a useful measure of memory use.
            privateBytes = TryGetLinuxPrivateBytes();
        }
        else if (OperatingSystem.IsMacOS())
        {
            privateBytes = TryGetMacOSPhysicalFootprint();
        }
        else
        {
            privateBytes = null;
        }

        return new(
            privateBytes,
            peakPrivateBytes,
            process.WorkingSet64,
            process.PeakWorkingSet64,
            gcMemoryInfo.TotalCommittedBytes,
            gcMemoryInfo.HeapSizeBytes);
    }

    /// <summary>
    /// Returns resident anonymous memory plus swapped out memory, which approximates the private memory of the process.
    /// </summary>
    private static long? TryGetLinuxPrivateBytes()
    {
        try
        {
            long? rssAnonKilobytes = null;
            long swapKilobytes = 0;
            foreach (var line in File.ReadLines("/proc/self/status"))
            {
                if (TryParseProcStatusKilobytes(line, "RssAnon:", out var kilobytes))
                    rssAnonKilobytes = kilobytes;
                else if (TryParseProcStatusKilobytes(line, "VmSwap:", out kilobytes))
                    swapKilobytes = kilobytes;
            }

            return rssAnonKilobytes is { } rssAnon ? (rssAnon + swapKilobytes) * 1024 : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>
    /// Parses a <c>/proc/[pid]/status</c> line of the form <c>Field:    1234 kB</c>.
    /// </summary>
    internal static bool TryParseProcStatusKilobytes(string line, string fieldName, out long kilobytes)
    {
        kilobytes = 0;
        if (!line.StartsWith(fieldName, StringComparison.Ordinal))
            return false;

        var value = line.AsSpan(fieldName.Length).Trim();
        if (value.EndsWith("kB", StringComparison.Ordinal))
            value = value[..^2].TrimEnd();

        return long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out kilobytes);
    }

    /// <summary>
    /// Returns the physical footprint of the process, the measure macOS itself reports as a process's memory use (for
    /// example in Activity Monitor). It includes compressed and swapped memory and excludes shared file-backed pages.
    /// </summary>
    private static long? TryGetMacOSPhysicalFootprint()
    {
        try
        {
            return proc_pid_rusage(Environment.ProcessId, RUSAGE_INFO_V0, out var usage) == 0
                ? (long)usage.PhysFootprint
                : null;
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
        {
            return null;
        }
    }

    private const int RUSAGE_INFO_V0 = 0;

    /// <summary>
    /// <c>struct rusage_info_v0</c> from <c>&lt;sys/resource.h&gt;</c>.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct RUsageInfoV0
    {
        public ulong Uuid0;
        public ulong Uuid1;
        public ulong UserTime;
        public ulong SystemTime;
        public ulong PkgIdleWakeups;
        public ulong InterruptWakeups;
        public ulong PageIns;
        public ulong WiredSize;
        public ulong ResidentSize;
        public ulong PhysFootprint;
        public ulong ProcStartAbsTime;
        public ulong ProcExitAbsTime;
    }

    [DllImport("libc")]
    private static extern int proc_pid_rusage(int pid, int flavor, out RUsageInfoV0 buffer);
}
