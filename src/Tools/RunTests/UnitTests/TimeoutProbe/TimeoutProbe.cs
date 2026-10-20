// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Xunit;

namespace RunTests.TimeoutProbe;

public sealed class TimeoutProbe
{
    [Fact]
    public void IntentionalTimeout()
    {
        var mode = Environment.GetEnvironmentVariable("ROSLYN_TIMEOUT_PROBE_MODE");
        if (mode is null)
            return;

        var directory = Environment.GetEnvironmentVariable("ROSLYN_TIMEOUT_PROBE_OUTPUT")!;
        Directory.CreateDirectory(directory);
        using var self = Process.GetCurrentProcess();
        File.WriteAllText(Path.Combine(directory, "testhost.pid"), self.Id.ToString());
        Console.WriteLine($"Intentional timeout: mode={mode}, testhost PID={self.Id}, start={self.StartTime:O}.");
        if (mode != "host")
        {
            var fixture = Environment.GetEnvironmentVariable("ROSLYN_TIMEOUT_PROBE_FIXTURE")!;
            var dotnet = Environment.GetEnvironmentVariable("ROSLYN_TIMEOUT_PROBE_DOTNET")!;
            var ready = Path.Combine(directory, "child.pid");
            var arguments = $"exec --runtimeconfig \"{Path.ChangeExtension(fixture, ".runtimeconfig.json")}\" \"{fixture}\" leaf \"{ready}\"";
            using var child = Process.Start(new ProcessStartInfo(dotnet, arguments) { UseShellExecute = false })!;
            if (!SpinWait.SpinUntil(() => File.Exists(ready), TimeSpan.FromSeconds(30)))
                throw new TimeoutException("Intentional child did not become ready.");
            Console.WriteLine($"Intentional hung child: PID={child.Id}, start={child.StartTime:O}.");
        }
        if (mode == "exited-launcher")
        {
            var pid = int.Parse(Environment.GetEnvironmentVariable("ROSLYN_TIMEOUT_PROBE_LAUNCHER_PID")!);
            using var launcher = Process.GetProcessById(pid);
            Console.WriteLine($"Exiting ownership launcher PID={pid} while testhost and child retain inherited output.");
            launcher.Kill();
        }
        HungTestHost();
    }

    private static void HungTestHost() => Thread.Sleep(Timeout.Infinite);
}

public sealed class ActivityProbe
{
    public static IEnumerable<object[]> Cases()
    {
        for (var i = 0; i < 100; i++)
            yield return new object[] { i };
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void TestEventsKeepVSTestActive(int index)
    {
        if (Environment.GetEnvironmentVariable("ROSLYN_TIMEOUT_PROBE_ACTIVITY") == "1")
        {
            Console.WriteLine($"Activity test {index} maintains VSTest test events while another test is hung.");
            Thread.Sleep(TimeSpan.FromMinutes(1));
        }
    }
}
