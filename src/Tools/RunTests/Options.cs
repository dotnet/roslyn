// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Mono.Options;

namespace RunTests
{
    internal enum TestRuntime
    {
        Both,
        Core,
        Framework
    }

    internal class Options
    {
        /// <summary>
        /// The list of compiler test assembly include patterns used when --testCompilerOnly is specified.
        /// </summary>
        internal static readonly string[] CompilerTestAssemblyPatterns = new[]
        {
            @"^Microsoft\.CodeAnalysis\.UnitTests$",
            @"^Microsoft\.CodeAnalysis\.CompilerServer\.UnitTests$",
            @"^Microsoft\.CodeAnalysis\.CSharp\.Syntax\.UnitTests$",
            @"^Microsoft\.CodeAnalysis\.CSharp\.Symbol\.UnitTests$",
            @"^Microsoft\.CodeAnalysis\.CSharp\.Semantic\.UnitTests$",
            @"^Microsoft\.CodeAnalysis\.CSharp\.Emit\.UnitTests$",
            @"^Microsoft\.CodeAnalysis\.CSharp\.Emit2\.UnitTests$",
            @"^Microsoft\.CodeAnalysis\.CSharp\.Emit3\.UnitTests$",
            @"^Microsoft\.CodeAnalysis\.CSharp\.CSharp15\.UnitTests$",
            @"^Microsoft\.CodeAnalysis\.CSharp\.IOperation\.UnitTests$",
            @"^Microsoft\.CodeAnalysis\.CSharp\.CommandLine\.UnitTests$",
            @"^Microsoft\.CodeAnalysis\.VisualBasic\.Syntax\.UnitTests$",
            @"^Microsoft\.CodeAnalysis\.VisualBasic\.Symbol\.UnitTests$",
            @"^Microsoft\.CodeAnalysis\.VisualBasic\.Semantic\.UnitTests$",
            @"^Microsoft\.CodeAnalysis\.VisualBasic\.Emit\.UnitTests$",
            @"^Roslyn\.Compilers\.VisualBasic\.IOperation\.UnitTests$",
            @"^Microsoft\.CodeAnalysis\.VisualBasic\.CommandLine\.UnitTests$",
            @"^Microsoft\.Build\.Tasks\.CodeAnalysis\.UnitTests$",
        };

        /// <summary>
        /// Use HTML output files.
        /// </summary>
        public bool IncludeHtml { get; set; }

        /// <summary>
        /// Filter string to pass to xunit.
        /// </summary>
        public string? TestFilter { get; set; }

        public string Configuration { get; set; }

        /// <summary>
        /// The set of target frameworks that should be probed for test assemblies.
        /// </summary>
        public TestRuntime TestRuntime { get; set; } = TestRuntime.Both;

        public List<string> IncludeFilter { get; set; } = new List<string>();

        public List<string> ExcludeFilter { get; set; } = new List<string>();

        public string ArtifactsDirectory { get; }

        /// <summary>
        /// Time after which the runner should kill the xunit process and exit with a failure.
        /// </summary>
        public TimeSpan? Timeout { get; set; }

        /// <summary>
        /// Whether or not to collect dumps on crashes and timeouts.
        /// </summary>
        public bool CollectDumps { get; set; }

        /// <summary>
        /// Whether to run test partitions as Helix work items.
        /// </summary>
        public bool UseHelix { get; set; }

        /// <summary>
        /// Name of the Helix queue to run tests on (only valid when <see cref="UseHelix" /> is <see langword="true" />).
        /// </summary>
        public string? HelixQueueName { get; set; }

        /// <summary>
        /// Access token to send jobs to helix (only valid when <see cref="UseHelix" /> is <see langword="true" />).
        /// This should only be set when using internal helix queues.
        /// </summary>
        public string? HelixApiAccessToken { get; set; }

        /// <summary>
        /// Path to the dotnet executable we should use for running dotnet test
        /// </summary>
        public string DotnetFilePath { get; set; }

        /// <summary>
        /// Directory to hold all of the xml files created as test results.
        /// </summary>
        public string TestResultsDirectory { get; set; }

        /// <summary>
        /// Directory to hold dump files and other log files created while running tests.
        /// </summary>
        public string LogFilesDirectory { get; set; }

        public string Architecture { get; set; }

        /// <summary>
        /// Environment variables to set in test execution processes.
        /// Populated from --env:KEY=VALUE command line arguments.
        /// </summary>
        public Dictionary<string, string> EnvironmentVariables { get; set; } = new();

