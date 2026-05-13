// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Frozen;
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

    private static readonly FrozenSet<string> s_dllsToExcludeFromMef = FrozenSet.ToFrozenSet(
    [
        // These DLLs are part of Razor, but should only be in their MEF composition not ours
        "Microsoft.CodeAnalysis.Razor.Workspaces.dll",
        "Microsoft.CodeAnalysis.Remote.Razor.dll",

        // This is a runtime dependency of Remote.Razor, but its host-layer exports belong to the remote host
        // composition and conflict with the language server's workspace services if included here.
        "Microsoft.CodeAnalysis.Remote.ServiceHub.dll",
    ], StringComparer.OrdinalIgnoreCase);

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
        // Load Roslyn assemblies from the language server directory.
        using var _ = ArrayBuilder<string>.GetInstance(out var assemblyPathsBuilder);

        // Don't catch IO exceptions as it's better to fail to build the catalog than give back
        // a partial catalog that will surely blow up later.
        assemblyPathsBuilder.AddRange(FilterFiles(baseDirectory, "Microsoft.CodeAnalysis*.dll"));
        assemblyPathsBuilder.AddRange(FilterFiles(baseDirectory, "Microsoft.ServiceHub*.dll"));

        // The Razor extension ships with Roslyn and so needs to be in our MEF composition
        assemblyPathsBuilder.Add(Path.Combine(baseDirectory, "Microsoft.VisualStudioCode.RazorExtension.dll"));

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

    private static IEnumerable<string> FilterFiles(string baseDirectory, string filter)
    {
        foreach (var file in Directory.EnumerateFiles(baseDirectory, filter))
        {
            if (!s_dllsToExcludeFromMef.Contains(Path.GetFileName(file)))
                yield return file;
        }
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
