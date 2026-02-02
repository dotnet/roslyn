// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Rebuild;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Roslyn.Utilities;

namespace BuildValidator
{
    /// <summary>
    /// Build Validator enumerates the output of the Roslyn build, extracts the compilation options
    /// from the PE and attempts to rebuild the source using that information. It then checks
    /// that the new build output is the same as the original build
    /// </summary>
    internal class Program
    {
        internal const int ExitSuccess = 0;
        internal const int ExitFailure = 1;

        static int Main(string[] args)
        {
            System.Diagnostics.Trace.Listeners.Clear();

            var assembliesPath = new Option<string[]>("--assembliesPath")
            {
                Description = BuildValidatorResources.Path_to_assemblies_to_rebuild_can_be_specified_one_or_more_times,
                Required = true,
                Arity = ArgumentArity.OneOrMore,
            };
            var exclude = new Option<string[]>("--exclude")
            {
                Description = BuildValidatorResources.Assemblies_to_be_excluded_substring_match,
                Arity = ArgumentArity.ZeroOrMore,
            };
            var source = new Option<string>("--sourcePath")
            {
                Description = BuildValidatorResources.Path_to_sources_to_use_in_rebuild,
                Required = true,
            };
            var referencesPath = new Option<string[]>("--referencesPath")
            {
                Description = BuildValidatorResources.Path_to_referenced_assemblies_can_be_specified_zero_or_more_times,
                Arity = ArgumentArity.ZeroOrMore,
            };
            var verbose = new Option<bool>("--verbose")
            {
                Description = BuildValidatorResources.Output_verbose_log_information
            };
            var quiet = new Option<bool>("--quiet")
            {
                Description = BuildValidatorResources.Do_not_output_log_information_to_console
            };
            var debug = new Option<bool>("--debug")
            {
                Description = BuildValidatorResources.Output_debug_info_when_rebuild_is_not_equal_to_the_original
            };
            var debugPath = new Option<string?>("--debugPath")
            {
                Description = BuildValidatorResources.Path_to_output_debug_info
            };

            var rootCommand = new RootCommand
            {
                assembliesPath,
                exclude,
                source,
                referencesPath,
                verbose,
                quiet,
                debug,
                debugPath,
            };

            rootCommand.SetAction(parseResult =>
            {
                return HandleCommand(
                    assembliesPath: parseResult.GetValue(assembliesPath)!,
                    exclude: parseResult.GetValue(exclude),
                    sourcePath: parseResult.GetValue(source)!,
                    referencesPath: parseResult.GetValue(referencesPath),
                    verbose: parseResult.GetValue(verbose),
                    quiet: parseResult.GetValue(quiet),
                    debug: parseResult.GetValue(debug),
                    debugPath: parseResult.GetValue(debugPath));
            });

            return rootCommand.Parse(args).Invoke();
        }

        static int HandleCommand(string[] assembliesPath, string[]? exclude, string sourcePath, string[]? referencesPath, bool verbose, bool quiet, bool debug, string? debugPath)
        {
            // If user provided a debug path then assume we should write debug outputs.
            debug |= debugPath is object;
            debugPath ??= Path.Combine(Path.GetTempPath(), $"BuildValidator");
            referencesPath ??= Array.Empty<string>();

            var excludes = new List<string>(exclude ?? Array.Empty<string>());
            excludes.Add(Path.DirectorySeparatorChar + "runtimes" + Path.DirectorySeparatorChar);
            excludes.Add(@".resources.dll");

            var options = new Options(assembliesPath, referencesPath, excludes.ToArray(), sourcePath, verbose, quiet, debug, debugPath);

            // TODO: remove the DemoLoggerProvider or convert it to something more permanent
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel((options.Verbose, options.Quiet) switch
                {
                    (_, true) => LogLevel.Error,
                    (true, _) => LogLevel.Trace,
                    _ => LogLevel.Information
                });
                builder.AddProvider(new DemoLoggerProvider());
            });

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
                var artifactsDirs = options.AssembliesPaths.Select(path => new DirectoryInfo(path));
                using (logger.BeginScope("Rebuild Search Paths"))
                {
                    foreach (var artifactsDir in artifactsDirs)
                    {
                        logger.LogInformation($@"""{artifactsDir.FullName}""");
                    }
                }

                var assemblyInfos = GetAssemblyInfos(
                    options.AssembliesPaths,
                    options.Excludes,
                    logger);

                logAssemblyInfos();

                var success = ValidateFiles(assemblyInfos, options, loggerFactory);

