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

    [IterationSetup]
    public void IterationSetup()
        => _daemon = _testHost.CreateDaemonAsync().GetAwaiter().GetResult();

    [Benchmark]
    public async Task LoadTwoConsoleApplications()
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
        _processMemoryDelta = ProcessMemorySnapshot.Capture(_benchmarkProcess) - memoryBeforeLoad;
    }

    [IterationCleanup]
    public void IterationCleanup()
    {
        try
        {
            WriteProcessMemoryDelta();
            DisposeIterationAsync().GetAwaiter().GetResult();
        }
        finally
        {
            _processMemoryDelta = null;
        }
    }

    private void WriteProcessMemoryDelta()
    {
        if (_processMemoryDelta is not { } delta)
            return;

        Console.WriteLine($"Process memory load delta: privateBytes={delta.PrivateBytes}, workingSetBytes={delta.WorkingSetBytes}");
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

        public static ProcessMemorySnapshot operator -(ProcessMemorySnapshot later, ProcessMemorySnapshot earlier)
            => new(later.PrivateBytes - earlier.PrivateBytes, later.WorkingSetBytes - earlier.WorkingSetBytes);
    }
}
