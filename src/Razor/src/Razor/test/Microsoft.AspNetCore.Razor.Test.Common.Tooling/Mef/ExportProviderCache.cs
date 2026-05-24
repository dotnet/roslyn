// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.AspNetCore.Razor.Test.Common.Mef;

public static class ExportProviderCache
{
    private static readonly PartDiscovery s_partDiscovery = CreatePartDiscovery(Resolver.DefaultInstance);

    public static ComposableCatalog CreateAssemblyCatalog(IEnumerable<Assembly> assemblies, Resolver? resolver = null)
    {
        var discovery = resolver is null ? s_partDiscovery : CreatePartDiscovery(resolver);

        // If we run CreatePartsAsync on the test thread we may deadlock since it'll schedule stuff back
        // on the thread.
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
        var parts = Task.Run(async () => await discovery.CreatePartsAsync(assemblies).ConfigureAwait(false)).Result;
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits

        return ComposableCatalog.Create(resolver ?? Resolver.DefaultInstance).AddParts(parts);
    }

    public static ComposableCatalog CreateTypeCatalog(IEnumerable<Type> types, Resolver? resolver = null)
    {
        var discovery = resolver is null ? s_partDiscovery : CreatePartDiscovery(resolver);

        // If we run CreatePartsAsync on the test thread we may deadlock since it'll schedule stuff back
        // on the thread.
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
        var parts = Task.Run(async () => await discovery.CreatePartsAsync(types).ConfigureAwait(false)).Result;
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits

        return ComposableCatalog.Create(resolver ?? Resolver.DefaultInstance).AddParts(parts);
    }

    public static Resolver CreateResolver()
    {
        // simple assembly loader is stateless, so okay to share
        return new Resolver(SimpleAssemblyLoader.Instance);
    }

    public static PartDiscovery CreatePartDiscovery(Resolver resolver)
        => PartDiscovery.Combine(new AttributedPartDiscoveryV1(resolver), new AttributedPartDiscovery(resolver, isNonPublicSupported: true));

    public static ComposableCatalog WithParts(this ComposableCatalog catalog, IEnumerable<Type> types)
        => catalog.AddParts(CreateTypeCatalog(types).DiscoveredParts);

    /// <summary>
    /// Creates a <see cref="ComposableCatalog"/> derived from <paramref name="catalog"/>, but with all exported
    /// parts assignable to any type in <paramref name="types"/> removed from the catalog.
    /// </summary>
    public static ComposableCatalog WithoutPartsOfTypes(this ComposableCatalog catalog, IEnumerable<Type> types)
    {
        var parts = catalog.Parts.Where(composablePartDefinition => !IsExcludedPart(composablePartDefinition));
        return ComposableCatalog.Create(Resolver.DefaultInstance).AddParts(parts);

        bool IsExcludedPart(ComposablePartDefinition part)
        {
            return types.Any(excludedType => excludedType.IsAssignableFrom(part.Type));
        }
    }

    public static IExportProviderFactory CreateExportProviderFactory(ComposableCatalog catalog)
    {
        var configuration = CompositionConfiguration.Create(catalog.WithCompositionService());
        ValidateConfiguration(configuration);

        var runtimeComposition = RuntimeComposition.CreateRuntimeComposition(configuration);
        var exportProviderFactory = runtimeComposition.CreateExportProviderFactory();

        return exportProviderFactory;
    }

    private static void ValidateConfiguration(CompositionConfiguration configuration)
    {
        foreach (var errorCollection in configuration.CompositionErrors)
        {
            foreach (var error in errorCollection)
            {
                foreach (var part in error.Parts)
                {
                    foreach (var pair in part.SatisfyingExports)
                    {
                        var (importBinding, exportBindings) = (pair.Key, pair.Value);
                        if (exportBindings.Count <= 1)
                        {
                            // Ignore composition errors for missing parts
                            continue;
                        }

                        if (importBinding.ImportDefinition.Cardinality != ImportCardinality.ZeroOrMore)
                        {
                            // This failure occurs when a binding fails because multiple exports were
                            // provided but only a single one (at most) is expected. This typically occurs
                            // when a test ExportProvider is created with a mock implementation without
                            // first removing a value provided by default.
                            throw new InvalidOperationException(
                                "Failed to construct the MEF catalog for testing. Multiple exports were found for a part for which only one export is expected:" + Environment.NewLine
                                + error.Message);
                        }
                    }
                }
            }
        }
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
#pragma warning disable SYSLIB0044 // Type or member is obsolete
                assemblyName.CodeBase = codeBasePath;
#pragma warning restore SYSLIB0044 // Type or member is obsolete
            }

            return LoadAssembly(assemblyName);
        }
    }
}
