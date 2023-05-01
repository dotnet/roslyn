// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.Writing;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;
using CompilerInvocationsReader = Microsoft.Build.Logging.StructuredLogger.CompilerInvocationsReader;

namespace Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator
{
    internal static class Program
    {
        public static Task Main(string[] args)
        {
            var generateCommand = new RootCommand("generates an LSIF file")
            {
                new Option("--solution", "input solution file") { Argument = new Argument<FileInfo>().ExistingOnly() },
                new Option("--project", "input project file") { Argument = new Argument<FileInfo>().ExistingOnly() },
                new Option("--compiler-invocation", "path to a .json file that contains the information for a csc/vbc invocation") { Argument = new Argument<FileInfo>().ExistingOnly() },
                new Option("--binlog", "path to a MSBuild binlog that csc/vbc invocations will be extracted from") { Argument = new Argument<FileInfo>().ExistingOnly() },
                new Option("--output", "file to write the LSIF output to, instead of the console") { Argument = new Argument<string?>(defaultValue: () => null).LegalFilePathsOnly() },
                new Option("--output-format", "format of LSIF output") { Argument = new Argument<LsifFormat>(defaultValue: () => LsifFormat.Line) },
                new Option("--log", "file to write a log to") { Argument = new Argument<string?>(defaultValue: () => null).LegalFilePathsOnly() }
            };

            generateCommand.Handler = CommandHandler.Create((Func<FileInfo?, FileInfo?, FileInfo?, FileInfo?, string?, LsifFormat, string?, Task>)GenerateAsync);

            return generateCommand.InvokeAsync(args);
        }

        private static async Task GenerateAsync(
            FileInfo? solution,
            FileInfo? project,
            FileInfo? compilerInvocation,
            FileInfo? binLog,
            string? output,
            LsifFormat outputFormat,
            string? log)
        {
            // If we have an output file, we'll write to that, else we'll use Console.Out
            using var outputFile = output != null ? new StreamWriter(output, append: false, Encoding.UTF8) : null;
            TextWriter outputWriter;

            if (outputFile is null)
            {
                Console.OutputEncoding = Encoding.UTF8;
                outputWriter = Console.Out;
            }
            else
            {
                outputWriter = outputFile;
            }

            using var logFile = log != null ? new StreamWriter(log) : TextWriter.Null;
            ILsifJsonWriter lsifWriter = outputFormat switch
            {
                LsifFormat.Json => new JsonModeLsifJsonWriter(outputWriter),
                LsifFormat.Line => new LineModeLsifJsonWriter(outputWriter),
                _ => throw new NotImplementedException()
            };

            var cancellationToken = CancellationToken.None;

            try
            {
                // Exactly one of "solution", or "project" or "compilerInvocation" should be specified
                var fileInputs = new[] { solution, project, compilerInvocation, binLog };
                var nonNullFileInputs = fileInputs.Count(p => p is not null);

                if (nonNullFileInputs != 1)
                {
                    throw new Exception("Exactly one of either a solution path, project path or a compiler invocation path should be supplied.");
                }

                if (solution != null)
                {
                    await LocateAndRegisterMSBuild(logFile, solution.Directory);
                    await GenerateFromSolutionAsync(solution, lsifWriter, logFile, cancellationToken);
                }
                else if (project != null)
                {
                    await LocateAndRegisterMSBuild(logFile, project.Directory);
                    await GenerateFromProjectAsync(project, lsifWriter, logFile, cancellationToken);
                }
                else if (compilerInvocation != null)
                {
                    await GenerateFromCompilerInvocationAsync(compilerInvocation, lsifWriter, logFile, cancellationToken);
                }
                else
                {
                    Contract.ThrowIfNull(binLog);

                    // If we're loading a binlog, we don't need to discover an MSBuild that matches the SDK or source that we're processing, since we're not running
                    // any MSBuild builds or tasks/targets in our process. Since we're reading a binlog, simply none of the SDK will be loaded. We might load analyzers
                    // or source generators from the SDK or user-built, but those must generally target netstandard2.0 so we don't really expect them to have problems loading
                    // on one version of the runtime versus another.
                    await LocateAndRegisterMSBuild(logFile, sourceDirectory: null);
                    await GenerateFromBinaryLogAsync(binLog, lsifWriter, logFile, cancellationToken);
                }
            }
            catch (Exception e)
            {
                // If it failed, write out to the logs and error, but propagate the error too
                var message = "Unhandled exception: " + e.ToString();
                await logFile.WriteLineAsync(message);
                Console.Error.WriteLine(message);
                throw;
            }

            (lsifWriter as IDisposable)?.Dispose();
            await logFile.WriteLineAsync("Generation complete.");
        }

