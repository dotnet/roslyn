// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.IO.Hashing;
using System.Text;
using Microsoft.CodeAnalysis.LanguageServer.Logging;
using Microsoft.CodeAnalysis.LanguageServer.Services;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Composition;
using Roslyn.Utilities;
using RoslynLog = Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.LanguageServer;

internal sealed class ExportProviderBuilder
{
    public static async Task<ExportProvider> CreateExportProviderAsync(
        ExtensionAssemblyManager extensionManager,
        IAssemblyLoader assemblyLoader,
        string? devKitDependencyPath,
        string? cacheDirectory,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger<ExportProviderBuilder>();
        var baseDirectory = AppContext.BaseDirectory;

        // Load any Roslyn assemblies from the extension directory
        var assemblyPaths = Directory.EnumerateFiles(baseDirectory, "Microsoft.CodeAnalysis*.dll");
        assemblyPaths = assemblyPaths.Concat(Directory.EnumerateFiles(baseDirectory, "Microsoft.ServiceHub*.dll"));

        // DevKit assemblies are not shipped in the main language server folder
        // and not included in ExtensionAssemblyPaths (they get loaded into the default ALC).
        // So manually add them to the MEF catalog here.
        if (devKitDependencyPath != null)
        {
            assemblyPaths = assemblyPaths.Concat(devKitDependencyPath);
        }

        // Add the extension assemblies to the MEF catalog.
        assemblyPaths = assemblyPaths.Concat(extensionManager.ExtensionAssemblyPaths);

        // Get the cached MEF composition or create a new one.
        var exportProviderFactory = await GetCompositionConfigurationAsync(assemblyPaths.ToImmutableArray(), assemblyLoader, cacheDirectory, logger);

        // Create an export provider, which represents a unique container of values.
        // You can create as many of these as you want, but typically an app needs just one.
        var exportProvider = exportProviderFactory.CreateExportProvider();

        // Immediately set the logger factory, so that way it'll be available for the rest of the composition
        exportProvider.GetExportedValue<ServerLoggerFactory>().SetFactory(loggerFactory);

        // Also add the ExtensionAssemblyManager so it will be available for the rest of the composition.
        exportProvider.GetExportedValue<ExtensionAssemblyManagerMefProvider>().SetMefExtensionAssemblyManager(extensionManager);

        return exportProvider;
    }

