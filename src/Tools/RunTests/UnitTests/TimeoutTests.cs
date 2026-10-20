// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace RunTests.UnitTests;

public sealed class TimeoutTests
{
    internal static string DotnetPath => Path.Combine(
        Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(typeof(object).Assembly.Location))))!,
        OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet");

    internal static ProcessStartInfo FixtureStartInfo(string mode, string readyPath)
    {
        var info = new ProcessStartInfo(DotnetPath) { UseShellExecute = false };
        info.ArgumentList.Add("exec");
        info.ArgumentList.Add("--runtimeconfig");
        var runtimeConfig = Path.ChangeExtension(typeof(TimeoutTests).Assembly.Location, ".runtimeconfig.json");
        info.ArgumentList.Add(runtimeConfig);
        info.ArgumentList.Add(typeof(ProcessFixture.Program).Assembly.Location);
        info.ArgumentList.Add(mode);
        info.ArgumentList.Add(readyPath);
        info.ArgumentList.Add(runtimeConfig);
        return info;
    }

    internal static async Task<int> WaitForChildAsync(string path)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (DateTime.UtcNow < deadline)
        {
            if (File.Exists(path) && int.TryParse(await File.ReadAllTextAsync(path), out var pid))
                return pid;
            await Task.Delay(20);
        }

        throw new TimeoutException($"Fixture did not write child identity to {path}.");
    }

    [ConditionalFact(typeof(WindowsOnly))]
    public async Task TimeoutDiscoversChildAfterLauncherExit()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".pid");
        var info = FixtureStartInfo("orphan", path);
        info.RedirectStandardOutput = true;
        info.RedirectStandardError = true;
        var launcher = ProcessRunner.CreateProcess(info);
        Process? child = null;
        try
        {
            child = Process.GetProcessById(await WaitForChildAsync(path));
            var deadline = DateTime.UtcNow.AddSeconds(30);
            while (!launcher.Process.HasExited && DateTime.UtcNow < deadline)
                await Task.Delay(20);

            Assert.True(launcher.Process.HasExited);
            Assert.False(launcher.Result.IsCompleted);
            var descendants = await ProcessUtil.GetChildProcessesAsync(launcher.Process);
            try
            {
                Assert.Contains(descendants, process => process.Id == child.Id);
            }
            finally
            {
                foreach (var process in descendants)
                    process.Dispose();
            }
        }
        finally
        {
            if (child is not null)
            {
                ProcessUtil.KillTree(child);
                child.Dispose();
            }
            ProcessUtil.KillTree(launcher.Process);
            launcher.StopReadingOutput();
            await launcher.Result.WaitAsync(TimeSpan.FromSeconds(30));
            launcher.Process.Dispose();
            File.Delete(path);
        }
    }

    [Theory]
    [InlineData("orphan")]
    [InlineData("parent")]
    public async Task TimeoutOwnsAndCleansSurvivingChild(string mode)
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".pid");
        using var unrelated = Process.Start(FixtureStartInfo("leaf", path + ".unrelated"))!;
        using var owned = await OwnedProcess.StartAsync(FixtureStartInfo(mode, path), DotnetPath, "ownership regression");
        Process? child = null;
        try
        {
            child = Process.GetProcessById(await WaitForChildAsync(path));
            await WaitForChildAsync(path + ".unrelated");
            var processes = await owned.GetProcessesAsync();
            try
            {
                Assert.Contains(processes, process => process.Process.Id == child.Id);
                Assert.DoesNotContain(processes, process => process.Process.Id == unrelated.Id);
            }
            finally
            {
                foreach (var process in processes)
                    process.Process.Dispose();
            }
            await owned.CleanupAsync();
            Assert.True(child.HasExited);
            Assert.True(owned.Info.Result.IsCompleted);
            Assert.False(unrelated.HasExited);
        }
        finally
        {
            await owned.CleanupAsync();
            if (child is not null)
            {
                ProcessUtil.KillTree(child);
                child.Dispose();
            }
            ProcessUtil.KillTree(unrelated);
            owned.Info.Process.Dispose();
            File.Delete(path);
            File.Delete(path + ".unrelated");
        }
    }

    [ConditionalFact(typeof(WindowsOnly))]
    public async Task ClosingOwnershipJobTerminatesSurvivingChild()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".pid");
        using var owned = await OwnedProcess.StartAsync(FixtureStartInfo("orphan", path), DotnetPath, "job closure regression");
        Process? child = null;
        try
        {
            child = Process.GetProcessById(await WaitForChildAsync(path));
            await owned.Info.Process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(30));
            Assert.False(child.HasExited);
            owned.Dispose();
            await child.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
            Assert.True(child.HasExited);
        }
        finally
        {
            if (child is not null)
            {
                ProcessUtil.KillTree(child);
                child.Dispose();
            }
            ProcessUtil.KillTree(owned.Info.Process);
            owned.Info.StopReadingOutput();
            await owned.Info.Result.WaitAsync(TimeSpan.FromSeconds(30));
            owned.Info.Process.Dispose();
            File.Delete(path);
        }
    }

    [Fact]
    public async Task TargetStartupHookRunsInsideOwnershipScope()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".pid");
        var info = FixtureStartInfo("exit", path);
        info.Environment["DOTNET_STARTUP_HOOKS"] = typeof(ProcessFixture.Program).Assembly.Location;
        info.Environment["RUNTESTS_FIXTURE_STARTUP_PID"] = path;
        info.Environment["RUNTESTS_FIXTURE_RUNTIME_CONFIG"] = Path.ChangeExtension(typeof(TimeoutTests).Assembly.Location, ".runtimeconfig.json");
        OwnedProcess? owned = null;
        Process? child = null;
        try
        {
            owned = await OwnedProcess.StartAsync(info, DotnetPath, "startup hook regression");
            child = Process.GetProcessById(await WaitForChildAsync(path));
            await owned.Info.Process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(30));
            var processes = await owned.GetProcessesAsync();
            try
            {
                Assert.Contains(processes, process => process.Process.Id == child.Id);
            }
            finally
            {
                foreach (var process in processes)
                    process.Process.Dispose();
            }
            await owned.CleanupAsync();
            Assert.True(child.HasExited);
        }
        finally
        {
            if (child is null && File.Exists(path))
                child = Process.GetProcessById(await WaitForChildAsync(path));
            if (child is not null)
            {
                ProcessUtil.KillTree(child);
                child.Dispose();
            }
            if (owned is not null)
            {
                await owned.DisposeAsync();
                owned.Info.Process.Dispose();
            }
            File.Delete(path);
        }
    }

    [Theory]
    [InlineData("dump")]
    [InlineData("exit")]
    [InlineData("fail")]
    [InlineData("leaf")]
    public async Task DumpHelperOutcomeIsBoundedAndValidated(string mode)
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".partial");
        var dumpPath = Path.ChangeExtension(path, ".dmp");
        try
        {
            var collection = DumpCollector.CollectWithHelperAsync(FixtureStartInfo(mode, path), DotnetPath,
                path, dumpPath, "collector regression", TimeSpan.FromSeconds(2), CancellationToken.None);
            if (mode == "dump")
            {
                await collection;
                Assert.True(File.Exists(dumpPath));
                Assert.False(File.Exists(path));
            }
            else if (mode == "leaf")
            {
                using var helper = Process.GetProcessById(await WaitForChildAsync(path));
                await Assert.ThrowsAsync<TimeoutException>(() => collection);
                Assert.True(helper.HasExited);
                Assert.False(File.Exists(dumpPath));
            }
            else
            {
                await Assert.ThrowsAsync<InvalidOperationException>(() => collection);
                Assert.False(File.Exists(dumpPath));
            }
        }
        finally
        {
            File.Delete(path);
            File.Delete(dumpPath);
        }
    }

    [Fact]
    public async Task DumpAggregateCancellationStillCleansHelper()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".pid");
        using var budget = new CancellationTokenSource();
        try
        {
            var collection = DumpCollector.CollectWithHelperAsync(FixtureStartInfo("leaf", path), DotnetPath,
                path + ".partial", path + ".dmp", "budget regression", TimeSpan.FromSeconds(30), budget.Token);
            using var helper = Process.GetProcessById(await WaitForChildAsync(path));
            budget.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => collection);
            Assert.True(helper.HasExited);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task DumpUsesCapturedIdentityAndPersistsMismatchFailure()
    {
        using var temp = new TempRoot();
        var directory = temp.CreateDirectory().Path;
        var path = Path.Combine(directory, "child.pid");
        using var owned = await OwnedProcess.StartAsync(FixtureStartInfo("leaf", path), DotnetPath, "identity regression");
        List<OwnedProcess.Target> processes = [];
        var logPath = Path.Combine(directory, "runtests.log");
        try
        {
            var childId = await WaitForChildAsync(path);
            processes = await owned.GetProcessesAsync();
            var target = Assert.Single(processes, process => process.Process.Id == childId);
            Logger.Initialize(logPath);
            await DumpCollector.CollectAsync([target with { StartIdentity = target.StartIdentity + 1 }],
                DotnetPath, directory, "identity regression", CancellationToken.None);
            Assert.Empty(Directory.EnumerateFiles(directory, "*.dmp", SearchOption.AllDirectories));
            using var stream = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);
            var log = reader.ReadToEnd();
            Assert.Contains($"Process {childId} no longer matches the requested dump target.", log);
            Assert.Contains($"dump failed: PID={childId}", log);
            Assert.DoesNotContain($"Dump backend discovery started for PID={childId}", log);
        }
        finally
        {
            try
            {
                await owned.CleanupAsync();
            }
            finally
            {
                foreach (var process in processes)
                    process.Process.Dispose();
                owned.Info.Process.Dispose();
                Logger.Close();
            }
        }
    }

    [Fact]
    public async Task StoppedOutputReadersDoNotWaitForSurvivingChild()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".pid");
        var info = FixtureStartInfo("orphan", path);
        info.RedirectStandardOutput = true;
        info.RedirectStandardError = true;
        var launcher = ProcessRunner.CreateProcess(info);
        Process? child = null;
        try
        {
            child = Process.GetProcessById(await WaitForChildAsync(path));
            await launcher.Process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(30));
            Assert.False(launcher.Result.IsCompleted);
            launcher.StopReadingOutput();
            await launcher.Result.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.False(child.HasExited);
        }
        finally
        {
            if (child is not null)
            {
                ProcessUtil.KillTree(child);
                child.Dispose();
            }
            ProcessUtil.KillTree(launcher.Process);
            launcher.StopReadingOutput();
            await launcher.Result.WaitAsync(TimeSpan.FromSeconds(30));
            launcher.Process.Dispose();
            File.Delete(path);
        }
    }

    [Fact]
    public void StoppingReadersToleratesCompletionDisposalRace()
    {
        using var process = new Process();
        using var cancellation = new CancellationTokenSource();
        var completion = new TaskCompletionSource<ProcessResult>();
        var info = new ProcessInfo(process, new ProcessStartInfo(), completion.Task, cancellation);
        cancellation.Dispose();
        info.StopReadingOutput();
        completion.SetResult(default);
    }

    [Fact]
    public async Task ConcurrentHelpersShareAggregateCancellation()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".pid");
        var otherPath = path + ".other";
        using var budget = new CancellationTokenSource();
        var first = DumpCollector.CollectWithHelperAsync(FixtureStartInfo("leaf", path), DotnetPath,
            path + ".partial", path + ".dmp", "first concurrent helper", TimeSpan.FromSeconds(30), budget.Token);
        var second = DumpCollector.CollectWithHelperAsync(FixtureStartInfo("leaf", otherPath), DotnetPath,
            otherPath + ".partial", otherPath + ".dmp", "second concurrent helper", TimeSpan.FromSeconds(30), budget.Token);
        try
        {
            using var firstProcess = Process.GetProcessById(await WaitForChildAsync(path));
            using var secondProcess = Process.GetProcessById(await WaitForChildAsync(otherPath));
            budget.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => second);
            Assert.True(firstProcess.HasExited);
            Assert.True(secondProcess.HasExited);
        }
        finally
        {
            budget.Cancel();
            try
            {
                await Task.WhenAll(first, second);
            }
            catch (OperationCanceledException) when (budget.IsCancellationRequested)
            {
            }
            File.Delete(path);
            File.Delete(otherPath);
        }
    }

    [Fact]
    public void TimeoutLogIsPersistedBeforeRunCompletion()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".log");
        try
        {
            Logger.Initialize(path);
            ConsoleUtil.Warning("[persistence regression] dump failed: target PID=123; stage=backend; code=7.");
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);
            Assert.Contains("[persistence regression] dump failed: target PID=123; stage=backend; code=7.", reader.ReadToEnd());
        }
        finally
        {
            Logger.Close();
            File.Delete(path);
        }
    }
}
