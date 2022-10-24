// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.CodeAnalysis.LanguageServer;

internal sealed class ExportProviderBuilder
{
    /// <summary>
    /// These assemblies won't necessarily be loaded when we want to run MEF discovery.
    /// We'll need to add them to the catalog manually.
    /// </summary>
    private static readonly ImmutableHashSet<string> AssembliesToDiscover = ImmutableHashSet.Create(
        "Microsoft.CodeAnalysis.LanguageServer.Protocol.dll",
        "Microsoft.CodeAnalysis.Features.dll",
        "Microsoft.CodeAnalysis.Workspaces.dll");

    public static async Task<ExportProvider> CreateExportProviderAsync()
    {
        var baseDirectory = AppContext.BaseDirectory;
        var assembliesWithFullPath = AssembliesToDiscover.Select(a => Path.Combine(baseDirectory, a));

        var discovery = PartDiscovery.Combine(
            new AttributedPartDiscovery(Resolver.DefaultInstance, isNonPublicSupported: true), // "NuGet MEF" attributes (Microsoft.Composition)
            new AttributedPartDiscoveryV1(Resolver.DefaultInstance));

        // TODO - we should likely cache the catalog so we don't have to rebuild it every time.
        var catalog = ComposableCatalog.Create(Resolver.DefaultInstance)
            .AddParts(await discovery.CreatePartsAsync(Assembly.GetExecutingAssembly()))
            .AddParts(await discovery.CreatePartsAsync(assembliesWithFullPath))
            .WithCompositionService(); // Makes an ICompositionService export available to MEF parts to import

        // Assemble the parts into a valid graph.
        var config = CompositionConfiguration.Create(catalog);
        _ = config.ThrowOnErrors();

        // Prepare an ExportProvider factory based on this graph.
        var exportProviderFactory = config.CreateExportProviderFactory();

        // Create an export provider, which represents a unique container of values.
        // You can create as many of these as you want, but typically an app needs just one.
        var exportProvider = exportProviderFactory.CreateExportProvider();

        // Obtain our first exported value
        return exportProvider;
    }
}
