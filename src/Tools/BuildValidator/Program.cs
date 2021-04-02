// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
using Newtonsoft.Json;

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
            var rootCommand = new RootCommand
            {
                new Option<string>(
                    "--assembliesPath", "Path to assemblies to rebuild (can be specified one or more times)"
                ) { IsRequired = true, Argument = { Arity = ArgumentArity.OneOrMore } },
                new Option<string>(
                    "--exclude", "Assemblies to be excluded (substring match)"
                ) { Argument = { Arity = ArgumentArity.ZeroOrMore } },
                new Option<string>(
                    "--sourcePath", "Path to sources to use in rebuild"
                ) { IsRequired = true },
                new Option<string>(
                    "--referencesPath", "Path to referenced assemblies (can be specified zero or more times)"
                ) { Argument = { Arity = ArgumentArity.ZeroOrMore } },
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
            rootCommand.Handler = CommandHandler.Create(new Func<string[], string[]?, string, string[]?, bool, bool, bool, string, int>(HandleCommand));
            return rootCommand.Invoke(args);
        }

        static int HandleCommand(string[] assembliesPath, string[]? exclude, string sourcePath, string[]? referencesPath, bool verbose, bool quiet, bool debug, string? debugPath)
        {
            // If user provided a debug path then assume we should write debug outputs.
            debug |= debugPath is object;
            debugPath ??= Path.Combine(Path.GetTempPath(), $"BuildValidator");
            referencesPath ??= Array.Empty<string>();

            var excludes = new List<string>(exclude ?? Array.Empty<string>());
            excludes.Add(Path.DirectorySeparatorChar + "runtimes" + Path.DirectorySeparatorChar);
            excludes.Add(Path.DirectorySeparatorChar + "ref" + Path.DirectorySeparatorChar);
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
                        logger.LogError($"Skipping non-pe file {filePath}");
                        continue;
                    }

                    if (peInfo.IsReadyToRun)
                    {
                        logger.LogError($"Skipping ReadyToRun file {filePath}");
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

            var sourceResolver = new LocalSourceResolver(options, loggerFactory);
            var referenceResolver = new LocalReferenceResolver(options, loggerFactory);

            var assembliesCompiled = new List<CompilationDiff>();
            foreach (var assemblyInfo in assemblyInfos)
            {
                var compilationDiff = ValidateFile(assemblyInfo, logger, sourceResolver, referenceResolver);
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
            AssemblyInfo assemblyInfo,
            ILogger logger,
            LocalSourceResolver sourceResolver,
            LocalReferenceResolver referenceResolver)
        {
            MetadataReaderProvider? pdbReaderProvider = null;

            try
            {
                // Find the embedded pdb
                using var originalBinaryStream = File.OpenRead(assemblyInfo.FilePath);
                using var originalPeReader = new PEReader(originalBinaryStream);
                var originalBinary = new FileInfo(assemblyInfo.FilePath);

                var pdbOpened = originalPeReader.TryOpenAssociatedPortablePdb(
                    peImagePath: assemblyInfo.FilePath,
                    filePath => File.Exists(filePath) ? File.OpenRead(filePath) : null,
                    out pdbReaderProvider,
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

                var encoding = optionsReader.GetEncoding();
                var metadataReferenceInfos = optionsReader.GetMetadataReferences();
                var sourceFileInfos = optionsReader.GetSourceFileInfos(encoding);

                logger.LogInformation("Locating metadata references");
                if (!referenceResolver.TryResolveReferences(metadataReferenceInfos, out var metadataReferences))
                {
                    logger.LogError($"Failed to rebuild {originalBinary.Name} due to missing metadata references");
                    return CompilationDiff.CreateMissingReferences(assemblyInfo, referenceResolver, metadataReferenceInfos);
                }
                logResolvedMetadataReferences();

                var sourceLinks = ResolveSourceLinks(optionsReader, logger);
                if (sourceResolver.ResolveSources(sourceFileInfos, sourceLinks, encoding) is not { } sources)
                {
                    logger.LogError($"Failed to resolve sources");
                    return CompilationDiff.CreateMiscError(assemblyInfo, "Failed to resolve sources");
                }
                logResolvedSources();

                CompilationFactory compilationFactory;
                try
                {
                    compilationFactory = CompilationFactory.Create(
                        originalBinary.Name,
                        optionsReader);
                }
                catch (Exception ex)
                {
                    return CompilationDiff.CreateMiscError(assemblyInfo, ex.Message);
                }

                return CompilationDiff.Create(
                    assemblyInfo,
                    compilationFactory,
                    sources.SelectAsArray(x => compilationFactory.CreateSyntaxTree(x.SourceFileInfo.SourceFilePath, x.SourceText)),
                    metadataReferences,
                    logger);

                void logResolvedMetadataReferences()
                {
                    using var _ = logger.BeginScope("Metadata References");
                    for (var i = 0; i < metadataReferenceInfos.Length; i++)
                    {
                        logger.LogInformation($@"""{metadataReferences[i].Display}"" - {metadataReferenceInfos[i].Mvid}");
                    }
                }

                void logResolvedSources()
                {
                    using var _ = logger.BeginScope("Source Names");
                    foreach (var resolvedSource in sources)
                    {
                        var sourceFileInfo = resolvedSource.SourceFileInfo;
                        var hash = BitConverter.ToString(sourceFileInfo.Hash).Replace("-", "");
                        var embeddedCompressedHash = sourceFileInfo.EmbeddedCompressedHash is { } compressedHash
                            ? ("[uncompressed]" + BitConverter.ToString(compressedHash).Replace("-", ""))
                            : null;
                        logger.LogInformation($@"""{resolvedSource.DisplayPath}"" - {sourceFileInfo.HashAlgorithm} - {hash} - {embeddedCompressedHash}");
                    }
                }
            }
            finally
            {
                pdbReaderProvider?.Dispose();
            }
        }

        private static ImmutableArray<SourceLink> ResolveSourceLinks(CompilationOptionsReader compilationOptionsReader, ILogger logger)
        {
            using var _ = logger.BeginScope("Source Links");

            var sourceLinkUTF8 = compilationOptionsReader.GetSourceLinkUTF8();
            if (sourceLinkUTF8 is null)
            {
                return default;
            }

            var parseResult = JsonConvert.DeserializeAnonymousType(Encoding.UTF8.GetString(sourceLinkUTF8), new { documents = (Dictionary<string, string>?)null });
            var sourceLinks = parseResult.documents.Select(makeSourceLink).ToImmutableArray();

            if (sourceLinks.IsDefault)
            {
                logger.LogInformation("No source links found in pdb");
                sourceLinks = ImmutableArray<SourceLink>.Empty;
            }
            else
            {
                foreach (var link in sourceLinks)
                {
                    logger.LogInformation($@"""{link.Prefix}"": ""{link.Replace}""");
                }
            }
            return sourceLinks;

            static SourceLink makeSourceLink(KeyValuePair<string, string> entry)
            {
                // TODO: determine if this subsitution is correct
                var (key, value) = (entry.Key, entry.Value); // TODO: use Deconstruct in .NET Core
                var prefix = key.Remove(key.LastIndexOf("*"));
                var replace = value.Remove(value.LastIndexOf("*"));
                return new SourceLink(prefix, replace);
            }
        }
    }
}
