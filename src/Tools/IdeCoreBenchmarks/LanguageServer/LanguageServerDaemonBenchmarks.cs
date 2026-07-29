// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using Microsoft.CodeAnalysis.LanguageServer.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace IdeCoreBenchmarks;

[MemoryDiagnoser]
[Config(typeof(BenchmarkConfig))]
public class LanguageServerDaemonBenchmarks
{
    private TempRoot _tempRoot = null!;
    private LanguageServerBenchmarkHost _testHost = null!;
    private MaterializedLspWorkspace _firstWorkspace = null!;
    private MaterializedLspWorkspace _secondWorkspace = null!;
    private LanguageServerBenchmarkHost.BenchmarkTestDaemon _daemon = null!;
    private LanguageServerBenchmarkHost.TestServer? _firstServer;
    private LanguageServerBenchmarkHost.TestServer? _secondServer;

    private sealed class BenchmarkConfig : ManualConfig
    {
        public BenchmarkConfig()
        {
            AddJob(Job.Default
                .WithStrategy(RunStrategy.Monitoring)
                .WithInvocationCount(1)
                .WithToolchain(new InProcessEmitToolchain(TimeSpan.FromMinutes(30), logOutput: true)));
        }
    }

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

        await Task.WhenAll(
            _firstServer.OpenProjectsAsync(
                ImmutableArray.Create(_firstWorkspace.GetFullPath(_firstWorkspace.Content.LoadPath!)),
                CancellationToken.None),
            _secondServer.OpenProjectsAsync(
                ImmutableArray.Create(_secondWorkspace.GetFullPath(_secondWorkspace.Content.LoadPath!)),
                CancellationToken.None));
    }

    [IterationCleanup]
    public void IterationCleanup()
        => DisposeIterationAsync().GetAwaiter().GetResult();

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
        _testHost.Dispose();
        _tempRoot.Dispose();
    }
}
