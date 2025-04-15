// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Threading;
using Microsoft.VisualStudio.Composition;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote;

internal sealed class ExportProviderBuilder
{
    private const string CatalogSuffix = ".mef-composition";

    // For testing purposes, track the last cache write task.
    private static Task? s_cacheWriteTask;

    public record struct ExportProviderCreationArguments(
        ImmutableArray<string> AssemblyPaths,
        Resolver Resolver,
        string CacheDirectory,
        string CatalogPrefix,
        ImmutableArray<string> ExpectedErrorParts,
        bool PerformCleanup,
        Action<string> LogError,
        Action<string> LogTrace);

    public static async Task<ExportProvider> CreateExportProviderAsync(
        ExportProviderCreationArguments args,
        CancellationToken cancellationToken)
    {
        // Clear any previous cache write task, so that it is easy to discern whether
        // a cache write was attempted.
        s_cacheWriteTask = null;

        // Get the cached MEF composition or create a new one.
        var exportProviderFactory = await GetCompositionConfigurationAsync(args, cancellationToken).ConfigureAwait(false);

        // Create an export provider, which represents a unique container of values.
        // You can create as many of these as you want, but typically an app needs just one.
        var exportProvider = exportProviderFactory.CreateExportProvider();

        return exportProvider;
    }

