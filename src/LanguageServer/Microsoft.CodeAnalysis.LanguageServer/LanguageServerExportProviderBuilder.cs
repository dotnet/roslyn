// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.LanguageServer.Logging;
using Microsoft.CodeAnalysis.LanguageServer.Services;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.CodeAnalysis.LanguageServer;

internal sealed class LanguageServerExportProviderBuilder : ExportProviderBuilder
{
    private readonly ILogger<ExportProviderBuilder> _logger;

    // For testing purposes, track the last cache write task.
    private static Task? s_cacheWriteTask;

    private LanguageServerExportProviderBuilder(
        ImmutableArray<string> assemblyPaths,
        Resolver resolver,
        string cacheDirectory,
        string catalogPrefix,
        ImmutableArray<string> expectedErrorParts,
        ILoggerFactory loggerFactory)
        : base(assemblyPaths, resolver, cacheDirectory, catalogPrefix, expectedErrorParts)
    {
        _logger = loggerFactory.CreateLogger<ExportProviderBuilder>();
    }

    public static async Task<ExportProvider> CreateExportProviderAsync(
        ExtensionAssemblyManager extensionManager,
        IAssemblyLoader assemblyLoader,
        string? devKitDependencyPath,
        string cacheDirectory,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var baseDirectory = AppContext.BaseDirectory;

        // Load any Roslyn assemblies from the extension directory
        using var _ = ArrayBuilder<string>.GetInstance(out var assemblyPathsBuilder);
        assemblyPathsBuilder.AddRange(Directory.EnumerateFiles(baseDirectory, "Microsoft.CodeAnalysis*.dll"));
        assemblyPathsBuilder.AddRange(Directory.EnumerateFiles(baseDirectory, "Microsoft.ServiceHub*.dll"));

        // DevKit assemblies are not shipped in the main language server folder
        // and not included in ExtensionAssemblyPaths (they get loaded into the default ALC).
        // So manually add them to the MEF catalog here.
        if (devKitDependencyPath != null)
            assemblyPathsBuilder.AddRange(devKitDependencyPath);

        // Add the extension assemblies to the MEF catalog.
        assemblyPathsBuilder.AddRange(extensionManager.ExtensionAssemblyPaths);

        // Create a MEF resolver that can resolve assemblies in the extension contexts.
        var builder = new LanguageServerExportProviderBuilder(
            assemblyPathsBuilder.ToImmutableAndClear(),
            new Resolver(assemblyLoader),
            cacheDirectory,
            catalogPrefix: "c#-languageserver",
            expectedErrorParts: ["CSharpMapCodeService", "PythiaSignatureHelpProvider", "CopilotSemanticSearchQueryExecutor"],
            loggerFactory);
        var exportProvider = await builder.CreateExportProviderAsync(cancellationToken);

        // Also add the ExtensionAssemblyManager so it will be available for the rest of the composition.
        exportProvider.GetExportedValue<ExtensionAssemblyManagerMefProvider>().SetMefExtensionAssemblyManager(extensionManager);

        // Immediately set the logger factory, so that way it'll be available for the rest of the composition
        exportProvider.GetExportedValue<ServerLoggerFactory>().SetFactory(loggerFactory);

        return exportProvider;
    }

    protected override void LogError(string message)
        => _logger.LogError(message);

    protected override void LogTrace(string message)
        => _logger.LogTrace(message);

    protected override Task<ExportProvider> CreateExportProviderAsync(CancellationToken cancellationToken)
    {
        // Clear any previous cache write task, so that it is easy to discern whether
        // a cache write was attempted.
        s_cacheWriteTask = null;

        return base.CreateExportProviderAsync(cancellationToken);
    }

    protected override Task WriteCompositionCacheAsync(string compositionCacheFile, CompositionConfiguration config, CancellationToken cancellationToken)
    {
        s_cacheWriteTask = base.WriteCompositionCacheAsync(compositionCacheFile, config, cancellationToken);

        return s_cacheWriteTask;
    }

    protected override void PerformCacheDirectoryCleanup(DirectoryInfo directoryInfo, CancellationToken cancellationToken)
    {
        // No cache directory cleanup is needed for the language server.
    }

    internal static class TestAccessor
    {
#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
        public static Task? GetCacheWriteTask() => s_cacheWriteTask;
#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods
    }
}
