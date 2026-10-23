// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

public static class StartupHook
{
    public static void Initialize()
    {
        var mode = Environment.GetEnvironmentVariable("ROSLYN_DIRECT_PROOF_MODE");
        if (mode is null)
            return;
        var launcher = false;
        foreach (var arg in Environment.GetCommandLineArgs())
            launcher |= arg.EndsWith("vstest.console.dll", StringComparison.OrdinalIgnoreCase);
        if (!launcher)
        {
            if (mode == "exited-launcher")
                AppContext.SetSwitch("MSTest.EnableParentProcessQuery", false);
            return;
        }
        var directory = Environment.GetEnvironmentVariable("ROSLYN_DIRECT_PROOF_OUTPUT")!;
        Directory.CreateDirectory(directory);
        using var self = Process.GetCurrentProcess();
        File.WriteAllText(Path.Combine(directory, "launcher.pid"), self.Id.ToString());
        if (mode == "startup-child")
        {
            var dotnet = Environment.GetEnvironmentVariable("ROSLYN_DIRECT_PROOF_DOTNET")!;
            var runner = Environment.GetEnvironmentVariable("ROSLYN_DIRECT_PROOF_RUNNER")!;
            var ready = Path.Combine(directory, "child.pid");
            var info = new ProcessStartInfo(dotnet, $"\"{runner}\" --direct-proof-child \"{ready}\"") { UseShellExecute = false };
            info.Environment.Remove("DOTNET_STARTUP_HOOKS");
            using var child = Process.Start(info)!;
            if (!SpinWait.SpinUntil(() => File.Exists(ready), TimeSpan.FromSeconds(30)))
                throw new TimeoutException("Startup-hook child did not become ready.");
            Environment.Exit(0);
        }
    }
}
