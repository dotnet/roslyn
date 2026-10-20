// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
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
        using var owned = OwnedProcess.Start(FixtureStartInfo(mode, path), DotnetPath, "ownership regression");
        Process? child = null;
        try
        {
            child = Process.GetProcessById(await WaitForChildAsync(path));
            await WaitForChildAsync(path + ".unrelated");
            var processes = await owned.GetProcessesAsync();
            try
            {
                Assert.Contains(processes, process => process.Id == child.Id);
                Assert.DoesNotContain(processes, process => process.Id == unrelated.Id);
            }
            finally
            {
                foreach (var process in processes)
                    process.Dispose();
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
