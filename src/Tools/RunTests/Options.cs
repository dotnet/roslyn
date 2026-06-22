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
    [Flags]
    internal enum TestRuntime
    {
        Core = 1,
        Framework = 2,
    }

    internal class Options
    {
        /// <summary>
        /// The list of compiler test assembly include patterns used when --testSet compiler is specified.
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
        public TestRuntime TestRuntime { get; set; } = TestRuntime.Core | TestRuntime.Framework;

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

        internal static Options? Parse(string[] args, out bool helpShown)
        {
            helpShown = false;
            string? dotnetFilePath = null;
            var platform = Microsoft.CodeAnalysis.Test.Utilities.IlasmUtilities.Architecture;
            var includeHtml = false;
            var testRuntime = TestRuntime.Core | TestRuntime.Framework;
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
            var testFrameworks = new List<string>();
            string? testSet = null;
            string? testKind = null;
            var showHelp = false;
            var environmentVariables = new Dictionary<string, string>(
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);

            var optionSet = new OptionSet()
            {
                { "h|help|?", "Show this help message and exit", o => showHelp = o is object },
                { "dotnet=", "Path to dotnet", s => dotnetFilePath = s },
                { "testConfiguration=", "Configuration to test: Debug or Release", s => configuration = s },
                { "include=", "Regex for including unit test dlls (can be specified multiple times). Default: .*UnitTests.*", s => includeFilter.Add(s) },
                { "exclude=", "Regex for excluding unit test dlls (can be specified multiple times)", s => excludeFilter.Add(s) },
                { "testPlatform=", "Architecture to test on: x86, x64 or arm64", s => platform = s },
                { "html", "Include HTML file output", o => includeHtml = o is object },
                { "helix", "Run tests on Helix", o => helix = o is object },
                { "helixQueueName=", "Name of the Helix queue to run tests on", s => helixQueueName = s },
                { "helixApiAccessToken=", "Access token for internal helix queues", s => helixApiAccessToken = s },
                { "testfilter=", "xUnit string to pass to --filter, e.g. FullyQualifiedName~TestClass1|Category=CategoryA", s => testFilter = s },
                { "timeout=", "Minute timeout to limit the tests to (default: 90, not supported with --helix)", (int i) => timeout = i },
                { "out=", "Test result file directory (when running on Helix, this is relative to the Helix work item directory)", s => resultFileDirectory = s },
                { "logs=", "Log file directory (when running on Helix, this is relative to the Helix work item directory)", s => logFileDirectory = s },

                { "artifactspath=", "Path to the artifacts directory (auto-detected from binary location if not set)", s => artifactsPath = s },
                { "collectdumps", "Gather dumps on timeouts and crashes (process executor only, not supported with --helix)", o => collectDumps = o is object },
                { "testFramework:", "Test framework to run: core or desktop (can be specified multiple times)", s => testFrameworks.Add(s) },
                { "testSet:", "Test set to run: compiler (restricts to compiler test assemblies)", s => testSet = s },
                { "testKind:", "Test kind to run: ioperation, runtimeasync, usedassemblies", s => testKind = s },
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

            if (showHelp)
            {
                ConsoleUtil.WriteLine("Usage: RunTests [OPTIONS]");
                ConsoleUtil.WriteLine();
                ConsoleUtil.WriteLine("Discovers and runs test assemblies from the artifacts/bin directory.");
                ConsoleUtil.WriteLine("Test assemblies are matched by --include/--exclude regex patterns against project folder names.");
                ConsoleUtil.WriteLine();
                ConsoleUtil.WriteLine("Options:");
                optionSet.WriteOptionDescriptions(Console.Out);
                helpShown = true;
                return null;
            }

            if (testFrameworks.Count > 0)
            {
                testRuntime = 0;
                foreach (var tf in testFrameworks)
                {
                    if (string.Equals(tf, "core", StringComparison.OrdinalIgnoreCase))
                    {
                        testRuntime |= TestRuntime.Core;
                    }
                    else if (string.Equals(tf, "desktop", StringComparison.OrdinalIgnoreCase))
                    {
                        testRuntime |= TestRuntime.Framework;
                    }
                    else
                    {
                        ConsoleUtil.WriteLine($"Invalid --testFramework value '{tf}'. Must be 'core' or 'desktop'.");
                        return null;
                    }
                }
            }

            var testDesktop = (testRuntime & TestRuntime.Framework) != 0;
            var testCoreClr = (testRuntime & TestRuntime.Core) != 0;

            var testCompilerOnly = string.Equals(testSet, "compiler", StringComparison.OrdinalIgnoreCase);

            if (testSet is not null && !testCompilerOnly)
            {
                ConsoleUtil.WriteLine($"Invalid --testSet value '{testSet}'. Must be 'compiler'.");
                return null;
            }

            if (testKind is not null)
            {
                if (string.Equals(testKind, "ioperation", StringComparison.OrdinalIgnoreCase))
                {
                    environmentVariables["ROSLYN_TEST_IOPERATION"] = "true";
                }
                else if (string.Equals(testKind, "runtimeasync", StringComparison.OrdinalIgnoreCase))
                {
                    environmentVariables["DOTNET_RuntimeAsync"] = "1";
                }
                else if (string.Equals(testKind, "usedassemblies", StringComparison.OrdinalIgnoreCase))
                {
                    environmentVariables["ROSLYN_TEST_USEDASSEMBLIES"] = "true";
                }
                else
                {
                    ConsoleUtil.WriteLine($"Invalid --testKind value '{testKind}'. Must be 'ioperation', 'runtimeasync', or 'usedassemblies'.");
                    return null;
                }
            }

            if (testDesktop && environmentVariables.ContainsKey("DOTNET_RuntimeAsync"))
            {
                ConsoleUtil.WriteLine("Cannot run desktop tests with runtime async validation enabled.");
                return null;
            }

            if (testCompilerOnly)
            {
                foreach (var pattern in CompilerTestAssemblyPatterns)
                {
                    includeFilter.Add(pattern);
                }
            }

            // Desktop x64 excludes InteractiveHost tests
            if (testDesktop && platform != "x86")
            {
                excludeFilter.Add(@"\.InteractiveHost");
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
                    ConsoleUtil.Error("--collectdumps is not supported with --helix. Dump collection is only available for process-based test execution.");
                    return null;
                }

                if (timeout is not null)
                {
                    ConsoleUtil.Error("--timeout is not supported with --helix. Timeout is managed by the helix infrastructure.");
                    return null;
                }

                if (includeHtml)
                {
                    ConsoleUtil.Error("--html is not supported with --helix.");
                    return null;
                }

                if (testFilter is not null)
                {
                    ConsoleUtil.Error("--testfilter is not supported with --helix.");
                    return null;
                }
            }
            else
            {
                timeout ??= 90;

                if (helixApiAccessToken is not null)
                {
                    ConsoleUtil.Error("--helixApiAccessToken is not supported without --helix.");
                    return null;
                }

                if (helixQueueName != "Windows.10.Amd64.Open")
                {
                    ConsoleUtil.Error("--helixQueueName is not supported without --helix.");
                    return null;
                }
            }

            return new Options(
                dotnetFilePath: dotnetFilePath,
                artifactsDirectory: artifactsPath,
                configuration: configuration,
                testResultsDirectory: resultFileDirectory,
                logFilesDirectory: logFileDirectory,
                architecture: platform)
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