    private static async Task<IExportProviderFactory> GetCompositionConfigurationAsync(
        ImmutableArray<string> assemblyPaths,
        IAssemblyLoader assemblyLoader,
        string cacheDirectory,
        ILogger logger)
    {
        // Create a MEF resolver that can resolve assemblies in the extension contexts.
        var resolver = new Resolver(assemblyLoader);

        var compositionCacheFile = GetCompositionCacheFilePath(cacheDirectory, assemblyPaths);

        // Try to load a cached composition.
        try
        {
            if (File.Exists(compositionCacheFile))
            {
                logger.LogTrace($"Loading cached MEF catalog: {compositionCacheFile}");

                CachedComposition cachedComposition = new();
                using FileStream cacheStream = new(compositionCacheFile, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
                var exportProviderFactory = await cachedComposition.LoadExportProviderFactoryAsync(cacheStream, resolver);

                RoslynLog.Logger.Log(RoslynLog.FunctionId.LSP_MEF_Cache_Load_Success);

                return exportProviderFactory;
            }
        }
        catch (Exception ex)
        {
            // Log the error, and move on to recover by recreating the MEF composition.
            logger.LogError($"Loading cached MEF composition failed: {ex}");
            RoslynLog.Logger.Log(RoslynLog.FunctionId.LSP_MEF_Cache_Load_Failure);
        }

        logger.LogTrace($"Composing MEF catalog using:{Environment.NewLine}{string.Join($"    {Environment.NewLine}", assemblyPaths)}.");

        var discovery = PartDiscovery.Combine(
            resolver,
            new AttributedPartDiscovery(resolver, isNonPublicSupported: true), // "NuGet MEF" attributes (Microsoft.Composition)
            new AttributedPartDiscoveryV1(resolver));

        var catalog = ComposableCatalog.Create(resolver)
            .AddParts(await discovery.CreatePartsAsync(assemblyPaths))
            .WithCompositionService(); // Makes an ICompositionService export available to MEF parts to import

        // Assemble the parts into a valid graph.
        var config = CompositionConfiguration.Create(catalog);

        // Verify we only have expected errors.
        ThrowOnUnexpectedErrors(config, catalog, logger);

        RoslynLog.Logger.Log(RoslynLog.FunctionId.LSP_MEF_Cache_Built);

        // Try to cache the composition.
        _ = WriteCompositionCacheAsync(compositionCacheFile, config, logger);

        // Prepare an ExportProvider factory based on this graph.
        return config.CreateExportProviderFactory();
    }

    private static string GetCompositionCacheFilePath(string cacheDirectory, ImmutableArray<string> assemblyPaths)
    {
        // Include the .NET runtime version in the cache path so that running on a newer
        // runtime causes the cache to be rebuilt.
        var cacheSubdirectory = $".NET {Environment.Version.Major}";
        return Path.Combine(cacheDirectory, cacheSubdirectory, $"c#-languageserver.{ComputeAssemblyHash(assemblyPaths)}.mef-composition");

        static string ComputeAssemblyHash(ImmutableArray<string> assemblyPaths)
        {
            // Ensure AssemblyPaths are always in the same order.
            assemblyPaths = assemblyPaths.Sort();

            var assemblies = new StringBuilder();
            foreach (var assemblyPath in assemblyPaths)
            {
                // Include assembly path in the hash so that changes to the set of included
                // assemblies cause the composition to be rebuilt.
                assemblies.Append(assemblyPath);
                // Include the last write time in the hash so that newer assemblies written
                // to the same location cause the composition to be rebuilt.
                assemblies.Append(File.GetLastWriteTimeUtc(assemblyPath).ToString("F"));
            }

            var hash = XxHash128.Hash(Encoding.UTF8.GetBytes(assemblies.ToString()));
            // Convert to filename safe base64 string.
            return Convert.ToBase64String(hash).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }
    }

    private static async Task WriteCompositionCacheAsync(string compositionCacheFile, CompositionConfiguration config, ILogger logger)
    {
        try
        {
            if (Path.GetDirectoryName(compositionCacheFile) is string directory)
            {
                Directory.CreateDirectory(directory);
            }

            CachedComposition cachedComposition = new();
            var tempFilePath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
            using (FileStream cacheStream = new(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
            {
                await cachedComposition.SaveAsync(config, cacheStream);
            }

            File.Move(tempFilePath, compositionCacheFile, overwrite: true);
        }
        catch (Exception ex)
        {
            logger.LogError($"Failed to save MEF cache: {ex}");
        }
    }

    private static void ThrowOnUnexpectedErrors(CompositionConfiguration configuration, ComposableCatalog catalog, ILogger logger)
    {
        // Verify that we have exactly the MEF errors that we expect.  If we have less or more this needs to be updated to assert the expected behavior.
        // Currently we are expecting the following:
        //     "----- CompositionError level 1 ------
        //     Microsoft.CodeAnalysis.ExternalAccess.Pythia.PythiaSignatureHelpProvider.ctor(implementation): expected exactly 1 export matching constraints:
        //         Contract name: Microsoft.CodeAnalysis.ExternalAccess.Pythia.Api.IPythiaSignatureHelpProviderImplementation
        //         TypeIdentityName: Microsoft.CodeAnalysis.ExternalAccess.Pythia.Api.IPythiaSignatureHelpProviderImplementation
        //     but found 0.
        //         part definition Microsoft.CodeAnalysis.ExternalAccess.Pythia.PythiaSignatureHelpProvider
        var erroredParts = configuration.CompositionErrors.FirstOrDefault()?.SelectMany(error => error.Parts).Select(part => part.Definition.Type.Name) ?? Enumerable.Empty<string>();
        var expectedErroredParts = new string[] { "PythiaSignatureHelpProvider" };
        var hasUnexpectedErroredParts = erroredParts.Any(part => !expectedErroredParts.Contains(part));

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
                logger.LogError($"Encountered errors in the MEF composition:{Environment.NewLine}{ex.ErrorsAsString}");
                throw;
            }
        }
    }
}