        private static async Task LocateAndRegisterMSBuild(TextWriter logFile, DirectoryInfo? sourceDirectory)
        {
            // Make sure we pick the highest version
            var options = VisualStudioInstanceQueryOptions.Default;

            if (sourceDirectory != null)
                options.WorkingDirectory = sourceDirectory.FullName;

            var msBuildInstance = MSBuildLocator.QueryVisualStudioInstances(options).OrderByDescending(i => i.Version).FirstOrDefault();
            if (msBuildInstance == null)
            {
                throw new Exception($"No MSBuild instances could be found; discovery types being used: {options.DiscoveryTypes}.");
            }
            else
            {
                await logFile.WriteLineAsync($"Using the MSBuild instance located at {msBuildInstance.MSBuildPath}.");
            }

            MSBuildLocator.RegisterInstance(msBuildInstance);
        }

        private static async Task GenerateFromProjectAsync(
            FileInfo projectFile, ILsifJsonWriter lsifWriter, TextWriter logFile, CancellationToken cancellationToken)
        {
            await GenerateWithMSBuildWorkspaceAsync(
                projectFile, lsifWriter, logFile,
                async (workspace, cancellationToken) =>
                {
                    var project = await workspace.OpenProjectAsync(projectFile.FullName, cancellationToken: cancellationToken);
                    return project.Solution;
                },
                cancellationToken);
        }

        private static async Task GenerateFromSolutionAsync(
            FileInfo solutionFile, ILsifJsonWriter lsifWriter, TextWriter logFile, CancellationToken cancellationToken)
        {
            await GenerateWithMSBuildWorkspaceAsync(
                solutionFile, lsifWriter, logFile,
                (workspace, cancellationToken) => workspace.OpenSolutionAsync(solutionFile.FullName, cancellationToken: cancellationToken),
                cancellationToken);
        }

        // This method can't be loaded until we've registered MSBuild with MSBuildLocator, as otherwise
        // we load ILogger prematurely which breaks MSBuildLocator.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task GenerateWithMSBuildWorkspaceAsync(
            FileInfo solutionOrProjectFile,
            ILsifJsonWriter lsifWriter,
            TextWriter logFile,
            Func<MSBuildWorkspace, CancellationToken, Task<Solution>> openAsync,
            CancellationToken cancellationToken)
        {
            await logFile.WriteLineAsync($"Loading {solutionOrProjectFile.FullName}...");

            var solutionLoadStopwatch = Stopwatch.StartNew();

            var msbuildWorkspace = MSBuildWorkspace.Create(await Composition.CreateHostServicesAsync());
            msbuildWorkspace.WorkspaceFailed += (s, e) => logFile.WriteLine("Error while loading: " + e.Diagnostic.Message);

            var solution = await openAsync(msbuildWorkspace, cancellationToken);

            var options = GeneratorOptions.Default;

            await logFile.WriteLineAsync($"Load completed in {solutionLoadStopwatch.Elapsed.ToDisplayString()}.");
            var lsifGenerator = Generator.CreateAndWriteCapabilitiesVertex(lsifWriter, logFile);

            var totalTimeInGenerationAndCompilationFetchStopwatch = Stopwatch.StartNew();
            var totalTimeInGenerationPhase = TimeSpan.Zero;

            foreach (var project in solution.Projects)
            {
                if (project.SupportsCompilation && project.FilePath != null)
                {
                    var compilationCreationStopwatch = Stopwatch.StartNew();
                    var compilation = await project.GetRequiredCompilationAsync(cancellationToken);

                    await logFile.WriteLineAsync($"Fetch of compilation for {project.FilePath} completed in {compilationCreationStopwatch.Elapsed.ToDisplayString()}.");

                    var generationForProjectStopwatch = Stopwatch.StartNew();
                    await lsifGenerator.GenerateForProjectAsync(project, options, cancellationToken);
                    generationForProjectStopwatch.Stop();

                    totalTimeInGenerationPhase += generationForProjectStopwatch.Elapsed;

                    await logFile.WriteLineAsync($"Generation for {project.FilePath} completed in {generationForProjectStopwatch.Elapsed.ToDisplayString()}.");
                }
            }

            await logFile.WriteLineAsync($"Total time spent in the generation phase for all projects, excluding compilation fetch time: {totalTimeInGenerationPhase.ToDisplayString()}");
            await logFile.WriteLineAsync($"Total time spent in the generation phase for all projects, including compilation fetch time: {totalTimeInGenerationAndCompilationFetchStopwatch.Elapsed.ToDisplayString()}");
        }

