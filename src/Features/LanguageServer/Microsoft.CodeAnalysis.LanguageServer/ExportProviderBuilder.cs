// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis.LanguageServer.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.CodeAnalysis.LanguageServer;

internal sealed class ExportProviderBuilder
{
    public static async Task<ExportProvider> CreateExportProviderAsync()
    {
        var baseDirectory = AppContext.BaseDirectory;

        // Load any Roslyn assemblies from the extension directory
        var assembliesToDiscover = Directory.EnumerateFiles(baseDirectory, "Microsoft.CodeAnalysis*.dll");

        var discovery = PartDiscovery.Combine(
            new AttributedPartDiscovery(Resolver.DefaultInstance, isNonPublicSupported: true), // "NuGet MEF" attributes (Microsoft.Composition)
            new AttributedPartDiscoveryV1(Resolver.DefaultInstance));

        // TODO - we should likely cache the catalog so we don't have to rebuild it every time.
        var catalog = ComposableCatalog.Create(Resolver.DefaultInstance)
            .AddParts(await discovery.CreatePartsAsync(Assembly.GetExecutingAssembly()).ConfigureAwait(false))
            .AddParts(await discovery.CreatePartsAsync(assembliesToDiscover).ConfigureAwait(false))
            .WithCompositionService(); // Makes an ICompositionService export available to MEF parts to import

        // Assemble the parts into a valid graph.
        var config = CompositionConfiguration.Create(catalog);

        // Verify we only have expected errors.
        ThrowOnUnexpectedErrors(config);

        // Prepare an ExportProvider factory based on this graph.
        var exportProviderFactory = config.CreateExportProviderFactory();

        // Create an export provider, which represents a unique container of values.
        // You can create as many of these as you want, but typically an app needs just one.
        var exportProvider = exportProviderFactory.CreateExportProvider();

        return exportProvider;
    }

    private static void ThrowOnUnexpectedErrors(CompositionConfiguration configuration)
    {
        // Verify that we have exactly the MEF errors that we expect.  If we have less or more this needs to be updated to assert the expected behavior.
        // Currently we are expecting the following:
        //     "----- CompositionError level 1 ------
        //     Microsoft.CodeAnalysis.CSharp.CodeRefactorings.AddMissingImports.CSharpAddMissingImportsRefactoringProvider.ctor(pasteTrackingService): expected exactly 1 export matching constraints:
        //         Contract name: Microsoft.CodeAnalysis.PasteTracking.IPasteTrackingService
        //         TypeIdentityName: Microsoft.CodeAnalysis.PasteTracking.IPasteTrackingService
        //     but found 0.
        //         part definition Microsoft.CodeAnalysis.CSharp.CodeRefactorings.AddMissingImports.CSharpAddMissingImportsRefactoringProvider

        //     Microsoft.CodeAnalysis.ExternalAccess.Pythia.PythiaSignatureHelpProvider.ctor(implementation): expected exactly 1 export matching constraints:
        //         Contract name: Microsoft.CodeAnalysis.ExternalAccess.Pythia.Api.IPythiaSignatureHelpProviderImplementation
        //         TypeIdentityName: Microsoft.CodeAnalysis.ExternalAccess.Pythia.Api.IPythiaSignatureHelpProviderImplementation
        //     but found 0.
        //         part definition Microsoft.CodeAnalysis.ExternalAccess.Pythia.PythiaSignatureHelpProvider
        var erroredParts = configuration.CompositionErrors.FirstOrDefault()?.SelectMany(error => error.Parts).Select(part => part.Definition.Type.Name) ?? Enumerable.Empty<string>();
        var expectedErroredParts = new string[] { "CSharpAddMissingImportsRefactoringProvider", "PythiaSignatureHelpProvider" };
        if (erroredParts.Count() != expectedErroredParts.Length || !erroredParts.All(part => expectedErroredParts.Contains(part)))
        {
            configuration.ThrowOnErrors();
        }
    }
}
