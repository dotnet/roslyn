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
using System.Text;
using System.Threading.Tasks;
using Microsoft.TestPlatform.VsTestConsole.TranslationLayer;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace PrepareTests;
internal class TestDiscovery
{
    public static void RunDiscovery(string sourceDirectory, string dotnetPath, bool isUnix)
    {
        var binDirectory = Path.Combine("artifacts", "bin");
        var assemblies = GetAssemblies(binDirectory, isUnix);

        Console.WriteLine($"Found {assemblies} test assemblies");

        var vsTestConsole = Directory.EnumerateFiles(Path.Combine(Path.GetDirectoryName(dotnetPath), "sdk"), "vstest.console.dll", SearchOption.AllDirectories).Last();

        var vstestConsoleWrapper = new VsTestConsoleWrapper(vsTestConsole, new ConsoleParameters
        {
            LogFilePath = Path.Combine(AppContext.BaseDirectory, "logs", "vstestconsolelogs.txt"),
            TraceLevel = TraceLevel.Verbose,
        });

        var tests = new ConcurrentBag<string>();

        var stopwatch = new Stopwatch();
        stopwatch.Start();
        Parallel.ForEach(assemblies, (assembly) =>
        {
            Console.WriteLine($"Discovering {assembly}");
            vstestConsoleWrapper.DiscoverTests(assemblies, @"<RunSettings><RunConfiguration></RunConfiguration></RunSettings>", new DiscoveryHandler((test) => tests.Add(test)));
        });

        stopwatch.Stop();
        Console.WriteLine($"Discovered {tests.ToList()} in {stopwatch.Elapsed}");
    }

    private class DiscoveryHandler : ITestDiscoveryEventsHandler
    {
        private List<TestCase> _tests = new();
        private bool _isComplete = false;

        private readonly Action<string> _addTestsAction;

        public DiscoveryHandler(Action<string> addTestsAction)
        {
            _addTestsAction = addTestsAction;
        }

        public void HandleDiscoveredTests(IEnumerable<TestCase> discoveredTestCases)
        {
            foreach (var test in discoveredTestCases)
            {
                _addTestsAction(test.FullyQualifiedName);
            }
        }

        public void HandleDiscoveryComplete(long totalTests, IEnumerable<TestCase> lastChunk, bool isAborted)
        {
            _isComplete = true;
        }

        public void HandleLogMessage(TestMessageLevel level, string message)
        {
            Console.WriteLine(message);
        }

        public void HandleRawMessage(string rawMessage)
        {
        }

        public ImmutableArray<string> GetTests()
        {
            Contract.Assert(_isComplete);
            return _tests.Select(t => t.FullyQualifiedName).ToImmutableArray();
        }
    }


    private static List<string> GetAssemblies(string binDirectory, bool isUnix)
    {
        var unitTestAssemblies = Directory.GetFiles(binDirectory, "*UnitTests.dll", SearchOption.AllDirectories).Where(ShouldInclude);
        return unitTestAssemblies.ToList();

        bool ShouldInclude(string path)
        {
            if (isUnix)
            {
                return Path.GetFileName(Path.GetDirectoryName(path)) != "net472";
            }

            return true;
        }
    }
}
