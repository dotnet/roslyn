// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
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
    private LanguageServerBenchmarkHost.BenchmarkTestDaemon? _daemon;
    private LanguageServerBenchmarkHost.TestServer? _firstServer;
    private LanguageServerBenchmarkHost.TestServer? _secondServer;

    [Params(false, true)]
    public bool UseDaemon { get; set; }

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

        if (UseDaemon)
            _daemon = await _testHost.CreateDaemonAsync();
    }

    [Benchmark]
    public async Task LoadTwoConsoleApplications()
    {
        if (UseDaemon)
        {
            _firstServer = await _daemon!.CreateClientAsync();
            _secondServer = await _daemon.CreateClientAsync();
        }
        else
        {
            _firstServer = await _testHost.CreateSingleServerAsync();
            _secondServer = await _testHost.CreateSingleServerAsync();
        }

        await Task.WhenAll(
            _firstServer.OpenProjectsAsync(
                ImmutableArray.Create(_firstWorkspace.GetFullPath(_firstWorkspace.Content.LoadPath!)),
                CancellationToken.None),
            _secondServer.OpenProjectsAsync(
                ImmutableArray.Create(_secondWorkspace.GetFullPath(_secondWorkspace.Content.LoadPath!)),
                CancellationToken.None));
    }

    [IterationCleanup]
    public async Task IterationCleanup()
    {
        var firstServer = _firstServer;
        var secondServer = _secondServer;
        _firstServer = null;
        _secondServer = null;

        if (firstServer is not null && secondServer is not null)
            await Task.WhenAll(firstServer.DisposeAsync().AsTask(), secondServer.DisposeAsync().AsTask());
        else if (firstServer is not null)
            await firstServer.DisposeAsync();
        else if (secondServer is not null)
            await secondServer.DisposeAsync();
    }

    [GlobalCleanup]
    public async Task GlobalCleanup()
    {
        if (_daemon is not null)
            await _daemon.DisposeAsync();

        _testHost.Dispose();
        _tempRoot.Dispose();
    }
}
