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
    private static Task? s_cacheWriteTask_forTestingPurposesOnly;

    private LanguageServerExportProviderBuilder(
        ImmutableArray<string> assemblyPaths,
        Resolver resolver,
        string cacheDirectory,
        string catalogPrefix,
        ILoggerFactory loggerFactory)
        : base(assemblyPaths, resolver, cacheDirectory, catalogPrefix)
    {
        _logger = loggerFactory.CreateLogger<ExportProviderBuilder>();
    }

    public static async Task<ExportProvider> CreateExportProviderAsync(
        string baseDirectory,
        ExtensionAssemblyManager extensionManager,
        IAssemblyLoader assemblyLoader,
        string? devKitDependencyPath,
        string cacheDirectory,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        // Load any Roslyn assemblies from the extension directory
        using var _ = ArrayBuilder<string>.GetInstance(out var assemblyPathsBuilder);

        // Don't catch IO exceptions as it's better to fail to build the catalog than give back
        // a partial catalog that will surely blow up later.
        assemblyPathsBuilder.AddRange(Directory.EnumerateFiles(baseDirectory, "Microsoft.CodeAnalysis*.dll"));
        assemblyPathsBuilder.AddRange(Directory.EnumerateFiles(baseDirectory, "Microsoft.ServiceHub*.dll"));

        // DevKit assemblies are not shipped in the main language server folder
        // and not included in ExtensionAssemblyPaths (they get loaded into the default ALC).
        // So manually add them to the MEF catalog here.
        if (devKitDependencyPath != null)
            assemblyPathsBuilder.Add(devKitDependencyPath);

        // Add the extension assemblies to the MEF catalog.
        assemblyPathsBuilder.AddRange(extensionManager.ExtensionAssemblyPaths);

        // Create a MEF resolver that can resolve assemblies in the extension contexts.
        var builder = new LanguageServerExportProviderBuilder(
            assemblyPathsBuilder.ToImmutableAndClear(),
            new Resolver(assemblyLoader),
            cacheDirectory,
            catalogPrefix: "c#-languageserver",
            loggerFactory);
        var exportProvider = await builder.CreateExportProviderAsync(cancellationToken);

        // Also add the ExtensionAssemblyManager so it will be available for the rest of the composition.
        exportProvider.GetExportedValue<ExtensionAssemblyManagerMefProvider>().SetMefExtensionAssemblyManager(extensionManager);

        // Immediately set the logger factory, so that way it'll be available for the rest of the composition
        exportProvider.GetExportedValue<ServerLoggerFactory>().SetFactory(loggerFactory);

        return exportProvider;
    }

    protected override void LogError(string message, Exception exception)
        => _logger.LogError(exception, message);

    protected override void LogTrace(string message)
        => _logger.LogTrace(message);

    protected override Task<ExportProvider> CreateExportProviderAsync(CancellationToken cancellationToken)
    {
        // Clear any previous cache write task, so that it is easy to discern whether
        // a cache write was attempted.
        s_cacheWriteTask_forTestingPurposesOnly = null;

        return base.CreateExportProviderAsync(cancellationToken);
    }

    protected override bool ContainsUnexpectedErrors(IEnumerable<string> erroredParts)
    {
        // Verify that we have exactly the MEF errors that we expect.  If we have less or more this needs to be updated to assert the expected behavior.
        var expectedErrorPartsSet = new HashSet<string>(["CSharpMapCodeService", "PythiaSignatureHelpProvider", "CopilotSemanticSearchQueryExecutor"]);
        var hasUnexpectedErroredParts = erroredParts.Any(part => !expectedErrorPartsSet.Contains(part));

        return hasUnexpectedErroredParts;
    }

    protected override Task WriteCompositionCacheAsync(string compositionCacheFile, CompositionConfiguration config, CancellationToken cancellationToken)
    {
        s_cacheWriteTask_forTestingPurposesOnly = base.WriteCompositionCacheAsync(compositionCacheFile, config, cancellationToken);

        return s_cacheWriteTask_forTestingPurposesOnly;
    }

    protected override void PerformCacheDirectoryCleanup(DirectoryInfo directoryInfo, CancellationToken cancellationToken)
    {
        // No cache directory cleanup is needed for the language server.
    }

    internal static class TestAccessor
    {
#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
        public static Task? GetCacheWriteTask() => s_cacheWriteTask_forTestingPurposesOnly;
#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods
    }
}
