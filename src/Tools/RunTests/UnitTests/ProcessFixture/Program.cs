// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace RunTests.ProcessFixture;

public static class Program
{
    public static int Main(string[] args)
    {
        var mode = args[0];
        var readyPath = args[1];
        if (mode == "exit")
            return 0;
        if (mode == "fail")
            return 7;
        if (mode == "dump")
        {
            File.WriteAllText(readyPath, "completed collector output");
            return 0;
        }

        if (mode == "leaf")
        {
            File.WriteAllText(readyPath, Environment.ProcessId.ToString());
            HungChild();
            return 0;
        }

        var info = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false };
        info.ArgumentList.Add("exec");
        info.ArgumentList.Add("--runtimeconfig");
        info.ArgumentList.Add(args[2]);
        info.ArgumentList.Add(typeof(Program).Assembly.Location);
        info.ArgumentList.Add("leaf");
        info.ArgumentList.Add(readyPath);
        using var child = Process.Start(info)!;
        if (mode == "orphan")
            return 0;

        HungParent();
        return 0;
    }

    private static void HungChild() => Thread.Sleep(Timeout.Infinite);
    private static void HungParent() => Thread.Sleep(Timeout.Infinite);
}
