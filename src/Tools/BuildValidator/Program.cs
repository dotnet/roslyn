// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace BuildValidator
{
    class Program
    {
        private static ILogger? _logger;
        private static readonly Regex[] _ignorePatterns = new Regex[]
        {
            new Regex(@"\\runtimes?\\"),
            new Regex(@"\\ref\\"),
            new Regex(@"\.resources?\.")
        };

        static async Task Main(string[] args)
        {
            Options options;
            try
            {
                options = Options.Create(args);
            }
            catch (InvalidDataException)
            {
                PrintHelp();
                return;
            }

            var loggerFactory = new LoggerFactory(Enumerable.Empty<ILoggerProvider>(), new LoggerFilterOptions()
            {
                MinLevel = options.Verbose ? LogLevel.Trace : LogLevel.Warning
            });

            if (options.ConsoleOutput)
            {
                loggerFactory.AddConsole();
            }

            _logger = loggerFactory.CreateLogger<Program>();

            var sourceResolver = new LocalSourceResolver(loggerFactory);
            var referenceResolver = new LocalReferenceResolver(loggerFactory);

            var buildConstructor = new BuildConstructor(loggerFactory, referenceResolver, sourceResolver);

            var artifactsDir = LocalReferenceResolver.GetArtifactsDirectory();
            var thisCompilerVersion = options.IgnoreCompilerVersion
                ? null
                : typeof(Compilation).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

            var filesToValidate = artifactsDir.EnumerateFiles("*.exe", SearchOption.AllDirectories)
                .Concat(artifactsDir.EnumerateFiles("*.dll", SearchOption.AllDirectories))
                .Distinct(FileNameEqualityComparer.Instance);

            await ValidateFilesAsync(filesToValidate, buildConstructor, thisCompilerVersion).ConfigureAwait(false);
        }

        private static async Task ValidateFilesAsync(IEnumerable<FileInfo> files, BuildConstructor buildConstructor, string? thisCompilerVersion)
        {
            var assembliesCompiled = new List<CompilationDiff>();

            foreach (var file in files)
            {
                var compilationDiff = await ValidateFileAsync(file, buildConstructor, thisCompilerVersion).ConfigureAwait(false);

                if (compilationDiff is null)
                {
                    continue;
                }

                assembliesCompiled.Add(compilationDiff);
            }

            StringBuilder sb = new StringBuilder();

            sb.AppendLine("====================");
            sb.AppendLine("Summary:");
            sb.AppendLine();
            sb.AppendLine("Successful Tests:");

            foreach (var diff in assembliesCompiled.Where(a => a.AreEqual == true))
            {
                sb.AppendLine($"\t{diff.OriginalPath}");
            }

            sb.AppendLine();

            sb.AppendLine("Failed Tests:");
            foreach (var diff in assembliesCompiled.Where(a => a.AreEqual == false))
            {
                sb.AppendLine($"\t{diff.OriginalPath}");
            }

            sb.AppendLine();
            sb.AppendLine("Error Cases:");
            foreach (var diff in assembliesCompiled.Where(a => !a.AreEqual.HasValue))
            {
                sb.AppendLine($"\t{diff.OriginalPath}");
                if (diff.Exception != null)
                {
                    sb.AppendLine($"\tException: {diff.Exception.Message}");
                }
            }
            sb.AppendLine("====================");

            _logger.LogInformation(sb.ToString());
        }

        private static async Task<CompilationDiff?> ValidateFileAsync(FileInfo file, BuildConstructor buildConstructor, string? thisCompilerVersion)
        {

            if (_ignorePatterns.Any(r => r.IsMatch(file.FullName)))
            {
                _logger.LogTrace($"Ignoring {file.FullName}");
                return null;
            }

            // Check if the file was built by us
            if (!TryLoadAssembly(file.FullName, out var assembly) || assembly is null)
            {
                return null;
            }

            var assemblyVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (thisCompilerVersion != null && assemblyVersion != thisCompilerVersion)
            {
                _logger.LogInformation($"Skipping {file.FullName}");
                return null;
            }

            // Find the embedded pdb
            using var fileStream = file.OpenRead();
            using var peReader = new PEReader(fileStream);

            var entries = peReader.ReadDebugDirectory();
            DebugDirectoryEntry? embedded = null;
            foreach (var entry in entries)
            {
                if (entry.Type == DebugDirectoryEntryType.EmbeddedPortablePdb)
                {
                    embedded = entry;
                    break;
                }
            }

            if (!embedded.HasValue)
            {
                _logger.LogError($"Could not find embedded pdb for {file.FullName}");
                return null;
            }

            try
            {
                using var embeddedPdb = peReader.ReadEmbeddedPortablePdbDebugDirectoryData(embedded.Value);

                _logger.LogInformation($"Compiling {file.FullName}");

                var compilation = await buildConstructor.CreateCompilationAsync(embeddedPdb, file.Name).ConfigureAwait(false);
                return CompilationDiff.Create(assembly, compilation);
            }
            catch (Exception e)
            {
                _logger.LogError(e, file.FullName);
                return CompilationDiff.Create(assembly, e);
            }
        }

        private static void PrintHelp()
        {
            Console.WriteLine("Usage: BuildValidator [options]");
            Console.WriteLine("Options:");
            Console.WriteLine("/verbose                 Output verbose log information");
            Console.WriteLine("/quiet                   Do not output log information to console");
            Console.WriteLine("/ignorecompilerversion   Do not verify compiler version that assemblies were generated with");
            Console.WriteLine("/log <logFile>           Write logs into the log file specified");
        }

        private static bool TryLoadAssembly(string fullPath, out Assembly? assembly)
        {
            assembly = null;
            try
            {
                assembly = Assembly.LoadFrom(fullPath);
                return true;
            }
            catch (BadImageFormatException)
            {
                _logger.LogTrace($"Failed to load assembly for {fullPath}: Bad Image Format");
            }
            catch (IOException e)
            {
                _logger.LogError(e, $"Loading {fullPath} failed: IO Exception");
            }

            return false;
        }
    }
}