        private static async Task GenerateFromCompilerInvocationAsync(
            FileInfo compilerInvocationFile, ILsifJsonWriter lsifWriter, TextWriter logFile, CancellationToken cancellationToken)
        {
            await logFile.WriteLineAsync($"Processing compiler invocation from {compilerInvocationFile.FullName}...");

            var compilerInvocationLoadStopwatch = Stopwatch.StartNew();
            var project = await CompilerInvocation.CreateFromJsonAsync(File.ReadAllText(compilerInvocationFile.FullName));
            await logFile.WriteLineAsync($"Load of the project completed in {compilerInvocationLoadStopwatch.Elapsed.ToDisplayString()}.");

            var generationStopwatch = Stopwatch.StartNew();
            var lsifGenerator = Generator.CreateAndWriteCapabilitiesVertex(lsifWriter, logFile);

            await lsifGenerator.GenerateForProjectAsync(project, GeneratorOptions.Default, cancellationToken);
            await logFile.WriteLineAsync($"Generation for {project.FilePath} completed in {generationStopwatch.Elapsed.ToDisplayString()}.");
        }

        // This method can't be loaded until we've registered MSBuild with MSBuildLocator, as otherwise we might load a type prematurely.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task GenerateFromBinaryLogAsync(
            FileInfo binLog, ILsifJsonWriter lsifWriter, TextWriter logFile, CancellationToken cancellationToken)
        {
            await logFile.WriteLineAsync($"Reading binlog {binLog.FullName}...");
            var msbuildInvocations = CompilerInvocationsReader.ReadInvocations(binLog.FullName).ToImmutableArray();

            await logFile.WriteLineAsync($"Load of the binlog complete; {msbuildInvocations.Length} invocations were found.");

            var lsifGenerator = Generator.CreateAndWriteCapabilitiesVertex(lsifWriter, logFile);

            foreach (var msbuildInvocation in msbuildInvocations)
            {
                // Convert from the MSBuild "CompilerInvocation" type to our type that we use for our JSON-input mode already.
                var invocationInfo = new CompilerInvocation.CompilerInvocationInfo
                {
                    Arguments = msbuildInvocation.CommandLineArguments,
                    ProjectFilePath = msbuildInvocation.ProjectFilePath,
                    Tool = msbuildInvocation.Language == Microsoft.Build.Logging.StructuredLogger.CompilerInvocation.CSharp ? "csc" : "vbc"
                };

                var project = await CompilerInvocation.CreateFromInvocationInfoAsync(invocationInfo);

                var generationStopwatch = Stopwatch.StartNew();
                await lsifGenerator.GenerateForProjectAsync(project, GeneratorOptions.Default, cancellationToken);
                await logFile.WriteLineAsync($"Generation for {project.FilePath} completed in {generationStopwatch.Elapsed.ToDisplayString()}.");
            }
        }
    }
}
