// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis.CodeMetrics;
using Microsoft.CodeAnalysis.MSBuild;

namespace Metrics
{
    internal sealed class Program
    {
        public static int Main(string[] args)
        {
            using var tokenSource = new CancellationTokenSource();
            Console.CancelKeyPress += delegate
            {
                tokenSource.Cancel();
            };

            try
            {
                return (int)RunAsync(args, tokenSource.Token).GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Operation Cancelled.");
                return -1;
            }
        }

        private static async Task<ErrorCode> RunAsync(string[] args, CancellationToken cancellationToken)
        {
            var projectsOrSolutions = new List<string>();
            string outputFile = null;
            bool quiet = false;

            if (args.Length == 0)
            {
                return usage();
            }

            var errorCode = parseArguments();
            if (errorCode != ErrorCode.None)
            {
                return errorCode;
            }

            cancellationToken.ThrowIfCancellationRequested();
            MSBuildLocator.RegisterDefaults();

            cancellationToken.ThrowIfCancellationRequested();
            (ImmutableArray<(string, CodeAnalysisMetricData)> metricDatas, ErrorCode exitCode) = await GetMetricDatasAsync(projectsOrSolutions, quiet, cancellationToken).ConfigureAwait(false);
            if (exitCode != ErrorCode.None)
            {
                return exitCode;
            }

            cancellationToken.ThrowIfCancellationRequested();
            errorCode = writeOutput();
            if (!quiet && errorCode == ErrorCode.None)
            {
                Console.WriteLine("Completed Successfully.");
            }

            return errorCode;

            ErrorCode parseArguments()
            {
                // Parse arguments
                for (int i = 0; i < args.Length; i++)
                {
                    var arg = args[i];
                    if (!arg.StartsWith("/", StringComparison.Ordinal) && !arg.StartsWith("-", StringComparison.Ordinal))
                    {
                        return usage();
                    }

                    arg = arg.Substring(1);
                    switch (arg.ToUpperInvariant())
                    {
                        case "Q":
                        case "QUIET":
                            quiet = true;
                            continue;

                        case "?":
                        case "HELP":
                            return usage();

                        default:
                            var index = arg.IndexOf(':');
                            if (index == -1 || index == arg.Length - 1)
                            {
                                return usage();
                            }

                            var key = arg.Substring(0, index).ToUpperInvariant();
                            var value = arg.Substring(index + 1);
                            switch (key)
                            {
                                case "P":
                                case "PROJECT":
                                    if (!File.Exists(value))
                                    {
                                        return fileNotExists(value);
                                    }

                                    if (!value.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) &&
                                        !value.EndsWith(".vbproj", StringComparison.OrdinalIgnoreCase))
                                    {
                                        return notASupportedProject(value);
                                    }

                                    projectsOrSolutions.Add(value);
                                    break;

                                case "S":
                                case "SOLUTION":
                                    if (!File.Exists(value))
                                    {
                                        return fileNotExists(value);
                                    }

                                    if (!value.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
                                    {
                                        return notASolution(value);
                                    }

                                    projectsOrSolutions.Add(value);
                                    break;

                                case "O":
                                case "OUT":
                                    if (value.Length == 0)
                                    {
                                        return invalidOutputFile(value);
                                    }

                                    outputFile = value;
                                    break;

                                default:
                                    return usage();
                            }

                            break;
                    }
                }

                if (projectsOrSolutions.Count == 0)
                {
                    return requiresProjectOrSolution();
                }

                return ErrorCode.None;
            }

            static ErrorCode usage()
            {
                Console.WriteLine(@"
Usage: Metrics.exe <arguments>

Help for command-line arguments:

/project:<project-file>  [Short form: /p:<project-file>]
Project(s) to analyze.

/solution:<solution-file>  [Short form: /s:<solution-file>]
Solution(s) to analyze.

/out:<file>  [Short form: /o:<file>]
Metrics results XML output file.

/quiet  [Short form: /q]
Silence all console output other than error reporting.

/help  [Short form: /?]
Display this help message.");
                return ErrorCode.Usage;
            }

            static ErrorCode fileNotExists(string path)
            {
                Console.WriteLine($"Error: File '{path}' does not exist.");
                return ErrorCode.FileNotExists;
            }

            static ErrorCode requiresProjectOrSolution()
            {
                Console.WriteLine($"Error: No project or solution provided.");
                return ErrorCode.RequiresProjectOrSolution;
            }

            static ErrorCode notASolution(string path)
            {
                Console.WriteLine($"Error: File '{path}' is not a solution file.");
                return ErrorCode.NotASolution;
            }

            static ErrorCode notASupportedProject(string path)
            {
                Console.WriteLine($"Error: File '{path}' is not a C# or VB project file.");
                return ErrorCode.NotASupportedProject;
            }

            static ErrorCode invalidOutputFile(string path)
            {
                Console.WriteLine($"Error: File '{path}' is not a valid output file.");
                return ErrorCode.InvalidOutputFile;
            }

            ErrorCode writeOutput()
            {
                XmlTextWriter metricFile = null;
                try
                {
                    // Create the writer
                    if (outputFile != null)
                    {
                        if (!quiet)
                        {
                            Console.WriteLine($"Writing output to '{outputFile}'...");
                        }
                        metricFile = new XmlTextWriter(outputFile, Encoding.UTF8);
                    }
                    else
                    {
                        metricFile = new XmlTextWriter(Console.OpenStandardOutput(), Console.OutputEncoding);
                    }

                    MetricsOutputWriter.WriteMetricFile(metricDatas, metricFile);
                    if (outputFile == null)
                    {
                        metricFile.WriteString(Environment.NewLine + Environment.NewLine);
                    }

                    return ErrorCode.None;
                }
#pragma warning disable CA1031 // Do not catch general exception types - gracefully catch exceptions and log them to the console and output file.
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    return ErrorCode.WriteException;
                }
#pragma warning restore CA1031 // Do not catch general exception types
                finally
                {
                    if (metricFile != null)
                    {
                        metricFile.Close();
                    }
                }
            }
        }

