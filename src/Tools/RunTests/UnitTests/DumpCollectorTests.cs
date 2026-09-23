// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace RunTests.UnitTests;

public sealed class DumpCollectorTests
{
    [Fact]
    public async Task TryDumpProcessAsync_SucceedsOnlyWhenCollectorExitsSuccessfullyAndOutputExists()
    {
        using var tempDirectory = new TempDirectory();
        var dumpFilePath = Path.Combine(tempDirectory.Path, "test.dmp");
        File.WriteAllText(dumpFilePath, "dump");
        var process = FakeCollectorProcess.Completed(exitCode: 0);
        var processFactory = new FakeCollectorProcessFactory(process);
        var tool = new DumpCollector.DotnetDumpTool("dotnet", tempDirectory.Path);
        var target = new DumpCollector.DumpTarget(42, "testhost");

        var result = await DumpCollector.TryDumpProcessAsync(target, dumpFilePath, tool, TimeSpan.FromSeconds(30), processFactory, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.False(result.TimedOut);
        Assert.Equal(0, result.ExitCode);
        Assert.True(result.DumpFileExists);
        Assert.False(process.KillProcessTreeCalled);
        Assert.NotNull(processFactory.StartInfo);
        Assert.Equal("dotnet", processFactory.StartInfo.FileName);
        Assert.Equal(tempDirectory.Path, processFactory.StartInfo.WorkingDirectory);
        Assert.Equal(
            new[] { "tool", "run", "dotnet-dump", "--", "collect", "--process-id", "42", "--type", "Full", "--output", dumpFilePath },
            processFactory.StartInfo.ArgumentList);
    }

    [Fact]
    public async Task TryDumpProcessAsync_FailsWhenCollectorExitsSuccessfullyButOutputIsMissing()
    {
        using var tempDirectory = new TempDirectory();
        var dumpFilePath = Path.Combine(tempDirectory.Path, "missing.dmp");
        var processFactory = new FakeCollectorProcessFactory(FakeCollectorProcess.Completed(exitCode: 0));
        var tool = new DumpCollector.DotnetDumpTool("dotnet", tempDirectory.Path);
        var target = new DumpCollector.DumpTarget(42, "testhost");

        var result = await DumpCollector.TryDumpProcessAsync(target, dumpFilePath, tool, TimeSpan.FromSeconds(30), processFactory, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.False(result.TimedOut);
        Assert.Equal(0, result.ExitCode);
        Assert.False(result.DumpFileExists);
    }

    [Fact]
    public async Task TryDumpProcessAsync_FailsWhenCollectorExitsNonZero()
    {
        using var tempDirectory = new TempDirectory();
        var dumpFilePath = Path.Combine(tempDirectory.Path, "test.dmp");
        File.WriteAllText(dumpFilePath, "partial dump");
        var processFactory = new FakeCollectorProcessFactory(FakeCollectorProcess.Completed(exitCode: 123));
        var tool = new DumpCollector.DotnetDumpTool("dotnet", tempDirectory.Path);
        var target = new DumpCollector.DumpTarget(42, "testhost");

        var result = await DumpCollector.TryDumpProcessAsync(target, dumpFilePath, tool, TimeSpan.FromSeconds(30), processFactory, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.False(result.TimedOut);
        Assert.Equal(123, result.ExitCode);
        Assert.True(result.DumpFileExists);
    }

    [Fact]
    public async Task TryDumpProcessAsync_TerminatesCollectorProcessTreeOnTimeout()
    {
        using var tempDirectory = new TempDirectory();
        var dumpFilePath = Path.Combine(tempDirectory.Path, "partial.dmp");
        File.WriteAllText(dumpFilePath, "partial dump");
        var process = FakeCollectorProcess.NeverExits();
        var processFactory = new FakeCollectorProcessFactory(process);
        var tool = new DumpCollector.DotnetDumpTool("dotnet", tempDirectory.Path);
        var target = new DumpCollector.DumpTarget(42, "testhost");

        var result = await DumpCollector.TryDumpProcessAsync(target, dumpFilePath, tool, TimeSpan.FromMilliseconds(10), processFactory, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.True(result.TimedOut);
        Assert.Null(result.ExitCode);
        Assert.True(result.DumpFileExists);
        Assert.True(process.KillProcessTreeCalled);
    }

    [Fact]
    public async Task RunCoreAsync_CancelsTestRunCleanupAfterBoundedDumpTimeout()
    {
        using var tempDirectory = new TempDirectory();
        var options = new Options(
            dotnetFilePath: "dotnet",
            artifactsDirectory: tempDirectory.Path,
            configuration: "Debug",
            testResultsDirectory: tempDirectory.Path,
            logFilesDirectory: tempDirectory.Path,
            architecture: "x64")
        {
            Timeout = TimeSpan.FromMilliseconds(10),
        };

        var cleanupRan = false;
        var collectorProcess = FakeCollectorProcess.NeverExits();
        var processFactory = new FakeCollectorProcessFactory(collectorProcess);
        var dumpFilePath = Path.Combine(tempDirectory.Path, "partial.dmp");
        File.WriteAllText(dumpFilePath, "partial dump");

        var result = await Program.RunCoreAsync(
            options,
            CancellationToken.None,
            async (_, cancellationToken) =>
            {
                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                    return Program.ExitSuccess;
                }
                finally
                {
                    cleanupRan = true;
                }
            },
            async (_, cancellationToken) =>
            {
                var dumpResult = await DumpCollector.TryDumpProcessAsync(
                    new DumpCollector.DumpTarget(42, "testhost"),
                    dumpFilePath,
                    new DumpCollector.DotnetDumpTool("dotnet", tempDirectory.Path),
                    TimeSpan.FromMilliseconds(10),
                    processFactory,
                    cancellationToken);
                Assert.True(dumpResult.TimedOut);
            });

        Assert.Equal(Program.ExitFailure, result);
        Assert.True(collectorProcess.KillProcessTreeCalled);
        Assert.True(cleanupRan);
    }

    private sealed class FakeCollectorProcessFactory : IDumpCollectorProcessFactory
    {
        private readonly FakeCollectorProcess _process;

        internal FakeCollectorProcessFactory(FakeCollectorProcess process)
        {
            _process = process;
        }

        internal ProcessStartInfo? StartInfo { get; private set; }

        public IDumpCollectorProcess Start(ProcessStartInfo startInfo)
        {
            StartInfo = startInfo;
            return _process;
        }
    }

    private sealed class FakeCollectorProcess : IDumpCollectorProcess
    {
        private readonly TaskCompletionSource<object?> _waitForExitSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        private FakeCollectorProcess(int exitCode)
        {
            ExitCode = exitCode;
        }

        public int ExitCode { get; }
        public IReadOnlyList<string> OutputLines { get; } = Array.Empty<string>();
        public IReadOnlyList<string> ErrorLines { get; } = Array.Empty<string>();
        internal bool KillProcessTreeCalled { get; private set; }

        internal static FakeCollectorProcess Completed(int exitCode)
        {
            var process = new FakeCollectorProcess(exitCode);
            process._waitForExitSource.SetResult(null);
            return process;
        }

        internal static FakeCollectorProcess NeverExits()
            => new FakeCollectorProcess(exitCode: -1);

        public Task WaitForExitAsync(CancellationToken cancellationToken)
            => _waitForExitSource.Task.WaitAsync(cancellationToken);

        public void KillProcessTree()
        {
            KillProcessTreeCalled = true;
            _waitForExitSource.TrySetResult(null);
        }
    }

    private sealed class TempDirectory : IDisposable
    {
        internal TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "RunTests.UnitTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        internal string Path { get; }

        public void Dispose()
        {
            try
            {
                Directory.Delete(Path, recursive: true);
            }
            catch
            {
            }
        }
    }
}
