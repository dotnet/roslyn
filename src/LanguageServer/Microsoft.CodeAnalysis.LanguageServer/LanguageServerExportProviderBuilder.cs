// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.LanguageServer.Logging;
using Microsoft.CodeAnalysis.LanguageServer.Services;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Composition;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer;

internal sealed class LanguageServerExportProviderBuilder
{
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

        var logger = loggerFactory.CreateLogger<ExportProviderBuilder>();

        // Create a MEF resolver that can resolve assemblies in the extension contexts.
        var args = new ExportProviderBuilder.ExportProviderCreationArguments(
            AssemblyPaths: assemblyPaths.ToImmutableArray(),
            Resolver: new Resolver(assemblyLoader),
            CacheDirectory: cacheDirectory,
            CatalogPrefix: "c#-languageserver",
            ExpectedErrorParts: ["PythiaSignatureHelpProvider"],
            PerformCleanup: false,
            LogError: text => logger.LogError(text),
            LogTrace: text => logger.LogTrace(text));

        var exportProvider = await ExportProviderBuilder.CreateExportProviderAsync(args, cancellationToken);

        // Also add the ExtensionAssemblyManager so it will be available for the rest of the composition.
        exportProvider.GetExportedValue<ExtensionAssemblyManagerMefProvider>().SetMefExtensionAssemblyManager(extensionManager);

        // Immediately set the logger factory, so that way it'll be available for the rest of the composition
        exportProvider.GetExportedValue<ServerLoggerFactory>().SetFactory(loggerFactory);

        return exportProvider;
    }
}