        private static async Task<(ImmutableArray<(string, CodeAnalysisMetricData)>, ErrorCode)> GetMetricDatasAsync(List<string> projectsOrSolutions, bool quiet, CancellationToken cancellationToken)
        {
            var builder = ImmutableArray.CreateBuilder<(string, CodeAnalysisMetricData)>();

            try
            {
                using (var workspace = MSBuildWorkspace.Create())
                {
                    foreach (var projectOrSolution in projectsOrSolutions)
                    {
                        if (projectOrSolution.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
                        {
                            await computeSolutionMetricDataAsync(workspace, projectOrSolution, cancellationToken).ConfigureAwait(false);
                        }
                        else
                        {
                            Debug.Assert(projectOrSolution.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) ||
                                projectOrSolution.EndsWith(".vbproj", StringComparison.OrdinalIgnoreCase));
                            await computeProjectMetricDataAsync(workspace, projectOrSolution, cancellationToken).ConfigureAwait(false);
                        }
                    }
                }

                return (builder.ToImmutable(), ErrorCode.None);
            }
#pragma warning disable CA1031 // Do not catch general exception types - gracefully catch exceptions and log them to the console and output file.
            catch (Exception ex)
            {
                Console.Write(ex.Message);
                return (ImmutableArray<(string, CodeAnalysisMetricData)>.Empty, ErrorCode.ComputeException);
            }
#pragma warning restore CA1031 // Do not catch general exception types

            async Task computeProjectMetricDataAsync(MSBuildWorkspace workspace, string projectFile, CancellationToken cancellation)
            {
                cancellation.ThrowIfCancellationRequested();
                if (!quiet)
                {
                    Console.WriteLine($"Loading {Path.GetFileName(projectFile)}...");
                }

                var project = await workspace.OpenProjectAsync(projectFile, cancellationToken: CancellationToken.None).ConfigureAwait(false);

                if (!quiet)
                {
                    Console.WriteLine($"Computing code metrics for {Path.GetFileName(projectFile)}...");
                }

                if (!project.SupportsCompilation)
                {
                    throw new NotSupportedException("Project must support compilation.");
                }

                cancellation.ThrowIfCancellationRequested();
                var compilation = await project.GetCompilationAsync(CancellationToken.None).ConfigureAwait(false);
                var metricData = await CodeAnalysisMetricData.ComputeAsync(compilation.Assembly, compilation, CancellationToken.None).ConfigureAwait(false);
                builder.Add((projectFile, metricData));
            }

            async Task computeSolutionMetricDataAsync(MSBuildWorkspace workspace, string solutionFile, CancellationToken cancellation)
            {
                cancellation.ThrowIfCancellationRequested();
                if (!quiet)
                {
                    Console.WriteLine($"Loading {Path.GetFileName(solutionFile)}...");
                }

                var solution = await workspace.OpenSolutionAsync(solutionFile, cancellationToken: CancellationToken.None).ConfigureAwait(false);

                if (!quiet)
                {
                    Console.WriteLine($"Computing code metrics for {Path.GetFileName(solutionFile)}...");
                }

                foreach (var project in solution.Projects)
                {
                    if (!quiet)
                    {
                        Console.WriteLine($"    Computing code metrics for {Path.GetFileName(project.FilePath)}...");
                    }

                    if (!project.SupportsCompilation)
                    {
                        throw new NotSupportedException("Project must support compilation.");
                    }

                    cancellation.ThrowIfCancellationRequested();
                    var compilation = await project.GetCompilationAsync(CancellationToken.None).ConfigureAwait(false);
                    var metricData = await CodeAnalysisMetricData.ComputeAsync(compilation.Assembly, compilation, CancellationToken.None).ConfigureAwait(false);
                    builder.Add((project.FilePath, metricData));
                }
            }
        }

        private enum ErrorCode
        {
            None,
            Usage,
            FileNotExists,
            RequiresProjectOrSolution,
            NotASolution,
            NotASupportedProject,
            InvalidOutputFile,
            ComputeException,
            WriteException
        }
    }

}
