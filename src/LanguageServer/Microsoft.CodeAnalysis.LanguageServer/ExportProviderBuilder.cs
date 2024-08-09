// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.LanguageServer.Logging;
using Microsoft.CodeAnalysis.LanguageServer.Services;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Composition;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer;

internal sealed class ExportProviderBuilder
{
    public static async Task<ExportProvider> CreateExportProviderAsync(
        ExtensionAssemblyManager extensionManager,
        IAssemblyLoader assemblyLoader,
        string? devKitDependencyPath,
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

        logger.LogTrace($"Composing MEF catalog using:{Environment.NewLine}{string.Join($"    {Environment.NewLine}", assemblyPaths)}.");

        // Create a MEF resolver that can resolve assemblies in the extension contexts.
        var resolver = new Resolver(assemblyLoader);

        var discovery = PartDiscovery.Combine(
            resolver,
            new AttributedPartDiscovery(resolver, isNonPublicSupported: true), // "NuGet MEF" attributes (Microsoft.Composition)
            new AttributedPartDiscoveryV1(resolver));

        // TODO - we should likely cache the catalog so we don't have to rebuild it every time.
        var catalog = ComposableCatalog.Create(resolver)
            .AddParts(await discovery.CreatePartsAsync(assemblyPaths))
            .WithCompositionService(); // Makes an ICompositionService export available to MEF parts to import

        // Assemble the parts into a valid graph.
        var config = CompositionConfiguration.Create(catalog);

        // Verify we only have expected errors.
        ThrowOnUnexpectedErrors(config, catalog, logger);

        // Prepare an ExportProvider factory based on this graph.
        var exportProviderFactory = config.CreateExportProviderFactory();

        // Create an export provider, which represents a unique container of values.
        // You can create as many of these as you want, but typically an app needs just one.
        var exportProvider = exportProviderFactory.CreateExportProvider();

        // Immediately set the logger factory, so that way it'll be available for the rest of the composition
        exportProvider.GetExportedValue<ServerLoggerFactory>().SetFactory(loggerFactory);

        // Also add the ExtensionAssemblyManager so it will be available for the rest of the composition.
        exportProvider.GetExportedValue<ExtensionAssemblyManagerMefProvider>().SetMefExtensionAssemblyManager(extensionManager);

        return exportProvider;
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
        if (erroredParts.Count() != expectedErroredParts.Length || !erroredParts.All(part => expectedErroredParts.Contains(part)) || !catalog.DiscoveredParts.DiscoveryErrors.IsEmpty)
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