                Console.Out.Flush();
                return success ? ExitSuccess : ExitFailure;

                void logAssemblyInfos()
                {
                    logger.LogInformation("Assemblies to be validated");
                    foreach (var assemblyInfo in assemblyInfos)
                    {
                        logger.LogInformation($"\t{assemblyInfo.FilePath} - {assemblyInfo.Mvid}");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                logger.LogError(ex, ex.StackTrace);
                throw;
            }
        }

        private static AssemblyInfo[] GetAssemblyInfos(
            IEnumerable<string> assemblySearchPaths,
            IEnumerable<string> excludes,
            ILogger logger)
        {
            var map = new Dictionary<Guid, AssemblyInfo>();
            foreach (var directory in assemblySearchPaths)
            {
                foreach (var filePath in getAssemblyPaths(directory))
                {
                    if (excludes.Any(x => filePath.IndexOf(x, FileNameEqualityComparer.StringComparison) >= 0))
                    {
                        logger.LogInformation($"Skipping excluded file {filePath}");
                        continue;
                    }

                    if (Util.GetPortableExecutableInfo(filePath) is not { } peInfo)
                    {
                        logger.LogInformation($"Skipping non-pe file {filePath}");
                        continue;
                    }

                    if (peInfo.IsReadyToRun)
                    {
                        logger.LogInformation($"Skipping ReadyToRun file {filePath}");
                        continue;
                    }

                    if (peInfo.IsReferenceAssembly)
                    {
                        logger.LogInformation($"Skipping reference assembly {filePath}");
                        continue;
                    }

                    if (map.TryGetValue(peInfo.Mvid, out var assemblyInfo))
                    {
                        // It's okay for the assembly to be duplicated in the search path.
                        logger.LogInformation("Duplicate assembly path have same MVID");
                        logger.LogInformation($"\t{filePath}");
                        logger.LogInformation($"\t{assemblyInfo.FilePath}");
                        continue;
                    }

                    map[peInfo.Mvid] = new AssemblyInfo(filePath, peInfo.Mvid);
                }
            }

            return map.Values.OrderBy(x => x.FileName, FileNameEqualityComparer.StringComparer).ToArray();

            static IEnumerable<string> getAssemblyPaths(string directory)
            {
                var exePaths = Directory.EnumerateFiles(directory, "*.exe", SearchOption.AllDirectories);
                var dllPaths = Directory.EnumerateFiles(directory, "*.dll", SearchOption.AllDirectories);
                return Enumerable.Concat(exePaths, dllPaths);
            }
        }

        private static bool ValidateFiles(IEnumerable<AssemblyInfo> assemblyInfos, Options options, ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger<Program>();
            var referenceResolver = LocalReferenceResolver.Create(options, loggerFactory);

            var assembliesCompiled = new List<CompilationDiff>();
            foreach (var assemblyInfo in assemblyInfos)
            {
                var compilationDiff = ValidateFile(options, assemblyInfo, logger, referenceResolver);
                assembliesCompiled.Add(compilationDiff);

                if (!compilationDiff.Succeeded)
                {
                    logger.LogError($"Validation failed for {assemblyInfo.FilePath}");
                    var debugPath = Path.Combine(
                        options.DebugPath,
                        assemblyInfo.TargetFramework,
                        Path.GetFileNameWithoutExtension(assemblyInfo.FileName));
                    logger.LogInformation($@"Writing diffs to ""{Path.GetFullPath(debugPath)}""");
                    compilationDiff.WriteArtifacts(debugPath, logger);
                }
            }

            bool success = true;

            using var summary = logger.BeginScope("Summary");
            using (logger.BeginScope("Successful rebuilds"))
            {
                foreach (var diff in assembliesCompiled.Where(a => a.Result == RebuildResult.Success))
                {
                    logger.LogInformation($"\t{diff.AssemblyInfo.FilePath}");
                }
            }

            using (logger.BeginScope("Rebuilds with output differences"))
            {
                foreach (var diff in assembliesCompiled.Where(a => a.Result == RebuildResult.BinaryDifference))
                {
                    logger.LogWarning($"\t{diff.AssemblyInfo.FilePath}");
                    success = false;
                }
            }

            using (logger.BeginScope("Rebuilds with compilation errors"))
            {
                foreach (var diff in assembliesCompiled.Where(a => a.Result == RebuildResult.CompilationError))
                {
                    logger.LogError($"\t{diff.AssemblyInfo.FilePath} had {diff.Diagnostics.Length} diagnostics.");
                    success = false;
                }
            }

            using (logger.BeginScope("Rebuilds with missing references"))
            {
                foreach (var diff in assembliesCompiled.Where(a => a.Result == RebuildResult.MissingReferences))
                {
                    logger.LogError($"\t{diff.AssemblyInfo.FilePath}");
                    success = false;
                }
            }

            using (logger.BeginScope("Rebuilds with other issues"))
            {
                foreach (var diff in assembliesCompiled.Where(a => a.Result == RebuildResult.MiscError))
                {
                    logger.LogError($"{diff.AssemblyInfo.FilePath} {diff.MiscErrorMessage}");
                    success = false;
                }
            }

            return success;
        }

