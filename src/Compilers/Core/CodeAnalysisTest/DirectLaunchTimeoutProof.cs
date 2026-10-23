// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public sealed class DirectLaunchTimeoutProof
    {
        [Fact]
        public void IntentionalTimeout()
        {
            var mode = Environment.GetEnvironmentVariable("ROSLYN_DIRECT_PROOF_MODE");
            if (mode is null)
                return;
            var directory = Environment.GetEnvironmentVariable("ROSLYN_DIRECT_PROOF_OUTPUT")!;
            Directory.CreateDirectory(directory);
            using var self = Process.GetCurrentProcess();
            File.WriteAllText(Path.Combine(directory, "testhost.pid"), self.Id.ToString());
            if (mode != "host")
            {
                var dotnet = Environment.GetEnvironmentVariable("ROSLYN_DIRECT_PROOF_DOTNET")!;
                var runner = Environment.GetEnvironmentVariable("ROSLYN_DIRECT_PROOF_RUNNER")!;
                var ready = Path.Combine(directory, "child.pid");
                var info = new ProcessStartInfo(dotnet, $"\"{runner}\" --direct-proof-child \"{ready}\"") { UseShellExecute = false };
                info.EnvironmentVariables.Remove("DOTNET_STARTUP_HOOKS");
                using var child = Process.Start(info)!;
                if (!SpinWait.SpinUntil(() => File.Exists(Path.Combine(directory, "child.pid")), TimeSpan.FromSeconds(30)))
                    throw new TimeoutException("Hung child did not become ready.");
            }
            if (mode == "exited-launcher")
            {
                var pid = int.Parse(File.ReadAllText(Path.Combine(directory, "launcher.pid")));
#if NET
                // VSTest redirects testhost output. Retain its original output pipe explicitly for this orphaned-pipe scenario.
                using var launcherOutput = OperatingSystem.IsLinux()
                    ? new FileStream($"/proc/{pid}/fd/1", FileMode.Open, FileAccess.Write, FileShare.ReadWrite)
                    : null;
                new Thread(HungTestHost) { IsBackground = false }.Start();
#endif
                using var launcher = Process.GetProcessById(pid);
                launcher.Kill();
                HungTestHost();
            }
            HungTestHost();
        }

        private static void HungTestHost() => Thread.Sleep(Timeout.Infinite);
    }
}
