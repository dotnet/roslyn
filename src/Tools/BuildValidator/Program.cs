// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace BuildValidator
{
    /// <summary>
    /// Build Validator enumerates the output of the Roslyn build, extracts the compilation options
    /// from the PE and attempts to rebuild the source using that information. It then checks
    /// that the new build output is the same as the original build
    /// </summary>
    class Program
    {
        const int ExitSuccess = 0;
        const int ExitFailure = 1;

        private static readonly Regex[] s_ignorePatterns = new Regex[]
        {
            new Regex(@"\\runtimes?\\"),
            new Regex(@"\\ref\\"),
            new Regex(@"\.resources?\.")
        };

        static int Main(string[] args)
        {
            var rootCommand = new RootCommand
            {
                new Option<string>(
                    "--assembliesPath", "Path to assemblies to rebuild"
                ) { IsRequired = true },
                new Option<string>(
                    "--sourcePath", "Path to sources to use in rebuild"
                ) { IsRequired = true },
                new Option<string[]?>(
                    "--referencesPaths", "Additional paths to referenced assemblies"
                ),
                new Option<bool>(
                    "--verbose", "Output verbose log information"
                ),
                new Option<bool>(
                    "--quiet", "Do not output log information to console"
                ),
                new Option<bool>(
                    "--debug", "Output debug info when rebuild is not equal to the original"
                ),
                new Option<string?>(
                    "--debugPath", "Path to output debug info. Defaults to the user temp directory. Note that a unique debug path should be specified for every instance of the tool running with `--debug` enabled."
                )
            };
            rootCommand.Handler = CommandHandler.Create<string, string, string[]?, bool, bool, bool, string>(HandleCommand);
            return rootCommand.Invoke(args);
        }

        static int HandleCommand(string assembliesPath, string sourcePath, string[]? referencesPaths, bool verbose, bool quiet, bool debug, string? debugPath)
        {
            // If user provided a debug path then assume we should write debug outputs.
            debug |= debugPath is object;
            debugPath ??= Path.Combine(Path.GetTempPath(), $"BuildValidator");
            referencesPaths ??= Array.Empty<string>();

            var options = new Options(assembliesPath, referencesPaths, sourcePath, verbose, quiet, debug, debugPath);

            // TODO: remove the DemoLoggerProvider, update this dependency,
            // and move to the built in logger.
            var loggerFactory = new LoggerFactory(
                new[] { new ConsoleLoggerProvider(new ConsoleLoggerSettings()) },
                new LoggerFilterOptions()
                {
                    MinLevel = options.Verbose ? LogLevel.Trace : LogLevel.Information
                });

            if (!options.Quiet)
            {
                loggerFactory.AddProvider(new DemoLoggerProvider());
            }

            var logger = loggerFactory.CreateLogger<Program>();
            try
            {
                var fullDebugPath = Path.GetFullPath(debugPath);
                logger.LogInformation($@"Using debug folder: ""{fullDebugPath}""");
                Directory.Delete(debugPath, recursive: true);
                logger.LogInformation($@"Cleaned debug folder: ""{fullDebugPath}""");
            }
            catch (IOException)
            {
                // no-op
            }

            try
            {
                var sourceResolver = new LocalSourceResolver(options, loggerFactory);
                var referenceResolver = new LocalReferenceResolver(options, loggerFactory);

                var buildConstructor = new BuildConstructor(referenceResolver, sourceResolver, logger);

                var artifactsDir = new DirectoryInfo(options.AssembliesPath);

                var filesToValidate = artifactsDir.EnumerateFiles("*.exe", SearchOption.AllDirectories)
                    .Concat(artifactsDir.EnumerateFiles("*.dll", SearchOption.AllDirectories))
                    .Distinct(FileNameEqualityComparer.Instance);

                var success = ValidateFiles(filesToValidate, buildConstructor, logger, options);
                Console.Out.Flush();
                return success ? ExitSuccess : ExitFailure;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                throw;
            }
        }

        // TODO: it feels like "logger" and "options" should be instance variables of something
        private static bool ValidateFiles(IEnumerable<FileInfo> originalBinaries, BuildConstructor buildConstructor, ILogger logger, Options options)
        {
            var assembliesCompiled = new List<CompilationDiff>();
            foreach (var file in originalBinaries)
            {
                var compilationDiff = ValidateFile(file, buildConstructor, logger, options);

                if (compilationDiff is null)
                {
                    logger.LogInformation($"Ignoring {file.FullName}");
                    continue;
                }

                assembliesCompiled.Add(compilationDiff);
            }

            bool success = true;

            using var summary = logger.BeginScope("Summary");
            using (logger.BeginScope("Successful rebuilds"))
            {
                foreach (var diff in assembliesCompiled.Where(a => a.AreEqual == true))
                {
                    logger.LogInformation($"\t{diff.OriginalPath}");
                }
            }

            using (logger.BeginScope("Rebuilds with output differences"))
            {
                foreach (var diff in assembliesCompiled.Where(a => a.AreEqual == false))
                {
                    // TODO: can we include the path to any diff artifacts?
                    logger.LogWarning($"\t{diff.OriginalPath}");
                    success = false;
                }
            }

            using (logger.BeginScope("Rebuilds with compilation errors"))
            {
                foreach (var diff in assembliesCompiled.Where(a => a.AreEqual == null))
                {
                    logger.LogError($"{diff.OriginalPath} had {diff.Diagnostics.Length} diagnostics.");
                    success = false;
                }
            }

            return success;
        }

        private static CompilationDiff? ValidateFile(FileInfo originalBinary, BuildConstructor buildConstructor, ILogger logger, Options options)
        {
            if (s_ignorePatterns.Any(r => r.IsMatch(originalBinary.FullName)))
            {
                logger.LogTrace($"Ignoring {originalBinary.FullName}");
                return null;
            }

            MetadataReaderProvider? pdbReaderProvider = null;

            try
            {
                // Find the embedded pdb
                using var originalBinaryStream = originalBinary.OpenRead();
                using var originalPeReader = new PEReader(originalBinaryStream);

                var pdbOpened = originalPeReader.TryOpenAssociatedPortablePdb(
                    peImagePath: originalBinary.FullName,
                    filePath => File.Exists(filePath) ? File.OpenRead(filePath) : null,
                    out pdbReaderProvider,
                    out var pdbPath);

                if (!pdbOpened || pdbReaderProvider is null)
                {
                    logger.LogError($"Could not find pdb for {originalBinary.FullName}");
                    return null;
                }

                using var _ = logger.BeginScope($"Verifying {originalBinary.FullName} with pdb {pdbPath ?? "[embedded]"}");

                var pdbReader = pdbReaderProvider.GetMetadataReader();
                var optionsReader = new CompilationOptionsReader(logger, pdbReader, originalPeReader);

                var compilation = buildConstructor.CreateCompilation(
                    optionsReader,
                    Path.GetFileNameWithoutExtension(originalBinary.Name));

                var compilationDiff = CompilationDiff.Create(originalBinary, optionsReader, compilation, getDebugEntryPoint(), logger, options);
                return compilationDiff;

                IMethodSymbol? getDebugEntryPoint()
                {
                    if (optionsReader.GetMainTypeName() is { } mainTypeName &&
                        optionsReader.GetMainMethodName() is { } mainMethodName)
                    {
                        var typeSymbol = compilation.GetTypeByMetadataName(mainTypeName);
                        if (typeSymbol is object)
                        {
                            var methodSymbols = typeSymbol
                                .GetMembers()
                                .OfType<IMethodSymbol>()
                                .Where(x => x.Name == mainMethodName);
                            return methodSymbols.FirstOrDefault();
                        }
                    }

                    return null;
                }
            }
            finally
            {
                pdbReaderProvider?.Dispose();
            }
        }
    }
}
