// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Utilities;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.CodeAnalysis.Remote.Razor;

// Inspired by https://github.com/dotnet/roslyn/blob/25aa74d725e801b8232dbb3e5abcda0fa72da8c5/src/Workspaces/Remote/ServiceHub/Host/RemoteWorkspaceManager.cs#L77

internal sealed class RemoteMefComposition
{
    public static readonly ImmutableArray<Assembly> Assemblies = [typeof(RemoteMefComposition).Assembly];

    private static readonly AsyncLazy<CompositionConfiguration> s_lazyConfiguration = new(
        static () => CreateConfigurationAsync(CancellationToken.None),
        joinableTaskFactory: null);

    private static readonly AsyncLazy<ExportProvider> s_lazyExportProvider = new(
        static () => CreateExportProviderAsync(CacheDirectory, CancellationToken.None),
        joinableTaskFactory: null);

    private static Task? s_saveCacheFileTask;

    public static string? CacheDirectory { get; set; }

    /// <summary>
    ///  Gets a <see cref="CompositionConfiguration"/> built from <see cref="Assemblies"/>. Note that the
    ///  same <see cref="CompositionConfiguration"/> instance is returned for subsequent calls to this method.
    /// </summary>
    public static Task<CompositionConfiguration> GetConfigurationAsync(CancellationToken cancellationToken)
        => s_lazyConfiguration.GetValueAsync(cancellationToken);

    /// <summary>
    ///  Gets an <see cref="ExportProvider"/> for the shared MEF composition. Note that the
    ///  same <see cref="ExportProvider"/> instance is returned for subsequent calls to this method.
    /// </summary>
    public static Task<ExportProvider> GetSharedExportProviderAsync(CancellationToken cancellationToken)
        => s_lazyExportProvider.GetValueAsync(cancellationToken);

    private static async Task<CompositionConfiguration> CreateConfigurationAsync(CancellationToken cancellationToken)
    {
        var resolver = new Resolver(SimpleAssemblyLoader.Instance);
        var discovery = new AttributedPartDiscovery(resolver, isNonPublicSupported: true); // MEFv2 only
        var parts = await discovery.CreatePartsAsync(Assemblies, cancellationToken: cancellationToken).ConfigureAwait(false);
        var catalog = ComposableCatalog.Create(resolver).AddParts(parts);

        return CompositionConfiguration.Create(catalog).ThrowOnErrors();
    }

