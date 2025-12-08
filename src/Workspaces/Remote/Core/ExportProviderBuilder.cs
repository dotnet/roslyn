// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Threading;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.CodeAnalysis.Remote;

internal abstract class ExportProviderBuilder(
    ImmutableArray<string> assemblyPaths,
    Resolver resolver,
    string cacheDirectory,
    string catalogPrefix)
{
    private const string CatalogSuffix = ".mef-composition";

    protected ImmutableArray<string> AssemblyPaths { get; } = assemblyPaths;
    protected Resolver Resolver { get; } = resolver;
    protected string CacheDirectory { get; } = cacheDirectory;
    protected string CatalogPrefix { get; } = catalogPrefix;

    protected abstract void LogError(string message, Exception exception);
    protected abstract void LogTrace(string message);

    protected virtual async Task<ExportProvider> CreateExportProviderAsync(CancellationToken cancellationToken)
    {
        // Get the cached MEF composition or create a new one.
        var exportProviderFactory = await GetCompositionConfigurationAsync(cancellationToken).ConfigureAwait(false);

        // Create an export provider, which represents a unique container of values.
        // You can create as many of these as you want, but typically an app needs just one.
        var exportProvider = exportProviderFactory.CreateExportProvider();

        return exportProvider;
    }

    private async Task<IExportProviderFactory> GetCompositionConfigurationAsync(CancellationToken cancellationToken)
    {
        // Determine the path to the MEF composition cache file for the given assembly paths.
        var compositionCacheFile = GetCompositionCacheFilePath();

        // Try to load a cached composition.
        try
        {
            if (File.Exists(compositionCacheFile))
            {
                LogTrace($"Loading cached MEF catalog: {compositionCacheFile}");

                CachedComposition cachedComposition = new();
                using FileStream cacheStream = new(compositionCacheFile, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
                var exportProviderFactory = await cachedComposition.LoadExportProviderFactoryAsync(cacheStream, Resolver, cancellationToken).ConfigureAwait(false);

                return exportProviderFactory;
            }
        }
        catch (Exception ex)
        {
            // Log the error, and move on to recover by recreating the MEF composition.
            LogError("Loading cached MEF composition failed", ex);
        }

        LogTrace($"Composing MEF catalog using:{Environment.NewLine}{string.Join($"    {Environment.NewLine}", AssemblyPaths)}.");

        var discovery = PartDiscovery.Combine(
            Resolver,
            new AttributedPartDiscovery(Resolver, isNonPublicSupported: true), // "NuGet MEF" attributes (Microsoft.Composition)
            new AttributedPartDiscoveryV1(Resolver));

        var parts = await discovery.CreatePartsAsync(AssemblyPaths, progress: null, cancellationToken).ConfigureAwait(false);

        var catalog = ComposableCatalog.Create(Resolver)
            .AddParts(parts)
            .WithCompositionService(); // Makes an ICompositionService export available to MEF parts to import

        // Assemble the parts into a valid graph.
        var config = CompositionConfiguration.Create(catalog);

        // Check if we have errors, and report them accordingly.
        if (!CheckForAndReportCompositionErrors(config, catalog))
        {
            // There weren't any errors in the composition, so let's cache it. If there were errors, those errors might have been temporary and we don't want
            // to end up in a permanently broken case.
            _ = WriteCompositionCacheAsync(compositionCacheFile, config, cancellationToken).ReportNonFatalErrorAsync();
        }

        // Prepare an ExportProvider factory based on this graph.
        return config.CreateExportProviderFactory();
    }

    /// <summary>
    /// Returns the path to the MEF composition cache file. Inputs used to determine the file name include:
    /// 1) The given assembly paths
    /// 2) The last write times of the given assembly paths
    /// 3) The .NET runtime major version
    /// </summary>
    private string GetCompositionCacheFilePath()
    {
        return Path.Combine(CacheDirectory, $"{CatalogPrefix}.{ComputeAssemblyHash(AssemblyPaths)}{CatalogSuffix}");

        static string ComputeAssemblyHash(ImmutableArray<string> assemblyPaths)
        {
            // Ensure AssemblyPaths are always in the same order.
            assemblyPaths = assemblyPaths.Sort(StringComparer.Ordinal);

            var hashContents = new StringBuilder();

            // This should vary based on .NET runtime major version so that as some of our processes switch between our target
            // .NET version and the user's selected SDK runtime version (which may be newer), the MEF cache is kept isolated.
            // This can be important when the MEF catalog records full assembly names such as "System.Runtime, 8.0.0.0" yet
            // we might be running on .NET 7 or .NET 8, depending on the particular session and user settings.
            hashContents.Append(Environment.Version.Major.ToString(System.Globalization.CultureInfo.InvariantCulture));

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

    protected virtual async Task WriteCompositionCacheAsync(string compositionCacheFile, CompositionConfiguration config, CancellationToken cancellationToken)
    {
        // Generally, it's not a hard failure if this code doesn't execute to completion or even fails. The end effect would simply 
        // either be a non-existent or invalid file cached to disk. In the case of the file not getting cached, the next VS session
        // will just detect the file doesn't exist and attempt to recreate the cache. In the case where the cached file contents are
        // invalid, the next VS session will throw when attempting to read in the cached contents, and again, just recreate the cache.
        try
        {
            await Task.Yield().ConfigureAwait(false);

            var directory = Path.GetDirectoryName(compositionCacheFile)!;
            var directoryInfo = Directory.CreateDirectory(directory);
            PerformCacheDirectoryCleanup(directoryInfo, cancellationToken);

            CachedComposition cachedComposition = new();
            var tempFilePath = Path.Combine(directory, Path.GetRandomFileName());
            using (FileStream cacheStream = new(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
            {
                await cachedComposition.SaveAsync(config, cacheStream, cancellationToken).ConfigureAwait(false);
            }

            File.Move(tempFilePath, compositionCacheFile);
        }
        catch (Exception ex)
        {
            LogError("Failed to save MEF cache", ex);
        }
    }

    protected virtual void PerformCacheDirectoryCleanup(DirectoryInfo directoryInfo, CancellationToken cancellationToken)
    {
        // Delete any existing cached files.
        foreach (var fileInfo in directoryInfo.EnumerateFiles())
        {
            // Failing to delete any file is fine, we'll just try again the next VS session in which we attempt
            // to write a new cache
            IOUtilities.PerformIO(fileInfo.Delete);
            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    protected abstract bool ContainsUnexpectedErrors(IEnumerable<string> erroredParts);

    /// <returns>True if there was an unexpected composition error, false otherwise.</returns>
    private bool CheckForAndReportCompositionErrors(CompositionConfiguration configuration, ComposableCatalog catalog)
    {
        var hasErrors = false;

        foreach (var exception in catalog.DiscoveredParts.DiscoveryErrors)
        {
            hasErrors = true;
            LogError("Encountered exception in the MEF composition", exception);
        }

        // Verify that we have exactly the MEF errors that we expect.  If we have less or more this needs to be updated to assert the expected behavior.
        var erroredParts = configuration.CompositionErrors.SelectMany(c => c).SelectMany(error => error.Parts).Select(part => part.Definition.Type.Name);

        if (ContainsUnexpectedErrors(erroredParts))
        {
            hasErrors = true;

            try
            {
                configuration.ThrowOnErrors();
            }
            catch (CompositionFailedException ex)
            {
                // The ToString for the composition failed exception doesn't output a nice set of errors by default, so log it separately
                LogError($"Encountered errors in the MEF composition:{Environment.NewLine}{ex.ErrorsAsString}", ex);
            }
        }

        return hasErrors;
    }
}
