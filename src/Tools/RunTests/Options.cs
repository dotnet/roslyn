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
    internal enum Display
    {
        None,
        All,
        Succeeded,
        Failed,
    }

    internal enum TestRuntime
    {
        Both,
        Core,
        Framework
    }

    internal class Options
    {
        /// <summary>
        /// Use HTML output files.
        /// </summary>
        public bool IncludeHtml { get; set; }

        /// <summary>
        /// Display the results files.
        /// </summary>
        public Display Display { get; set; }

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
        /// The path to procdump.exe
        /// </summary>
        public string? ProcDumpFilePath { get; set; }

        /// <summary>
        /// Disable partitioning and parallelization across test assemblies.
        /// </summary>
        public bool Sequential { get; set; }

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
            var sequential = false;
            var helix = false;
            var helixQueueName = "Windows.10.Amd64.Open";
            string? helixApiAccessToken = null;
            string? testFilter = null;
            int? timeout = null;
            string? resultFileDirectory = null;
            string? logFileDirectory = null;
            var display = Display.None;
            var collectDumps = false;
            string? procDumpFilePath = null;
            string? artifactsPath = null;

            // High-level test category flags (previously handled by build.ps1/build.sh)
            string? testFramework = null;
            var testCompilerOnly = false;
            var environmentVariables = new Dictionary<string, string>();

            var optionSet = new OptionSet()
            {
                { "dotnet=", "Path to dotnet", s => dotnetFilePath = s },
                { "configuration=", "Configuration to test: Debug or Release", s => configuration = s },
                { "runtime=", "The runtime to test: both, core or framework", (TestRuntime t) => testRuntime = t},
                { "include=", "Expression for including unit test dlls: default *.UnitTests.dll", s => includeFilter.Add(s) },
                { "exclude=", "Expression for excluding unit test dlls: default is empty", s => excludeFilter.Add(s) },
                { "arch=", "Architecture to test on: x86, x64 or arm64", s => architecture = s },
                { "html", "Include HTML file output", o => includeHtml = o is object },
                { "sequential", "Run tests sequentially", o => sequential = o is object },
                { "helix", "Run tests on Helix", o => helix = o is object },
                { "helixQueueName=", "Name of the Helix queue to run tests on", s => helixQueueName = s },
                { "helixApiAccessToken=", "Access token for internal helix queues", s => helixApiAccessToken = s },
                { "testfilter=", "xUnit string to pass to --filter, e.g. FullyQualifiedName~TestClass1|Category=CategoryA", s => testFilter = s },
                { "timeout=", "Minute timeout to limit the tests to", (int i) => timeout = i },
                { "out=", "Test result file directory (when running on Helix, this is relative to the Helix work item directory)", s => resultFileDirectory = s },
                { "logs=", "Log file directory (when running on Helix, this is relative to the Helix work item directory)", s => logFileDirectory = s },
                { "display=", "Display", (Display d) => display = d },
                { "artifactspath=", "Path to the artifacts directory", s => artifactsPath = s },
                { "procdumppath=", "Path to procdump", s => procDumpFilePath = s },
                { "collectdumps", "Whether or not to gather dumps on timeouts and crashes", o => collectDumps = o is object },
                { "testFramework:", "Test framework to run: core or desktop", s => testFramework = s },
                { "testCompilerOnly", "Only run compiler test assemblies", o => testCompilerOnly = o is object },
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

            if (testFramework is not null && !testDesktop && !testCoreClr)
            {
                ConsoleUtil.WriteLine($"Invalid --testFramework value '{testFramework}'. Must be 'core' or 'desktop'.");
                return null;
            }

            if (testDesktop || testCoreClr)
            {
                if (testDesktop && environmentVariables.ContainsKey("DOTNET_RuntimeAsync"))
                {
                    ConsoleUtil.WriteLine("Cannot run desktop tests with runtime async validation enabled.");
                    return null;
                }

                testRuntime = testDesktop ? TestRuntime.Framework : TestRuntime.Core;
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

            // Auto-discover procdump on Windows when collectDumps is set but no path specified
            if (collectDumps && procDumpFilePath is null && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                procDumpFilePath = TryFindProcDump(artifactsPath);
            }

            if (procDumpFilePath is { } && !collectDumps)
            {
                ConsoleUtil.WriteLine($"procdumppath was specified without collectdumps hence it will not be used");
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
                Display = display,
                ProcDumpFilePath = procDumpFilePath,
                CollectDumps = collectDumps,
                Sequential = sequential,
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

            static string? TryFindProcDump(string artifactsPath)
            {
                // Check well-known locations for procdump
                var sysInternalsPath = @"C:\SysInternals\procdump.exe";
                if (File.Exists(sysInternalsPath))
                {
                    return @"C:\SysInternals";
                }

                // Check the tools directory relative to artifacts
                var repoRoot = Path.GetDirectoryName(artifactsPath);
                if (repoRoot is not null)
                {
                    var toolsPath = Path.Combine(repoRoot, ".tools", "ProcDump", "procdump.exe");
                    if (File.Exists(toolsPath))
                    {
                        return Path.GetDirectoryName(toolsPath);
                    }
                }

                return null;
            }
        }
    }
}
