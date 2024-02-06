// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Mono.Options;

namespace PrepareTests;

internal static class Program
{
    internal const int ExitFailure = 1;
    internal const int ExitSuccess = 0;

    public static int Main(string[] args)
    {
        string? source = null;
        string? destination = null;
        bool isUnix = false;
        string? dotnetPath = null;

        var options = new OptionSet()
        {
            { "source=", "Path to binaries", (string s) => source = s },
            { "destination=", "Output path", (string s) => destination = s },
            { "unix", "If true, prepares tests for unix environment instead of Windows", o => isUnix = o is object },
            { "dotnetPath=", "Path to the dotnet CLI", (string s) => dotnetPath = s },
        };
        options.Parse(args);

        if (source is null)
        {
            Console.Error.WriteLine("--source argument must be provided");
            return ExitFailure;
        }

        if (destination is null)
        {
            Console.Error.WriteLine("--destination argument must be provided");
            return ExitFailure;
        }

        if (dotnetPath is null)
        {
            dotnetPath = "dotnet";
        }

        var success = TestDiscovery.RunDiscovery(source, dotnetPath, isUnix);
        if (!success)
        {
            return ExitFailure;
        }

        MinimizeUtil.Run(source, destination, isUnix);
        return ExitSuccess;
    }
}
