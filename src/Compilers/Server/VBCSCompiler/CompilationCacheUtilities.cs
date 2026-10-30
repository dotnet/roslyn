// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Threading;
using Microsoft.CodeAnalysis.CommandLine;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CompilerServer
{
    internal sealed class CacheState
    {
        public required string DeterministicKey { get; init; }
        public required string HashKey { get; init; }
        public required string DiagnosticContext { get; init; }
    }

    /// <summary>
    /// Shared implementation of the compilation cache hooks used by
    /// <see cref="CSharpCompilerServer"/> and <see cref="VisualBasicCompilerServer"/>.
    /// </summary>
    internal static class CompilationCacheUtilities
    {
        /// <summary>
        /// Checks whether a cached output exists for the given compilation inputs. Returns the
        /// cached exit code when a hit is found, or <see langword="null"/> on a miss.
        /// Also outputs the computed keys and diagnostic context in <paramref name="cacheState"/>
        /// so callers can store them to avoid recomputing on success.
        /// </summary>
        internal static int? CheckCache(
            CompilationCache? cache,
            ICompilerServerLogger logger,
            CommandLineArguments arguments,
            Compilation compilation,
            ImmutableArray<DiagnosticAnalyzer> analyzers,
            ImmutableArray<ISourceGenerator> generators,
            ImmutableArray<AdditionalText> additionalTexts,
            CultureInfo culture,
            TextWriter consoleOutput,
            CompilationCacheTelemetry telemetry,
            CancellationToken cancellationToken,
            out CacheState? cacheState)
        {
            cacheState = null;

            if (cache is null || arguments.OutputFileName is null)
            {
                return null;
            }

            var dllName = arguments.OutputFileName;
            var outputTimestampUtc = DateTime.UtcNow;
            string deterministicKey;
            string diagnosticContext;
            telemetry.StartKeyComputeTimer();
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
                diagnosticContext = GetDiagnosticContext(arguments, compilation, additionalTexts, culture, cancellationToken);
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
            finally
            {
                telemetry.StopKeyComputeTimer();
            }

            var hashKey = CompilationCache.ComputeHashKey(deterministicKey);
            cacheState = new CacheState
            {
                DeterministicKey = deterministicKey,
                HashKey = hashKey,
                DiagnosticContext = diagnosticContext,
            };

            var outputFiles = BuildOutputFiles(arguments, dllName);
            telemetry.StartRestoreTimer();
            var restored = cache.TryRestoreCachedResult(dllName, hashKey, outputFiles, diagnosticContext, logger, outputTimestampUtc, out var diagnosticOutput);
            telemetry.StopRestoreTimer();

            if (restored)
            {
                telemetry.Status = CompilationCacheStatus.Hit;
                logger.Log($"Cache hit satisfied: {dllName} [{hashKey}]");
                consoleOutput.Write(diagnosticOutput);
                return CommonCompiler.Succeeded;
            }

            telemetry.Status = CompilationCacheStatus.Miss;
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
            CacheState? cacheState,
            string? diagnosticOutput,
            CompilationCacheTelemetry telemetry)
        {
            if (cache is null || arguments.OutputFileName is null || cacheState is null || diagnosticOutput is null)
            {
                return;
            }

            var dllName = arguments.OutputFileName;
            var outputFiles = BuildOutputFiles(arguments, dllName);
            telemetry.StartStoreTimer();
            telemetry.StoreResult = cache.TryStoreResult(dllName, cacheState.HashKey, outputFiles, cacheState.DeterministicKey, cacheState.DiagnosticContext, diagnosticOutput, logger);
            telemetry.StopStoreTimer();
        }

        private static string GetDiagnosticContext(
            CommandLineArguments arguments,
            Compilation compilation,
            ImmutableArray<AdditionalText> additionalTexts,
            CultureInfo culture,
            CancellationToken cancellationToken)
        {
            using var output = new StringWriter(CultureInfo.InvariantCulture);
            using var writer = new JsonWriter(output);
            writer.WriteObjectStart();
            writer.Write("culture", culture.Name);
            writer.Write("currentCulture", CultureInfo.CurrentCulture.Name);
            writer.Write("baseDirectory", arguments.BaseDirectory);
            writer.Write("fullPaths", arguments.PrintFullPaths);
            writer.Write("endLocations", arguments is CSharp.CSharpCommandLineArguments { ShouldIncludeErrorEndLocation: true });
            writer.Write("newLine", Environment.NewLine);
            writer.WriteArrayStart("sourcePaths");
            foreach (var tree in compilation.SyntaxTrees)
            {
                cancellationToken.ThrowIfCancellationRequested();
                writer.Write(tree.FilePath);
            }
            writer.WriteArrayEnd();
            writer.WriteArrayStart("referencePaths");
            foreach (var reference in compilation.References)
            {
                cancellationToken.ThrowIfCancellationRequested();
                writer.Write(reference.Display);
            }
            writer.WriteArrayEnd();
            writer.WriteArrayStart("additionalPaths");
            foreach (var additionalText in additionalTexts)
            {
                cancellationToken.ThrowIfCancellationRequested();
                writer.Write(additionalText.Path);
            }
            writer.WriteArrayEnd();
            writer.WriteObjectEnd();
            return CompilationCache.ComputeHashKey(output.ToString());
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
