// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Globalization;
using System.Runtime;
using System.Runtime.InteropServices;
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

    internal const string PrivateMBPropertyName = "PrivateMB";
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

        _ = PeriodicTelemetryLoop.RunAsync(
            sampleInterval,
            () => LogSample(ProcessMemorySnapshot.Capture(), getActiveClients()),
            _cancellationSource.Token);
    }

    public void Dispose()
    {
        _cancellationSource.Cancel();
        _cancellationSource.Dispose();
    }

    internal void LogSample(ProcessMemorySnapshot snapshot, int activeClients)
    {
        _telemetry.Log(FunctionId.VSCode_LanguageServer_Process_Memory, KeyValueLogMessage.Create(LogType.Trace, m =>
        {
            if (snapshot.PrivateBytes is { } privateBytes)
                m[PrivateMBPropertyName] = privateBytes / BytesPerMegabyte;

            m[WorkingSetMBPropertyName] = snapshot.WorkingSetBytes / BytesPerMegabyte;
            m[GCCommittedMBPropertyName] = snapshot.GCCommittedBytes / BytesPerMegabyte;
            m[ActiveClientsPropertyName] = activeClients;
            m[GCModePropertyName] = GCSettings.IsServerGC ? "Server" : "Workstation";
        }));
    }
}

/// <summary>
/// A point-in-time view of the memory used by the current process.
/// </summary>
/// <param name="PrivateBytes">
/// Memory that is private to this process, including native allocations but excluding file-backed pages (such as
/// mapped assemblies) that can be shared with other processes. This is the measure that shows whether processes that
/// share a daemon use less memory than separate processes, so it is collected on every platform the server supports.
/// <see langword="null"/> when the platform does not provide a reliable measure.
/// </param>
/// <param name="WorkingSetBytes">Resident memory, including pages shared with other processes.</param>
/// <param name="GCCommittedBytes">Memory committed by the GC as of the last collection.</param>
internal readonly record struct ProcessMemorySnapshot(long? PrivateBytes, long WorkingSetBytes, long GCCommittedBytes)
{
    public static ProcessMemorySnapshot Capture()
    {
        using var process = Process.GetCurrentProcess();

        long? privateBytes;
        if (OperatingSystem.IsWindows())
        {
            privateBytes = process.PrivateMemorySize64;
        }
        else if (OperatingSystem.IsLinux())
        {
            // PrivateMemorySize64 is VmData on Linux, which includes reserved but untouched address space (such as GC
            // reservations), so it is not a useful measure of memory use.
            privateBytes = TryGetLinuxPrivateBytes();
        }
        else if (OperatingSystem.IsMacOS())
        {
            // PrivateMemorySize64 is always 0 on macOS in .NET 10 (later runtimes report the physical footprint).
            privateBytes = TryGetMacOSPhysicalFootprint();
        }
        else
        {
            privateBytes = null;
        }

        return new(privateBytes, process.WorkingSet64, GC.GetGCMemoryInfo().TotalCommittedBytes);
    }

    /// <summary>
    /// Returns resident anonymous memory plus swapped out memory, which approximates the private memory of the process.
    /// </summary>
    private static long? TryGetLinuxPrivateBytes()
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

        // RssAnon requires Linux 4.5 or later.
        return rssAnonKilobytes is { } rssAnon ? (rssAnon + swapKilobytes) * 1024 : null;
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
        => proc_pid_rusage(Environment.ProcessId, RUSAGE_INFO_V0, out var usage) == 0
            ? (long)usage.PhysFootprint
            : null;

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
