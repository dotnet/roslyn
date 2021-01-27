// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
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
        private static readonly Regex[] s_ignorePatterns = new Regex[]
        {
            new Regex(@"\\runtimes?\\"),
            new Regex(@"\\ref\\"),
            new Regex(@"\.resources?\.")
        };

        static void Main(string[] args)
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
            
            var loggerFactory = new LoggerFactory(
                new[] { new ConsoleLoggerProvider(new ConsoleLoggerSettings()) },
                new LoggerFilterOptions()
                {
                    MinLevel = options.Verbose ? LogLevel.Trace : LogLevel.Information
                });

            if (options.ConsoleOutput)
            {
                loggerFactory.AddProvider(new DemoLoggerProvider());
            }

            try
            {
                var logger = loggerFactory.CreateLogger<Program>();
                var sourceResolver = new LocalSourceResolver(loggerFactory);
                var referenceResolver = new LocalReferenceResolver(loggerFactory);

                var buildConstructor = new BuildConstructor(referenceResolver, sourceResolver, logger);

                var artifactsDir = LocalReferenceResolver.GetArtifactsDirectory();
                var thisCompilerVersion = options.IgnoreCompilerVersion
                    ? null
                    : typeof(Compilation).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

                var filesToValidate = artifactsDir.EnumerateFiles("*.exe", SearchOption.AllDirectories)
                    .Concat(artifactsDir.EnumerateFiles("*.dll", SearchOption.AllDirectories))
                    .Distinct(FileNameEqualityComparer.Instance);

                ValidateFiles(filesToValidate, buildConstructor, thisCompilerVersion, logger);
                Console.Out.Flush();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        private static void ValidateFiles(IEnumerable<FileInfo> originalBinaries, BuildConstructor buildConstructor, string? thisCompilerVersion, ILogger logger)
        {
            var assembliesCompiled = new List<CompilationDiff>();
            var sb = new StringBuilder();

            foreach (var file in originalBinaries)
            {
                var compilationDiff = ValidateFile(file, buildConstructor, thisCompilerVersion, logger);

                if (compilationDiff is null)
                {
                    Console.WriteLine("ERROR!!!");
                    sb.AppendLine($"Ignoring {file.FullName}");
                    continue;
                }

                assembliesCompiled.Add(compilationDiff);
            }

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

            logger.LogInformation(sb.ToString());
        }

        private static CompilationDiff? ValidateFile(FileInfo originalBinary, BuildConstructor buildConstructor, string? thisCompilerVersion, ILogger logger)
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

                logger.LogInformation($"Verifying {originalBinary.FullName} with pdb {pdbPath ?? "[embedded]"}");
                using var _ = logger.BeginScope("");

                var pdbReader = pdbReaderProvider.GetMetadataReader();

                // TODO: Check compilation version using the PEReader

                var compilation = buildConstructor.CreateCompilation(
                    pdbReader,
                    originalPeReader,
                    Path.GetFileNameWithoutExtension(originalBinary.Name));

                var compilationDiff = CompilationDiff.Create(originalBinary, originalPeReader, pdbReader, compilation, GetDebugEntryPoint());
                logger.LogInformation(compilationDiff?.AreEqual == true ? "Verification succeeded" : "Verification failed");
                return compilationDiff;

                IMethodSymbol? GetDebugEntryPoint()
                {
                    var optionsReader = new CompilationOptionsReader(pdbReader, originalPeReader);
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
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                logger.LogError(e, originalBinary.FullName);
                return CompilationDiff.Create(originalBinary, e);
            }
            finally
            {
                pdbReaderProvider?.Dispose();
            }

        }

        private static void PrintHelp()
        {
            Console.WriteLine("Usage: BuildValidator [options]");
            Console.WriteLine("Options:");
            Console.WriteLine("/verbose                 Output verbose log information");
            Console.WriteLine("/quiet                   Do not output log information to console");
            Console.WriteLine("/ignorecompilerversion   Do not verify compiler version that assemblies were generated with");
        }
    }
}
