// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace RunTests.UnitTests;

public sealed class ProcessRunnerTests
{
    [ConditionalFact(typeof(WindowsOnly))]
    [WorkItem("https://github.com/dotnet/roslyn/pull/85985")]
    public async Task InheritedOutputHandlesCauseDrainFailure()
    {
        using var temp = new TempRoot();
        var pidFile = temp.CreateFile();
        var name = Guid.NewGuid().ToString("N");
        using var started = new EventWaitHandle(false, EventResetMode.ManualReset, name);
        using var release = new EventWaitHandle(false, EventResetMode.ManualReset, name + "-release");
        var powershell = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),
            @"WindowsPowerShell\v1.0\powershell.exe");
        var childScript = $$"""
            $ErrorActionPreference = 'Stop'
            $ready = [Threading.EventWaitHandle]::OpenExisting('{{name}}-ready')
            $release = [Threading.EventWaitHandle]::OpenExisting('{{name}}-release')
            [Console]::Out.WriteLine('child stdout')
            [Console]::Error.WriteLine('child stderr')
            [Console]::Out.Flush()
            [Console]::Error.Flush()
            $ready.Set() | Out-Null
            $release.WaitOne(120000) | Out-Null
            """;
        var parentScript = $$"""
            $ErrorActionPreference = 'Stop'
            $ready = [Threading.EventWaitHandle]::new($false, [Threading.EventResetMode]::ManualReset, '{{name}}-ready')
            $info = [Diagnostics.ProcessStartInfo]::new()
            $info.FileName = '{{powershell.Replace("'", "''")}}'
            $info.Arguments = '{{encodeCommand(childScript)}}'
            $info.UseShellExecute = $false
            # Leave the child's streams unredirected so it inherits the parent's output handles.
            $child = [Diagnostics.Process]::Start($info)
            [IO.File]::WriteAllText('{{pidFile.Path.Replace("'", "''")}}', $child.Id.ToString())
            $started = [Threading.EventWaitHandle]::OpenExisting('{{name}}')
            $started.Set() | Out-Null
            if (-not $ready.WaitOne(30000)) { throw 'Child did not become ready' }
            [Console]::Out.WriteLine('parent stdout')
            [Console]::Error.WriteLine('parent stderr')
            exit 42
            """;
        var info = ProcessRunner.CreateProcess(powershell, encodeCommand(parentScript), captureOutput: true);
        using var parent = info.Process;
        Process? child = null;
        try
        {
            Assert.True(started.WaitOne(TimeSpan.FromSeconds(30)), "Parent did not record its child PID.");
            child = Process.GetProcessById(int.Parse(pidFile.ReadAllText(), CultureInfo.InvariantCulture));
            var exception = await Assert.ThrowsAsync<ProcessOutputDrainException>(
                async () => await info.Result.WaitAsync(TimeSpan.FromSeconds(90)));

            Assert.Same(parent, exception.Result.Process);
            Assert.True(parent.HasExited);
            Assert.Equal(42, parent.ExitCode);
            Assert.Equal(42, exception.Result.ExitCode);
            Assert.Equal(["child stdout", "parent stdout"], exception.Result.OutputLines);
            Assert.Equal(["child stderr", "parent stderr"], exception.Result.ErrorLines);
            Assert.False(child.HasExited);
        }
        finally
        {
            // Also recover the child if setup failed after recording its PID.
            if (child is null && int.TryParse(pidFile.ReadAllText(), out var childId))
            {
                try
                {
                    child = Process.GetProcessById(childId);
                }
                catch (ArgumentException)
                {
                    // The child has already exited.
                }
            }

            release.Set();
            if (!parent.HasExited)
            {
                parent.Kill(entireProcessTree: true);
                Assert.True(parent.WaitForExit(10000), "Parent did not exit during cleanup.");
            }

            if (child is not null)
            {
                using (child)
                {
                    if (!child.HasExited)
                        child.Kill();
                    Assert.True(child.WaitForExit(10000), "Child did not exit during cleanup.");
                }
            }
        }

        static string encodeCommand(string script)
            => "-NoLogo -NoProfile -NonInteractive -EncodedCommand " + Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
    }
}
