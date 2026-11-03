// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NET

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace IdeCoreBenchmarks.Lsp;

[MemoryDiagnoser]
public class LspProjectLoadingBenchmarks
{
    private LanguageServerBenchmarkHost _testHost = null!;
    private LanguageServerBenchmarkHost.TestServer _server = null!;
    private string _solutionPath = null!;

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        var roslynRoot = Program.RoslynRootPath;
        _solutionPath = Path.GetFullPath(Path.Combine(roslynRoot, "Roslyn.slnx"));
        if (!File.Exists(_solutionPath))
            throw new FileNotFoundException("Couldn't find Roslyn.slnx.", _solutionPath);

        _testHost = new LanguageServerBenchmarkHost();
        await _testHost.WarmCompositionCacheAsync();
    }

    [IterationSetup]
    public void IterationSetup()
        => _server = _testHost.CreateServerAsync().GetAwaiter().GetResult();

    [Benchmark]
    public Task LoadRoslynSolution()
        => _server.OpenSolutionAsync(_solutionPath, CancellationToken.None);

    [IterationCleanup]
    public void IterationCleanup()
        => _server.DisposeAsync().AsTask().GetAwaiter().GetResult();

    [GlobalCleanup]
    public void GlobalCleanup()
        => _testHost.Dispose();
}

#endif
