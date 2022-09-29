// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.TestPlatform.VsTestConsole.TranslationLayer;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace PrepareTests;
internal class TestDiscovery
{
    public static void RunDiscovery(string repoRootDirectory, string dotnetPath, bool isUnix)
    {
        var binDirectory = Path.Combine(repoRootDirectory, "artifacts", "bin");
        var assemblies = GetAssemblies(binDirectory, isUnix);

        Console.WriteLine($"Found {assemblies.Count} test assemblies");

        var vsTestConsole = Directory.EnumerateFiles(Path.Combine(Path.GetDirectoryName(dotnetPath)!, "sdk"), "vstest.console.dll", SearchOption.AllDirectories).OrderBy(s => s).Last();

        var vstestConsoleWrapper = new VsTestConsoleWrapper(vsTestConsole, new ConsoleParameters
        {
            LogFilePath = Path.Combine(repoRootDirectory, "logs", "test_discovery_logs.txt"),
            TraceLevel = TraceLevel.Error,
        });

        var discoveryHandler = new DiscoveryHandler();

        var stopwatch = new Stopwatch();
        stopwatch.Start();
        vstestConsoleWrapper.DiscoverTests(assemblies, @"<RunSettings><RunConfiguration><MaxCpuCount>0</MaxCpuCount></RunConfiguration></RunSettings>", discoveryHandler);
        stopwatch.Stop();

        var tests = discoveryHandler.GetTests();

        Console.WriteLine($"Discovered {tests.Length} tests in {stopwatch.Elapsed}");

        stopwatch.Restart();
        var testGroupedByAssembly = tests.GroupBy(test => test.Source);
        foreach (var assemblyGroup in testGroupedByAssembly)
        {
            var directory = Path.GetDirectoryName(assemblyGroup.Key);

            // Tests with combinatorial data are output multiple times with the same fully qualified test name.
            // We only need to include it once as run all combinations under the same filter.
            var testToWrite = assemblyGroup.Select(test => test.FullyQualifiedName).Distinct().ToList();

            using var fileStream = File.Create(Path.Combine(directory!, "testlist.json"));
            JsonSerializer.Serialize(fileStream, testToWrite);
        }
        stopwatch.Stop();
        Console.WriteLine($"Serialized tests in {stopwatch.Elapsed}");
    }

    private class DiscoveryHandler : ITestDiscoveryEventsHandler
    {
        private readonly ConcurrentBag<TestCase> _tests = new();
        private bool _isComplete = false;

        public void HandleDiscoveredTests(IEnumerable<TestCase>? discoveredTestCases)
        {
            if (discoveredTestCases != null)
            {
                foreach (var test in discoveredTestCases)
                {
                    _tests.Add(test);
                }
            }
        }

        public void HandleDiscoveryComplete(long totalTests, IEnumerable<TestCase>? lastChunk, bool isAborted)
        {
            if (lastChunk != null)
            {
                foreach (var test in lastChunk)
                {
                    _tests.Add(test);
                }
            }

            _isComplete = true;
        }

        public void HandleLogMessage(TestMessageLevel level, string? message)
        {
            Console.WriteLine(message);
        }

        public void HandleRawMessage(string rawMessage)
        {
        }

        public ImmutableArray<TestCase> GetTests()
        {
            Contract.Assert(_isComplete);
            return _tests.ToImmutableArray();
        }
    }

    private static List<string> GetAssemblies(string binDirectory, bool isUnix)
    {
        var unitTestAssemblies = Directory.GetFiles(binDirectory, "*.UnitTests.dll", SearchOption.AllDirectories).Where(ShouldInclude);
        return unitTestAssemblies.ToList();

        bool ShouldInclude(string path)
        {
            if (isUnix)
            {
                // Our unix build will build net framework dlls for multi-targeted projects.
                // These are not valid testing on unix and discovery will throw if we try.
                return Path.GetFileName(Path.GetDirectoryName(path)) != "net472";
            }

            return true;
        }
    }
}
