// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.TestPlatform.VsTestConsole.TranslationLayer;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Mono.Options;

namespace TestExecutor;

public record struct TestExecutorInfo(List<string> Assemblies, string FilterString);

internal static class Program
{
    internal const int ExitFailure = 1;
    internal const int ExitSuccess = 0;

    public static int Main(string[] args)
    {
        Console.WriteLine($"Running with args: {string.Join(" ", args)}");

        string? dotnetPath = null;
        string? workItemInfoPath = null;
        string? runsettingsPath = null;

        var options = new OptionSet()
        {
            { "dotnetPath=", "Path to the dotnet executable", (string s) => dotnetPath = s },
            { "workItemInfoPath=", "File path containing the test assemblies and filters to execute", (string s) => workItemInfoPath = s },
            { "runsettingsPath=", ".runsettings file path", (string s) => runsettingsPath = s },
        };
        options.Parse(args);

        if (dotnetPath is null)
        {
            Console.Error.WriteLine("--dotnetPath argument must be provided");
            return ExitFailure;
        }

        if (workItemInfoPath is null)
        {
            Console.Error.WriteLine("--workItemInfoPath argument must be provided");
            return ExitFailure;
        }

        if (!File.Exists(workItemInfoPath))
        {
            Console.Error.WriteLine("--workItemInfoPath must exist");
            return ExitFailure;
        }

        if (runsettingsPath is null)
        {
            Console.Error.WriteLine("--runsettingsPath argument must be provided");
            return ExitFailure;
        }

        if (!File.Exists(runsettingsPath))
        {
            Console.Error.WriteLine("--runsettingsPath must exist");
            return ExitFailure;
        }

        var runsettings = File.ReadAllText(runsettingsPath);
        var testExecutorInfo = JsonSerializer.Deserialize<TestExecutorInfo>(File.ReadAllText(workItemInfoPath));
        var result = TestPlatformWrapper.RunTests(testExecutorInfo.Assemblies, testExecutorInfo.FilterString, runsettings, dotnetPath);
        Console.WriteLine($"Test run finished with code {result.ExitCode}, ran to completion: {result.RanToCompletion}");
        Console.WriteLine("### Standard Output ####");
        Console.WriteLine(result.StandardOutput);
        Console.WriteLine("### Error Output ####");
        Console.WriteLine(result.ErrorOutput);
        return result.ExitCode;
    }
}
