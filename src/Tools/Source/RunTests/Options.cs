// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
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
        public List<string> TargetFrameworks { get; set; } = new List<string>();

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

        public string? AccessToken { get; set; }

        public string? ProjectUri { get; set; }

        public string? PipelineDefinitionId { get; set; }

        public string? PhaseName { get; set; }

        public string? TargetBranchName { get; set; }

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
            var architecture = "x64";
            var includeHtml = false;
            var targetFrameworks = new List<string>();
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
            string? accessToken = null;
            string? projectUri = null;
            string? pipelineDefinitionId = null;
            string? phaseName = null;
            string? targetBranchName = null;
            var optionSet = new OptionSet()
            {
                { "dotnet=", "Path to dotnet", (string s) => dotnetFilePath = s },
                { "configuration=", "Configuration to test: Debug or Release", (string s) => configuration = s },
                { "tfm=", "Target framework to test", (string s) => targetFrameworks.Add(s) },
                { "include=", "Expression for including unit test dlls: default *.UnitTests.dll", (string s) => includeFilter.Add(s) },
                { "exclude=", "Expression for excluding unit test dlls: default is empty", (string s) => excludeFilter.Add(s) },
                { "arch=", "Architecture to test on: x86, x64 or arm64", (string s) => architecture = s },
                { "html", "Include HTML file output", o => includeHtml = o is object },
                { "sequential", "Run tests sequentially", o => sequential = o is object },
                { "helix", "Run tests on Helix", o => helix = o is object },
                { "helixQueueName=", "Name of the Helix queue to run tests on", (string s) => helixQueueName = s },
                { "helixApiAccessToken=", "Access token for internal helix queues", (string s) => helixApiAccessToken = s },
                { "testfilter=", "xUnit string to pass to --filter, e.g. FullyQualifiedName~TestClass1|Category=CategoryA", (string s) => testFilter = s },
                { "timeout=", "Minute timeout to limit the tests to", (int i) => timeout = i },
                { "out=", "Test result file directory (when running on Helix, this is relative to the Helix work item directory)", (string s) => resultFileDirectory = s },
                { "logs=", "Log file directory (when running on Helix, this is relative to the Helix work item directory)", (string s) => logFileDirectory = s },
                { "display=", "Display", (Display d) => display = d },
                { "artifactspath=", "Path to the artifacts directory", (string s) => artifactsPath = s },
                { "procdumppath=", "Path to procdump", (string s) => procDumpFilePath = s },
                { "collectdumps", "Whether or not to gather dumps on timeouts and crashes", o => collectDumps = o is object },
                { "accessToken=", "Pipeline access token with permissions to view test history", (string s) => accessToken = s },
                { "projectUri=", "ADO project containing the pipeline", (string s) => projectUri = s },
                { "pipelineDefinitionId=", "Pipeline definition id", (string s) => pipelineDefinitionId = s },
                { "phaseName=", "Pipeline phase name associated with this test run", (string s) => phaseName = s },
                { "targetBranchName=", "Target branch of this pipeline run", (string s) => targetBranchName = s },
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

            if (includeFilter.Count == 0)
            {
                includeFilter.Add(".*UnitTests.*");
            }

            if (targetFrameworks.Count == 0)
            {
                targetFrameworks.Add("net472");
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
                TargetFrameworks = targetFrameworks,
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
                AccessToken = accessToken,
                ProjectUri = projectUri,
                PipelineDefinitionId = pipelineDefinitionId,
                PhaseName = phaseName,
                TargetBranchName = targetBranchName,
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