    /// <summary>
    ///  Creates a new MEF composition and returns an <see cref="ExportProvider"/>. The catalog and configuration
    ///  are reused for subsequent calls to this method.
    /// </summary>
    public static async Task<ExportProvider> CreateExportProviderAsync(string? cacheDirectory, CancellationToken cancellationToken)
    {
        var cache = new CachedComposition();
        var compositionCacheFile = GetCompositionCacheFile(cacheDirectory);
        if (await TryLoadCachedExportProviderAsync(cache, compositionCacheFile, cancellationToken).ConfigureAwait(false) is { } cachedProvider)
        {
            return cachedProvider;
        }

        var configuration = await s_lazyConfiguration.GetValueAsync(cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        var runtimeComposition = RuntimeComposition.CreateRuntimeComposition(configuration);
        var exportProviderFactory = runtimeComposition.CreateExportProviderFactory();

        // We don't need to block on saving the cache, because if it fails or is corrupt, we'll just try again next time, but
        // we capture the task just so that tests can verify things.
        s_saveCacheFileTask = TrySaveCachedExportProviderAsync(cache, compositionCacheFile, runtimeComposition, cancellationToken);

        return exportProviderFactory.CreateExportProvider();
    }

    private static async Task<ExportProvider?> TryLoadCachedExportProviderAsync(CachedComposition cache, string? compositionCacheFile, CancellationToken cancellationToken)
    {
        if (compositionCacheFile is null)
        {
            return null;
        }

        try
        {
            if (File.Exists(compositionCacheFile))
            {
                var resolver = new Resolver(SimpleAssemblyLoader.Instance);
                using var cacheStream = new FileStream(compositionCacheFile, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
                var cachedFactory = await cache.LoadExportProviderFactoryAsync(cacheStream, resolver, cancellationToken).ConfigureAwait(false);
                return cachedFactory.CreateExportProvider();
            }
        }
        catch (Exception)
        {
            // We ignore all errors when loading the cache, because if the cache is corrupt we will just create a new export provider.
        }

        return null;
    }

    private static async Task TrySaveCachedExportProviderAsync(CachedComposition cache, string? compositionCacheFile, RuntimeComposition runtimeComposition, CancellationToken cancellationToken)
    {
        if (compositionCacheFile is null)
        {
            return;
        }

        try
        {
            var cacheDirectory = Path.GetDirectoryName(compositionCacheFile).AssumeNotNull();
            var directoryInfo = Directory.CreateDirectory(cacheDirectory);

            CleanCacheDirectory(directoryInfo, cancellationToken);

            var tempFilePath = Path.Combine(cacheDirectory, Path.GetRandomFileName());
            using (var cacheStream = new FileStream(compositionCacheFile, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
            {
                await cache.SaveAsync(runtimeComposition, cacheStream, cancellationToken).ConfigureAwait(false);
            }

            File.Move(tempFilePath, compositionCacheFile);
        }
        catch (Exception)
        {
            // We ignore all errors when saving the cache, because if something goes wrong, the next run will just create a new export provider.
        }
    }

    private static void CleanCacheDirectory(DirectoryInfo directoryInfo, CancellationToken cancellationToken)
    {
        try
        {
            // Delete any existing cached files.
            foreach (var fileInfo in directoryInfo.EnumerateFiles())
            {
                // Failing to delete any file is fine, we'll just try again the next VS session in which we attempt
                // to write a new cache
                fileInfo.Delete();
                cancellationToken.ThrowIfCancellationRequested();
            }
        }
        catch (Exception)
        {
            // We ignore all errors when cleaning the cache directory, because we'll try again if the cache is corrupt.
        }
    }

    [return: NotNullIfNotNull(nameof(cacheDirectory))]
    private static string? GetCompositionCacheFile(string? cacheDirectory)
    {
        if (cacheDirectory is null)
        {
            return null;
        }

        var checksum = new Checksum.Builder();
        foreach (var assembly in Assemblies)
        {
            var assemblyPath = assembly.Location.AssumeNotNull();
            checksum.Append(Path.GetFileName(assemblyPath));
            checksum.Append(File.GetLastWriteTimeUtc(assemblyPath).ToString("F"));
        }

        // Create base64 string of the hash.
        var hashAsBase64String = checksum.FreeAndGetChecksum().ToBase64String();

        // Convert to filename safe base64 string.
        hashAsBase64String = hashAsBase64String.Replace('+', '-').Replace('/', '_').TrimEnd('=');

        return Path.Combine(cacheDirectory, $"razor.mef.{hashAsBase64String}.cache");
    }

    private sealed class SimpleAssemblyLoader : IAssemblyLoader
    {
        public static readonly IAssemblyLoader Instance = new SimpleAssemblyLoader();

        public Assembly LoadAssembly(AssemblyName assemblyName)
            => Assembly.Load(assemblyName);

        public Assembly LoadAssembly(string assemblyFullName, string? codeBasePath)
        {
            var assemblyName = new AssemblyName(assemblyFullName);
            if (!string.IsNullOrEmpty(codeBasePath))
            {
#pragma warning disable SYSLIB0044 // https://github.com/dotnet/roslyn/issues/71510
                assemblyName.CodeBase = codeBasePath;
#pragma warning restore SYSLIB0044
            }

            return LoadAssembly(assemblyName);
        }
    }

    public static class TestAccessor
    {
        public static Task? SaveCacheFileTask => s_saveCacheFileTask;

        public static void ClearSaveCacheFileTask()
        {
            s_saveCacheFileTask = null;
        }

        public static string GetCacheCompositionFile(string cacheDirectory)
        {
            return GetCompositionCacheFile(cacheDirectory);
        }
    }
}