        public Options(
            string dotnetFilePath,
            string artifactsDirectory,
            string configuration,
            string testResultsDirectory,
            string logFilesDirectory,
            string architecture)
        {
            DotnetFilePath = dotnetFilePath;
            ArtifactsDirectory = artifactsDirectory;
            Configuration = configuration;
            TestResultsDirectory = testResultsDirectory;
            LogFilesDirectory = logFilesDirectory;
            Architecture = architecture;
        }

        internal static Options? Parse(string[] args)
        {
            string? dotnetFilePath = null;
            var architecture = Microsoft.CodeAnalysis.Test.Utilities.IlasmUtilities.Architecture;
            var includeHtml = false;
            var testRuntime = TestRuntime.Both;
            var configuration = "Debug";
            var includeFilter = new List<string>();
            var excludeFilter = new List<string>();
            var helix = false;
            var helixQueueName = "Windows.10.Amd64.Open";
            string? helixApiAccessToken = null;
            string? testFilter = null;
            int? timeout = null;
            string? resultFileDirectory = null;
            string? logFileDirectory = null;
            var collectDumps = false;
            string? artifactsPath = null;

            // High-level test category flags (previously handled by build.ps1/build.sh)
            string? testFramework = null;
            string? testSet = null;
            var environmentVariables = new Dictionary<string, string>(
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);

            var optionSet = new OptionSet()
            {
                { "dotnet=", "Path to dotnet", s => dotnetFilePath = s },
                { "testConfiguration=", "Configuration to test: Debug or Release", s => configuration = s },
                { "include=", "Expression for including unit test dlls: default *.UnitTests.dll", s => includeFilter.Add(s) },
                { "exclude=", "Expression for excluding unit test dlls: default is empty", s => excludeFilter.Add(s) },
                { "testPlatform=", "Architecture to test on: x86, x64 or arm64", s => architecture = s },
                { "html", "Include HTML file output", o => includeHtml = o is object },
                { "helix", "Run tests on Helix", o => helix = o is object },
                { "helixQueueName=", "Name of the Helix queue to run tests on", s => helixQueueName = s },
                { "helixApiAccessToken=", "Access token for internal helix queues", s => helixApiAccessToken = s },
                { "testfilter=", "xUnit string to pass to --filter, e.g. FullyQualifiedName~TestClass1|Category=CategoryA", s => testFilter = s },
                { "timeout=", "Minute timeout to limit the tests to", (int i) => timeout = i },
                { "out=", "Test result file directory (when running on Helix, this is relative to the Helix work item directory)", s => resultFileDirectory = s },
                { "logs=", "Log file directory (when running on Helix, this is relative to the Helix work item directory)", s => logFileDirectory = s },

                { "artifactspath=", "Path to the artifacts directory", s => artifactsPath = s },
                { "collectdumps", "Whether or not to gather dumps on timeouts and crashes (process executor only)", o => collectDumps = o is object },
                { "testFramework:", "Test framework to run: core, desktop, or both", s => testFramework = s },
                { "testSet:", "Test set to run: compiler", s => testSet = s },
                { "ci", "Running in CI - sets ROSLYN_TEST_CI=true in test processes", o => {
                    if (o is object)
                        environmentVariables["ROSLYN_TEST_CI"] = "true";
                }},
                { "env:", "Set an environment variable in test processes (format: --env:KEY=VALUE or --env:KEY for KEY=true)", s => {
                    var eqIndex = s.IndexOf('=');
                    if (eqIndex >= 0)
                    {
                        environmentVariables[s[..eqIndex]] = s[(eqIndex + 1)..];
                    }
                    else
                    {
                        environmentVariables[s] = "true";
                    }
                }},
            };

            List<string> assemblyList;
            try
            {
                assemblyList = optionSet.Parse(args);
            }
            catch (OptionException e)
            {
                ConsoleUtil.WriteLine($"Error parsing command line arguments: {e.Message}");
                optionSet.WriteOptionDescriptions(Console.Out);
                return null;
            }

            // Apply high-level test category flags. These replicate the logic previously
            // in build.ps1's TestUsingRunTests function.
            var testDesktop = string.Equals(testFramework, "desktop", StringComparison.OrdinalIgnoreCase);
            var testCoreClr = string.Equals(testFramework, "core", StringComparison.OrdinalIgnoreCase);
            var testBoth = string.Equals(testFramework, "both", StringComparison.OrdinalIgnoreCase);

            if (testFramework is not null && !testDesktop && !testCoreClr && !testBoth)
            {
                ConsoleUtil.WriteLine($"Invalid --testFramework value '{testFramework}'. Must be 'core', 'desktop', or 'both'.");
                return null;
            }

            var testCompilerOnly = string.Equals(testSet, "compiler", StringComparison.OrdinalIgnoreCase);

            if (testSet is not null && !testCompilerOnly)
            {
                ConsoleUtil.WriteLine($"Invalid --testSet value '{testSet}'. Must be 'compiler'.");
                return null;
            }

            if (testDesktop || testCoreClr || testBoth)
            {
                if (testDesktop && environmentVariables.ContainsKey("DOTNET_RuntimeAsync"))
                {
                    ConsoleUtil.WriteLine("Cannot run desktop tests with runtime async validation enabled.");
                    return null;
                }

                testRuntime = testBoth ? TestRuntime.Both : testDesktop ? TestRuntime.Framework : TestRuntime.Core;
                timeout ??= 90;

                if (testCompilerOnly)
                {
                    foreach (var pattern in CompilerTestAssemblyPatterns)
                    {
                        includeFilter.Add(pattern);
                    }
                }
                else
                {
                    if (includeFilter.Count == 0)
                    {
                        includeFilter.Add(@"\.UnitTests");
                    }
                }

                // Desktop x64 excludes InteractiveHost tests
                if (testDesktop && architecture != "x86")
                {
                    excludeFilter.Add(@"\.InteractiveHost");
                }
            }

            if (includeFilter.Count == 0)
            {
                includeFilter.Add(".*UnitTests.*");
            }

            artifactsPath ??= TryGetArtifactsPath();
            if (artifactsPath is null || !Directory.Exists(artifactsPath))
            {
                ConsoleUtil.WriteLine($"Did not find artifacts directory at {artifactsPath}");
                return null;
            }

            resultFileDirectory ??= helix
                ? "."
                : Path.Combine(artifactsPath, "TestResults", configuration);

            logFileDirectory ??= resultFileDirectory;

            dotnetFilePath ??= TryGetDotNetPath();
            if (dotnetFilePath is null || !File.Exists(dotnetFilePath))
            {
                ConsoleUtil.WriteLine($"Did not find 'dotnet' at {dotnetFilePath}");
                return null;
            }

            if (helix)
            {
                if (collectDumps)
                {
                    ConsoleUtil.WriteLine("--collectdumps is not supported with --helix. Dump collection is only available for process-based test execution.");
                    return null;
                }

                if (timeout is not null)
                {
                    ConsoleUtil.WriteLine("--timeout is not supported with --helix. Timeout is managed by the helix infrastructure.");
                    return null;
                }

                if (includeHtml)
                {
                    ConsoleUtil.WriteLine("--html is not supported with --helix.");
                    return null;
                }
            }
            else
            {
                if (helixApiAccessToken is not null)
                {
                    ConsoleUtil.WriteLine("--helixApiAccessToken is not supported without --helix.");
                    return null;
                }

                if (helixQueueName != "Windows.10.Amd64.Open")
                {
                    ConsoleUtil.WriteLine("--helixQueueName is not supported without --helix.");
                    return null;
                }
            }

            return new Options(
                dotnetFilePath: dotnetFilePath,
                artifactsDirectory: artifactsPath,
                configuration: configuration,
                testResultsDirectory: resultFileDirectory,
                logFilesDirectory: logFileDirectory,
                architecture: architecture)
            {
                TestRuntime = testRuntime,
                IncludeFilter = includeFilter,
                ExcludeFilter = excludeFilter,

                CollectDumps = collectDumps,
                UseHelix = helix,
                HelixQueueName = helixQueueName,
                HelixApiAccessToken = helixApiAccessToken,
                IncludeHtml = includeHtml,
                TestFilter = testFilter,
                Timeout = timeout is { } t ? TimeSpan.FromMinutes(t) : null,
                EnvironmentVariables = environmentVariables,
            };

            static string? TryGetArtifactsPath()
            {
                var path = AppContext.BaseDirectory;
                while (path is object && Path.GetFileName(path) != "artifacts")
                {
                    path = Path.GetDirectoryName(path);
                }

                return path;
            }

            static string? TryGetDotNetPath()
            {
                var dir = RuntimeEnvironment.GetRuntimeDirectory();
                var programName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dotnet.exe" : "dotnet";

                while (dir != null && !File.Exists(Path.Combine(dir, programName)))
                {
                    dir = Path.GetDirectoryName(dir);
                }

                return dir == null ? null : Path.Combine(dir, programName);
            }
        }
    }
}
