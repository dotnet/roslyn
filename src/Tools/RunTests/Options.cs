// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;

namespace TestRunner.RunTests;

internal sealed class Options : CommonOptions
{
    public bool IncludeHtml { get; set; }
    public string? TestFilter { get; set; }
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(90);
    public bool CollectDumps { get; set; }
    public bool Sequential { get; set; }
    public string TestResultsDirectory { get; private set; } = "";
    public string LogFilesDirectory { get; private set; } = "";

    internal static Options? Parse(string[] args, out bool helpShown)
    {
        var options = new Options();
        string? resultsDirectory = null;
        string? logsDirectory = null;
        var optionSet = options.GetOptionSet();
        optionSet.Add("html", "Include HTML file output", o => options.IncludeHtml = o is object);
        optionSet.Add("sequential", "Run tests sequentially", o => options.Sequential = o is object);
        optionSet.Add("testfilter=", "VSTest filter, e.g. FullyQualifiedName~TestClass1|Category=CategoryA", s => options.TestFilter = s);
        optionSet.Add<int>("timeout=", "Minute timeout to limit the tests to (default: 90)", i => options.Timeout = TimeSpan.FromMinutes(i));
        optionSet.Add("out=", "Test result file directory", s => resultsDirectory = s);
        optionSet.Add("logs=", "Log file directory", s => logsDirectory = s);
        optionSet.Add("collectdumps", "Gather dumps on timeouts and crashes", o => options.CollectDumps = o is object);

        if (!options.ParseCore(args, optionSet, "RunTests", "Discovers and runs local test assemblies from the artifacts/bin directory.", out helpShown))
        {
            return null;
        }

        options.TestResultsDirectory = resultsDirectory ?? Path.Combine(options.ArtifactsDirectory, "TestResults", options.Configuration);
        options.LogFilesDirectory = logsDirectory ?? options.TestResultsDirectory;
        return options;
    }
}
