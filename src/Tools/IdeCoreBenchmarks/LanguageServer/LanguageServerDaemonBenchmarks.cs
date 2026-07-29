// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace IdeCoreBenchmarks;

[MemoryDiagnoser]
[Config(typeof(BenchmarkConfig))]
public class LanguageServerDaemonBenchmarks
{
    private const string MSBuildDisableNodeReuseEnvironmentVariable = "MSBUILDDISABLENODEREUSE";

    private TempRoot _tempRoot = null!;
    private LanguageServerBenchmarkHost _testHost = null!;
    private string _firstSolutionPath = null!;
    private string _secondSolutionPath = null!;
    private LanguageServerBenchmarkHost.BenchmarkTestDaemon _daemon = null!;
    private LanguageServerBenchmarkHost.TestServer? _firstServer;
    private LanguageServerBenchmarkHost.TestServer? _secondServer;
    private string? _originalMSBuildDisableNodeReuse;

    private sealed class BenchmarkConfig : ManualConfig
    {
        public BenchmarkConfig()
        {
            AddJob(Job.Default
                .WithStrategy(RunStrategy.Monitoring)
                .WithInvocationCount(1)
                .WithIterationCount(3)
                .WithWarmupCount(0)
                .WithLaunchCount(1)
                .WithToolchain(new InProcessEmitToolchain(TimeSpan.FromHours(1), logOutput: true)));
        }
    }

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        _tempRoot = new TempRoot();
        _testHost = new LanguageServerBenchmarkHost();
        await _testHost.WarmCompositionCacheAsync();

        var sourceRoot = Environment.GetEnvironmentVariable(Program.RoslynRootPathEnvVariableName);
        if (string.IsNullOrEmpty(sourceRoot))
            throw new InvalidOperationException($"{Program.RoslynRootPathEnvVariableName} is not set.");

        var commit = (await RunProcessAsync("git", new[] { "rev-parse", "HEAD" }, sourceRoot)).Trim();
        _firstSolutionPath = await CreateRestoredCheckoutAsync(sourceRoot, commit);
        _secondSolutionPath = await CreateRestoredCheckoutAsync(sourceRoot, commit);
    }

    [IterationSetup(Target = nameof(LoadTwoRoslynSolutionsWithoutSharedMetadataCache))]
    public void IterationSetupWithoutSharedMetadataCache()
        => IterationSetup(useSharedMetadataCache: false);

    [IterationSetup(Target = nameof(LoadTwoRoslynSolutionsWithSharedMetadataCache))]
    public void IterationSetupWithSharedMetadataCache()
        => IterationSetup(useSharedMetadataCache: true);

    private void IterationSetup(bool useSharedMetadataCache)
    {
        _originalMSBuildDisableNodeReuse = Environment.GetEnvironmentVariable(MSBuildDisableNodeReuseEnvironmentVariable);
        Environment.SetEnvironmentVariable(MSBuildDisableNodeReuseEnvironmentVariable, "1");

        try
        {
            _daemon = _testHost.CreateDaemonAsync(useSharedMetadataCache).GetAwaiter().GetResult();
        }
        catch
        {
            RestoreMSBuildDisableNodeReuseEnvironmentVariable();
            throw;
        }
    }

    [Benchmark(Baseline = true)]
    public Task LoadTwoRoslynSolutionsWithoutSharedMetadataCache()
        => LoadTwoRoslynSolutions();

    [Benchmark]
    public Task LoadTwoRoslynSolutionsWithSharedMetadataCache()
        => LoadTwoRoslynSolutions();

    private async Task LoadTwoRoslynSolutions()
    {
        _firstServer = await _daemon.CreateClientAsync();
        _secondServer = await _daemon.CreateClientAsync();

        await Task.WhenAll(
            _firstServer.OpenSolutionAsync(_firstSolutionPath, CancellationToken.None),
            _secondServer.OpenSolutionAsync(_secondSolutionPath, CancellationToken.None));
    }

    [IterationCleanup]
    public void IterationCleanup()
    {
        try
        {
            DisposeIterationAsync().GetAwaiter().GetResult();
        }
        finally
        {
            RestoreMSBuildDisableNodeReuseEnvironmentVariable();
        }
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
        _testHost.Dispose();
        _tempRoot.Dispose();
    }

    private async Task<string> CreateRestoredCheckoutAsync(string sourceRoot, string commit)
    {
        var checkoutPath = _tempRoot.CreateDirectory().Path;
        await RunProcessAsync(
            "git",
            new[] { "clone", "--shared", "--no-checkout", "--no-tags", sourceRoot, checkoutPath },
            sourceRoot);
        await RunProcessAsync(
            "git",
            new[] { "-c", "core.longpaths=true", "-C", checkoutPath, "checkout", "--detach", commit },
            sourceRoot);

        var solutionPath = Path.Combine(checkoutPath, "Roslyn.slnx");
        await RunProcessAsync(
            "dotnet",
            new[]
            {
                "restore",
                solutionPath,
                "/p:UseSharedCompilation=false",
                "/p:BuildInParallel=false",
                "/m:1",
                "/p:Deterministic=true",
                "/p:Optimize=true",
                "/nodeReuse:false",
            },
            checkoutPath);
        return solutionPath;
    }

    private static async Task<string> RunProcessAsync(string fileName, string[] arguments, string workingDirectory)
    {
        var startInfo = new ProcessStartInfo(fileName)
        {
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        foreach (var argument in arguments)
            startInfo.ArgumentList.Add(argument);

        startInfo.Environment["DOTNET_CLI_UI_LANGUAGE"] = "en-US";
        startInfo.Environment[MSBuildDisableNodeReuseEnvironmentVariable] = "1";

        using var process = new Process { StartInfo = startInfo };
        if (!process.Start())
            throw new InvalidOperationException($"Failed to start '{fileName}'.");

        var standardOutputTask = process.StandardOutput.ReadToEndAsync();
        var standardErrorTask = process.StandardError.ReadToEndAsync();
        using var processTimeoutSource = new CancellationTokenSource(TimeSpan.FromMinutes(30));
        try
        {
            await process.WaitForExitAsync(processTimeoutSource.Token);
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);

            throw new TimeoutException($"'{fileName} {string.Join(' ', arguments)}' timed out.");
        }

        string[] processOutput;
        try
        {
            processOutput = await Task.WhenAll(standardOutputTask, standardErrorTask).WaitAsync(TimeSpan.FromSeconds(30));
        }
        catch (TimeoutException)
        {
            throw new TimeoutException($"Timed out draining output from '{fileName} {string.Join(' ', arguments)}'.");
        }

        var standardOutput = processOutput[0];
        var standardError = processOutput[1];

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"""
                '{fileName} {string.Join(' ', arguments)}' exited with code {process.ExitCode}.

                Standard output:
                {standardOutput}

                Standard error:
                {standardError}
                """);
        }

        return standardOutput;
    }

    private void RestoreMSBuildDisableNodeReuseEnvironmentVariable()
    {
        Environment.SetEnvironmentVariable(MSBuildDisableNodeReuseEnvironmentVariable, _originalMSBuildDisableNodeReuse);
        _originalMSBuildDisableNodeReuse = null;
    }
}