    private static async Task<IExportProviderFactory> GetCompositionConfigurationAsync(
        ExportProviderCreationArguments args,
        CancellationToken cancellationToken)
    {
        // Determine the path to the MEF composition cache file for the given assembly paths.
        var compositionCacheFile = GetCompositionCacheFilePath(args.CacheDirectory, args.CatalogPrefix, args.AssemblyPaths);

        // Try to load a cached composition.
        try
        {
            if (File.Exists(compositionCacheFile))
            {
                args.LogTrace($"Loading cached MEF catalog: {compositionCacheFile}");

                CachedComposition cachedComposition = new();
                using FileStream cacheStream = new(compositionCacheFile, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
                var exportProviderFactory = await cachedComposition.LoadExportProviderFactoryAsync(cacheStream, args.Resolver, cancellationToken).ConfigureAwait(false);

                return exportProviderFactory;
            }
        }
        catch (Exception ex)
        {
            // Log the error, and move on to recover by recreating the MEF composition.
            args.LogError($"Loading cached MEF composition failed: {ex}");
        }

        args.LogTrace($"Composing MEF catalog using:{Environment.NewLine}{string.Join($"    {Environment.NewLine}", args.AssemblyPaths)}.");

        var discovery = PartDiscovery.Combine(
            args.Resolver,
            new AttributedPartDiscovery(args.Resolver, isNonPublicSupported: true), // "NuGet MEF" attributes (Microsoft.Composition)
            new AttributedPartDiscoveryV1(args.Resolver));

        var parts = await discovery.CreatePartsAsync(args.AssemblyPaths, progress: null, cancellationToken).ConfigureAwait(false);
        var catalog = ComposableCatalog.Create(args.Resolver)
            .AddParts(parts)
            .WithCompositionService(); // Makes an ICompositionService export available to MEF parts to import

        // Assemble the parts into a valid graph.
        var config = CompositionConfiguration.Create(catalog);

        // Verify we only have expected errors.
        ThrowOnUnexpectedErrors(config, catalog, args.ExpectedErrorParts, args.LogError);

        // Try to cache the composition.
        s_cacheWriteTask = WriteCompositionCacheAsync(compositionCacheFile, config, args.PerformCleanup, args.LogError, cancellationToken).ReportNonFatalErrorAsync();

        // Prepare an ExportProvider factory based on this graph.
        return config.CreateExportProviderFactory();
    }

    /// <summary>
    /// Returns the path to the MEF composition cache file. Inputs used to determine the file name include:
    /// 1) The given assembly paths
    /// 2) The last write times of the given assembly paths
    /// 3) The .NET runtime major version
    /// </summary>
    private static string GetCompositionCacheFilePath(string cacheDirectory, string catalogPrefix, ImmutableArray<string> assemblyPaths)
    {
        return Path.Combine(cacheDirectory, $"{catalogPrefix}.{ComputeAssemblyHash(assemblyPaths)}{CatalogSuffix}");

        static string ComputeAssemblyHash(ImmutableArray<string> assemblyPaths)
        {
            // Ensure AssemblyPaths are always in the same order.
            assemblyPaths = assemblyPaths.Sort();

            var hashContents = new StringBuilder();

            // This should vary based on .NET runtime major version so that as some of our processes switch between our target
            // .NET version and the user's selected SDK runtime version (which may be newer), the MEF cache is kept isolated.
            // This can be important when the MEF catalog records full assembly names such as "System.Runtime, 8.0.0.0" yet
            // we might be running on .NET 7 or .NET 8, depending on the particular session and user settings.
            hashContents.Append(Environment.Version.Major);

            foreach (var assemblyPath in assemblyPaths)
            {
                // Include assembly path in the hash so that changes to the set of included
                // assemblies cause the composition to be rebuilt.
                hashContents.Append(assemblyPath);
                // Include the last write time in the hash so that newer assemblies written
                // to the same location cause the composition to be rebuilt.
                hashContents.Append(File.GetLastWriteTimeUtc(assemblyPath).ToString("F"));
            }

            // Create base64 string of the hash.
            var hashAsBase64String = Checksum.Create(hashContents.ToString()).ToString();

            // Convert to filename safe base64 string.
            return hashAsBase64String.Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }
    }

    private static async Task WriteCompositionCacheAsync(string compositionCacheFile, CompositionConfiguration config, bool performCleanup, Action<string> logError, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Yield().ConfigureAwait(false);

            if (Path.GetDirectoryName(compositionCacheFile) is string directory)
            {
                var directoryInfo = Directory.CreateDirectory(directory);

                if (performCleanup)
                {
                    // Delete any existing cached files.
                    foreach (var fileInfo in directoryInfo.EnumerateFiles($"*{CatalogSuffix}"))
                    {
                        fileInfo.Delete();
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                }
            }

            CachedComposition cachedComposition = new();
            var tempFilePath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
            using (FileStream cacheStream = new(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
            {
                await cachedComposition.SaveAsync(config, cacheStream, cancellationToken).ConfigureAwait(false);
            }

#if NET
            File.Move(tempFilePath, compositionCacheFile, overwrite: true);
#else
            // On .NET Framework, File.Move doesn't support overwriting the destination file. Use File.Delete first
            // to ensure the destination file is removed before moving the temp file. File.Delete will not throw if
            // the file doesn't exist.
            File.Delete(compositionCacheFile);
            File.Move(tempFilePath, compositionCacheFile);
#endif
        }
        catch (Exception ex)
        {
            logError($"Failed to save MEF cache: {ex}");
        }
    }

    private static void ThrowOnUnexpectedErrors(CompositionConfiguration configuration, ComposableCatalog catalog, ImmutableArray<string> expectedErrorParts, Action<string> logError)
    {
        // Verify that we have exactly the MEF errors that we expect.  If we have less or more this needs to be updated to assert the expected behavior.
        var erroredParts = configuration.CompositionErrors.FirstOrDefault()?.SelectMany(error => error.Parts).Select(part => part.Definition.Type.Name) ?? [];
        var expectedErrorPartsSet = expectedErrorParts.ToSet();
        var hasUnexpectedErroredParts = erroredParts.Any(part => !expectedErrorPartsSet.Contains(part));

        if (hasUnexpectedErroredParts || !catalog.DiscoveredParts.DiscoveryErrors.IsEmpty)
        {
            try
            {
                catalog.DiscoveredParts.ThrowOnErrors();
                configuration.ThrowOnErrors();
            }
            catch (CompositionFailedException ex)
            {
                // The ToString for the composition failed exception doesn't output a nice set of errors by default, so log it separately
                logError($"Encountered errors in the MEF composition:{Environment.NewLine}{ex.ErrorsAsString}");
                throw;
            }
        }
    }

    internal static class TestAccessor
    {
#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
        public static Task? GetCacheWriteTask() => s_cacheWriteTask;
#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods
    }
}
