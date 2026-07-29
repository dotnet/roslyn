// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Microsoft.CodeAnalysis.LanguageServer.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace IdeCoreBenchmarks;

[MemoryDiagnoser]
[SimpleJob(RunStrategy.Monitoring, invocationCount: 1)]
public class LanguageServerDaemonBenchmarks
{
    private TempRoot _tempRoot = null!;
    private LanguageServerBenchmarkHost _testHost = null!;
    private MaterializedLspWorkspace _firstWorkspace = null!;
    private MaterializedLspWorkspace _secondWorkspace = null!;
    private Process _benchmarkProcess = null!;
    private LanguageServerBenchmarkHost.BenchmarkTestDaemon _daemon = null!;
    private LanguageServerBenchmarkHost.TestServer? _firstServer;
    private LanguageServerBenchmarkHost.TestServer? _secondServer;
    private LanguageServerBenchmarkHost.MetadataCacheStatistics? _statisticsAfterLoad;
    private ProcessMemorySnapshot? _processMemoryDelta;

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        _tempRoot = new TempRoot();
        _testHost = new LanguageServerBenchmarkHost();
        await _testHost.WarmCompositionCacheAsync();
        _firstWorkspace = MaterializedLspWorkspace.Create(
            _tempRoot,
            LspTestWorkspaces.CreateConsoleApplication("FirstConsoleApplication"),
            CancellationToken.None);
        _secondWorkspace = MaterializedLspWorkspace.Create(
            _tempRoot,
            LspTestWorkspaces.CreateConsoleApplication("SecondConsoleApplication"),
            CancellationToken.None);
        _benchmarkProcess = Process.GetCurrentProcess();
    }

    [IterationSetup(Target = nameof(LoadTwoConsoleApplicationsWithoutSharedMetadataCache))]
    public void IterationSetupWithoutSharedMetadataCache()
        => IterationSetup(useSharedMetadataCache: false);

    [IterationSetup(Target = nameof(LoadTwoConsoleApplicationsWithSharedMetadataCache))]
    public void IterationSetupWithSharedMetadataCache()
        => IterationSetup(useSharedMetadataCache: true);

    private void IterationSetup(bool useSharedMetadataCache)
        => _daemon = _testHost.CreateDaemonAsync(useSharedMetadataCache).GetAwaiter().GetResult();

    [Benchmark(Baseline = true)]
    public Task LoadTwoConsoleApplicationsWithoutSharedMetadataCache()
        => LoadTwoConsoleApplications();

    [Benchmark]
    public Task LoadTwoConsoleApplicationsWithSharedMetadataCache()
        => LoadTwoConsoleApplications();

    private async Task LoadTwoConsoleApplications()
    {
        _firstServer = await _daemon.CreateClientAsync();
        _secondServer = await _daemon.CreateClientAsync();

        var memoryBeforeLoad = ProcessMemorySnapshot.Capture(_benchmarkProcess);
        await Task.WhenAll(
            _firstServer.OpenProjectsAsync(
                ImmutableArray.Create(_firstWorkspace.GetFullPath(_firstWorkspace.Content.LoadPath!)),
                CancellationToken.None),
            _secondServer.OpenProjectsAsync(
                ImmutableArray.Create(_secondWorkspace.GetFullPath(_secondWorkspace.Content.LoadPath!)),
                CancellationToken.None));
        _processMemoryDelta = ProcessMemorySnapshot.Capture(_benchmarkProcess).Subtract(memoryBeforeLoad);
        _statisticsAfterLoad = _daemon.GetSharedMetadataCacheStatistics();
    }

    [IterationCleanup]
    public void IterationCleanup()
    {
        try
        {
            WriteProcessMemoryDelta();
            WriteMetadataCacheStatistics();
            DisposeIterationAsync().GetAwaiter().GetResult();
        }
        finally
        {
            _processMemoryDelta = null;
            _statisticsAfterLoad = null;
        }
    }

    private void WriteProcessMemoryDelta()
    {
        if (_processMemoryDelta is not { } delta)
            return;

        Console.WriteLine($"Process memory load delta: privateBytes={delta.PrivateBytes}, workingSetBytes={delta.WorkingSetBytes}");
    }

    private void WriteMetadataCacheStatistics()
    {
        if (_statisticsAfterLoad is not { } statistics)
            return;

        Console.WriteLine($"Shared metadata cache after two projects: {Format(statistics)}");

        static string Format(LanguageServerBenchmarkHost.MetadataCacheStatistics statistics)
            => $"requests={statistics.RequestCount}, hits={statistics.HitCount}, misses={statistics.MissCount}, " +
               $"loads={statistics.MetadataLoadCount}, failedLoads={statistics.FailedLoadCount}, " +
               $"duplicateLoads={statistics.DuplicateLoadCount}, " +
               $"nonCacheable={statistics.NonCacheableLoadCount}, changedDuringLoad={statistics.ChangedDuringLoadCount}, " +
               $"deadEntryRemovals={statistics.DeadEntryRemovalCount}, entries={statistics.EntryCount}";
    }

    private async Task DisposeIterationAsync()
    {
        var firstServer = _firstServer;
        var secondServer = _secondServer;
        _firstServer = null;
        _secondServer = null;

        try
        {
            if (firstServer is not null && secondServer is not null)
                await Task.WhenAll(firstServer.DisposeAsync().AsTask(), secondServer.DisposeAsync().AsTask());
            else if (firstServer is not null)
                await firstServer.DisposeAsync();
            else if (secondServer is not null)
                await secondServer.DisposeAsync();
        }
        finally
        {
            await _daemon.DisposeAsync();
        }
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _benchmarkProcess.Dispose();
        _testHost.Dispose();
        _tempRoot.Dispose();
    }

    private readonly record struct ProcessMemorySnapshot(long PrivateBytes, long WorkingSetBytes)
    {
        internal static ProcessMemorySnapshot Capture(Process process)
        {
            process.Refresh();
            return new(process.PrivateMemorySize64, process.WorkingSet64);
        }

        internal ProcessMemorySnapshot Subtract(ProcessMemorySnapshot earlier)
            => new(PrivateBytes - earlier.PrivateBytes, WorkingSetBytes - earlier.WorkingSetBytes);
    }
}
