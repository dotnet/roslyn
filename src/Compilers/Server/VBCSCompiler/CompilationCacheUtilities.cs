// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CommandLine;
using Microsoft.CodeAnalysis.Diagnostics;

using System;
using System.IO;

namespace Microsoft.CodeAnalysis.CompilerServer
{
    /// <summary>
    /// Shared implementation of the compilation cache hooks used by
    /// <see cref="CSharpCompilerServer"/> and <see cref="VisualBasicCompilerServer"/>.
    /// </summary>
    internal static class CompilationCacheUtilities
    {
        /// <summary>
        /// Checks whether a cached output exists for the given compilation inputs. Returns the
        /// cached exit code when a hit is found, or <see langword="null"/> on a miss.
        /// Also outputs the computed <paramref name="deterministicKey"/> and
        /// <paramref name="hashKey"/> so callers can store them to avoid recomputing on success.
        /// </summary>
        internal static int? CheckCache(
            CompilationCache? cache,
            ICompilerServerLogger logger,
            CommandLineArguments arguments,
            Compilation compilation,
            ImmutableArray<DiagnosticAnalyzer> analyzers,
            ImmutableArray<ISourceGenerator> generators,
            ImmutableArray<AdditionalText> additionalTexts,
            CancellationToken cancellationToken,
            out string? deterministicKey,
            out string? hashKey)
        {
            deterministicKey = null;
            hashKey = null;

            if (cache is null || arguments.OutputFileName is null)
            {
                return null;
            }

            var dllName = arguments.OutputFileName;
            try
            {
                using var sourceLinkStream = arguments.SourceLink is not null
                    ? new FileStream(arguments.SourceLink, FileMode.Open, FileAccess.Read, FileShare.Read)
                    : null;

                deterministicKey = compilation.GetDeterministicKey(
                    additionalTexts,
                    analyzers,
                    generators,
                    arguments.PathMap,
                    arguments.EmitOptions,
                    sourceLinkStream,
                    arguments.ManifestResources);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.Log($"Failed to compute deterministic key, skipping cache: {ex.Message}");
                return null;
            }

            hashKey = CompilationCache.ComputeHashKey(deterministicKey);

            var outputFiles = BuildOutputFiles(arguments, dllName);
            if (cache.TryRestoreCachedResult(dllName, hashKey, outputFiles, logger))
            {
                logger.Log($"Cache hit satisfied: {dllName} [{hashKey}]");
                return CommonCompiler.Succeeded;
            }

            cache.LogCacheMiss(dllName, hashKey, deterministicKey, logger);
            return null;
        }

        /// <summary>
        /// Stores the compiled output in the cache after a successful compilation.
        /// </summary>
        internal static void OnCompilationSucceeded(
            CompilationCache? cache,
            ICompilerServerLogger logger,
            CommandLineArguments arguments,
            string? deterministicKey,
            string? hashKey)
        {
            if (cache is null || arguments.OutputFileName is null || deterministicKey is null || hashKey is null)
            {
                return;
            }

            var dllName = arguments.OutputFileName;
            var outputFiles = BuildOutputFiles(arguments, dllName);
            cache.TryStoreResult(dllName, hashKey, outputFiles, deterministicKey, logger);
        }

        private static CompilationOutputFiles BuildOutputFiles(CommandLineArguments arguments, string dllName)
        {
            return new CompilationOutputFiles
            {
                AssemblyPath = arguments.GetOutputFilePath(dllName),
                PdbPath = arguments.EmitPdbFile ? arguments.GetPdbFilePath(dllName) : null,
                RefAssemblyPath = arguments.OutputRefFilePath,
                XmlDocPath = arguments.DocumentationPath,
            };
        }
    }
}
