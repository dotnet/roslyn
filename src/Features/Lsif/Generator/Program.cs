// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis.Lsif.Generator.Writing;
using Microsoft.CodeAnalysis.MSBuild;

namespace Microsoft.CodeAnalysis.Lsif.Generator
{
    internal static class Program
    {
        public static Task Main(string[] args)
        {
            var generateCommand = new RootCommand("generates an LSIF file")
            {
                new Option("--solution", "input solution file") { Argument = new Argument<FileInfo>().ExistingOnly(), Required = true },
                new Option("--output", "file to write the LSIF output to, instead of the console") { Argument = new Argument<string?>(defaultValue: () => null).LegalFilePathsOnly() },
                new Option("--log", "file to write a log to") { Argument = new Argument<string?>(defaultValue: () => null).LegalFilePathsOnly() }
            };

            generateCommand.Handler = CommandHandler.Create((Func<FileInfo, string?, string?, Task>)GenerateAsync);

            return generateCommand.InvokeAsync(args);
        }

        private static async Task GenerateAsync(FileInfo solution, string? output, string? log)
        {
            // If we have an output file, we'll write to that, else we'll use Console.Out
            using StreamWriter? outputFile = output != null ? new StreamWriter(output) : null;
            TextWriter outputWriter = outputFile ?? Console.Out;

            using TextWriter logFile = log != null ? new StreamWriter(log) : TextWriter.Null;

            try
            {
                await GenerateAsync(solution, outputWriter, logFile);
            }
            catch (Exception e)
            {
                // If it failed, write out to the logs and error, but propagate the error too
                var message = "Unhandled exception: " + e.ToString();
                await logFile.WriteLineAsync(message);
                Console.Error.WriteLine(message);
                throw;
            }

            await logFile.WriteLineAsync("Generation complete.");
        }

        private static async Task GenerateAsync(FileInfo solutionFile, TextWriter outputWriter, TextWriter logFile)
        {
            await logFile.WriteLineAsync($"Loading {solutionFile.FullName}...");

            var solutionLoadStopwatch = Stopwatch.StartNew();

            MSBuildLocator.RegisterDefaults();
            var msbuildWorkspace = MSBuildWorkspace.Create();
            var solution = await msbuildWorkspace.OpenSolutionAsync(solutionFile.FullName);

            await logFile.WriteLineAsync($"Load of the solution completed in {solutionLoadStopwatch.Elapsed.ToDisplayString()}.");

            using var lsifWriter = new TextLsifJsonWriter(outputWriter);
            var lsifGenerator = new Generator(lsifWriter);

            Stopwatch totalTimeInGenerationAndCompilationFetchStopwatch = Stopwatch.StartNew();
            TimeSpan totalTimeInGenerationPhase = TimeSpan.Zero;

            foreach (var project in solution.Projects)
            {
                if (project.SupportsCompilation && project.FilePath != null)
                {
                    var compilationCreationStopwatch = Stopwatch.StartNew();
                    var compilation = (await project.GetCompilationAsync())!;

                    await logFile.WriteLineAsync($"Fetch of compilation for {project.FilePath} completed in {compilationCreationStopwatch.Elapsed.ToDisplayString()}.");

                    var generationForProjectStopwatch = Stopwatch.StartNew();
                    await lsifGenerator.GenerateForCompilation(compilation, project.FilePath, project.LanguageServices);
                    generationForProjectStopwatch.Stop();

                    totalTimeInGenerationPhase += generationForProjectStopwatch.Elapsed;

                    await logFile.WriteLineAsync($"Generation for {project.FilePath} completed in {generationForProjectStopwatch.Elapsed.ToDisplayString()}.");
                }
            }

            await logFile.WriteLineAsync($"Total time spent in the generation phase for all projects, excluding compilation fetch time: {totalTimeInGenerationPhase.ToDisplayString()}");
            await logFile.WriteLineAsync($"Total time spent in the generation phase for all projects, including compilation fetch time: {totalTimeInGenerationAndCompilationFetchStopwatch.Elapsed.ToDisplayString()}");
        }
    }
}
