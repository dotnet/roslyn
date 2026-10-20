// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

public static class StartupHook
{
    public static void Initialize()
    {
        var readyPath = Environment.GetEnvironmentVariable("RUNTESTS_FIXTURE_STARTUP_PID");
        if (readyPath is null)
            return;

        var info = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false };
        info.Environment.Remove("DOTNET_STARTUP_HOOKS");
        info.ArgumentList.Add("exec");
        info.ArgumentList.Add("--runtimeconfig");
        info.ArgumentList.Add(Environment.GetEnvironmentVariable("RUNTESTS_FIXTURE_RUNTIME_CONFIG")!);
        info.ArgumentList.Add(typeof(RunTests.ProcessFixture.Program).Assembly.Location);
        info.ArgumentList.Add("leaf");
        info.ArgumentList.Add(readyPath);
        using var child = Process.Start(info)!;
        Environment.Exit(0);
    }
}
