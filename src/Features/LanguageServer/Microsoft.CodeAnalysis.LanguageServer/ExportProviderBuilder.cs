// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis.LanguageServer.Logging;
using Microsoft.CodeAnalysis.LanguageServer.Services;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.CodeAnalysis.LanguageServer;

internal sealed class ExportProviderBuilder
{
    public static async Task<ExportProvider> CreateExportProviderAsync(IEnumerable<string> extensionAssemblyPaths, string? sharedDependenciesPath, ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger<ExportProviderBuilder>();

        var baseDirectory = AppContext.BaseDirectory;

        var resolver = new Resolver(new CustomExportAssemblyLoader(baseDirectory));

        // Load any Roslyn assemblies from the extension directory
        var assemblyPaths = Directory.EnumerateFiles(baseDirectory, "Microsoft.CodeAnalysis*.dll");
        assemblyPaths = assemblyPaths.Concat(Directory.EnumerateFiles(baseDirectory, "Microsoft.ServiceHub*.dll"));

        // Temporarily explicitly load the dlls we want to add to the MEF composition.  This is due to a runtime bug
        // in the 7.0.4 runtime where the APIs MEF uses to load assemblies break with R2R assemblies.
        // See https://github.com/dotnet/runtime/issues/83526
        //
        // Once a newer version of the runtime is widely available, we can remove this.
        foreach (var path in assemblyPaths)
        {
            Assembly.LoadFrom(path);
        }

        var discovery = PartDiscovery.Combine(
            resolver,
            new AttributedPartDiscovery(resolver, isNonPublicSupported: true), // "NuGet MEF" attributes (Microsoft.Composition)
            new AttributedPartDiscoveryV1(resolver));

        var assemblies = new List<Assembly>()
        {
            typeof(ExportProviderBuilder).Assembly
        };

        foreach (var extensionAssemblyPath in extensionAssemblyPaths)
        {
            if (AssemblyLoadContextWrapper.TryLoadExtension(extensionAssemblyPath, sharedDependenciesPath, logger, out var extensionAssembly))
            {
                assemblies.Add(extensionAssembly);
            }
        }

        // TODO - we should likely cache the catalog so we don't have to rebuild it every time.
        var catalog = ComposableCatalog.Create(resolver)
            .AddParts(await discovery.CreatePartsAsync(assemblies))
            .AddParts(await discovery.CreatePartsAsync(assemblyPaths))
            .WithCompositionService(); // Makes an ICompositionService export available to MEF parts to import

        // Assemble the parts into a valid graph.
        var config = CompositionConfiguration.Create(catalog);

        // Verify we only have expected errors.
        ThrowOnUnexpectedErrors(config, logger);

        // Prepare an ExportProvider factory based on this graph.
        var exportProviderFactory = config.CreateExportProviderFactory();

        // Create an export provider, which represents a unique container of values.
        // You can create as many of these as you want, but typically an app needs just one.
        var exportProvider = exportProviderFactory.CreateExportProvider();

        // Immediately set the logger factory, so that way it'll be available for the rest of the composition
        exportProvider.GetExportedValue<ServerLoggerFactory>().SetFactory(loggerFactory);

        return exportProvider;
    }

    private static void ThrowOnUnexpectedErrors(CompositionConfiguration configuration, ILogger logger)
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
        if (erroredParts.Count() != expectedErroredParts.Length || !erroredParts.All(part => expectedErroredParts.Contains(part)))
        {
            try
            {
                configuration.ThrowOnErrors();
            }
            catch (CompositionFailedException ex)
            {
                // The ToString for the composition failed exception doesn't output a nice set of errors by default, so log it separately here.
                logger.LogError($"Encountered errors in the MEF composition:{Environment.NewLine}{ex.ErrorsAsString}");
                throw;
            }
        }
    }
}