        private static CompilationDiff ValidateFile(
            Options options,
            AssemblyInfo assemblyInfo,
            ILogger logger,
            LocalReferenceResolver referenceResolver)
        {
            // Find the embedded pdb
            using var originalPeReader = new PEReader(File.OpenRead(assemblyInfo.FilePath));
            var originalBinary = new FileInfo(assemblyInfo.FilePath);

            var pdbOpened = originalPeReader.TryOpenAssociatedPortablePdb(
                peImagePath: assemblyInfo.FilePath,
                filePath => File.Exists(filePath) ? new MemoryStream(File.ReadAllBytes(filePath)) : null,
                out var pdbReaderProvider,
                out var pdbPath);

            if (!pdbOpened || pdbReaderProvider is null)
            {
                logger.LogError($"Could not find pdb for {originalBinary.FullName}");
                return CompilationDiff.CreateMiscError(assemblyInfo, "Could not find pdb");
            }

            using var _ = logger.BeginScope($"Verifying {originalBinary.FullName} with pdb {pdbPath ?? "[embedded]"}");

            var pdbReader = pdbReaderProvider.GetMetadataReader();
            var optionsReader = new CompilationOptionsReader(logger, pdbReader, originalPeReader);
            if (!optionsReader.HasMetadataCompilationOptions)
            {
                return CompilationDiff.CreateMiscError(assemblyInfo, "Missing metadata compilation options");
            }

            var sourceLinks = ResolveSourceLinks(optionsReader, logger);
            var sourceResolver = new LocalSourceResolver(options, sourceLinks, logger);
            var artifactResolver = new RebuildArtifactResolver(sourceResolver, referenceResolver);

            CompilationFactory compilationFactory;
            try
            {
                compilationFactory = CompilationFactory.Create(
                    originalBinary.Name,
                    optionsReader);

                return CompilationDiff.Create(
                    assemblyInfo,
                    compilationFactory,
                    artifactResolver,
                    logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                logger.LogError(ex.StackTrace);
                return CompilationDiff.CreateMiscError(assemblyInfo, ex.Message);
            }
        }

        private static ImmutableArray<SourceLinkEntry> ResolveSourceLinks(CompilationOptionsReader compilationOptionsReader, ILogger logger)
        {
            using var _ = logger.BeginScope("Source Links");

            var sourceLinkUtf8 = compilationOptionsReader.GetSourceLinkUtf8();
            if (sourceLinkUtf8 is null)
            {
                logger.LogInformation("No source link cdi found in pdb");
                return ImmutableArray<SourceLinkEntry>.Empty;
            }

            var documents = JsonConvert.DeserializeAnonymousType(Encoding.UTF8.GetString(sourceLinkUtf8), new { documents = (Dictionary<string, string>?)null })?.documents
                ?? throw new InvalidOperationException("Failed to deserialize source links.");

            var sourceLinks = documents.SelectAsArray(makeSourceLink);

            if (sourceLinks.IsDefault)
            {
                logger.LogInformation("Empty source link cdi found in pdb");
                sourceLinks = ImmutableArray<SourceLinkEntry>.Empty;
            }
            else
            {
                foreach (var link in sourceLinks)
                {
                    logger.LogInformation($@"""{link.Prefix}"": ""{link.Replace}""");
                }
            }
            return sourceLinks;

            static SourceLinkEntry makeSourceLink(KeyValuePair<string, string> entry)
            {
                // TODO: determine if this subsitution is correct
                var (key, value) = (entry.Key, entry.Value); // TODO: use Deconstruct in .NET Core
                var prefix = key.Remove(key.LastIndexOf("*"));
                var replace = value.Remove(value.LastIndexOf("*"));
                return new SourceLinkEntry(prefix, replace);
            }
        }
    }
}
