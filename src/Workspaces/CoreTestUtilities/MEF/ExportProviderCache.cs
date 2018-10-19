﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.VisualStudio.Composition;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public static class ExportProviderCache
    {
        private static readonly PartDiscovery s_partDiscovery = CreatePartDiscovery(Resolver.DefaultInstance);

        // Cache the catalog and export provider factory for MefHostServices.DefaultAssemblies
        private static readonly ComposableCatalog s_defaultHostCatalog =
            CreateAssemblyCatalog(MefHostServices.DefaultAssemblies);

        private static readonly IExportProviderFactory s_defaultHostExportProviderFactory =
            CreateExportProviderFactory(s_defaultHostCatalog);

        // Cache the catalog and export provider factory for RoslynServices.RemoteHostAssemblies
        private static readonly ComposableCatalog s_remoteHostCatalog =
            CreateAssemblyCatalog(RoslynServices.RemoteHostAssemblies);

        private static readonly IExportProviderFactory s_remoteHostExportProviderFactory =
            CreateExportProviderFactory(s_remoteHostCatalog);

        private static bool _enabled;

        private static ExportProvider _currentExportProvider;
        private static ComposableCatalog _expectedCatalog;
        private static ExportProvider _expectedProviderForCatalog;

        internal static bool Enabled => _enabled;

        internal static ExportProvider ExportProviderForCleanup => _currentExportProvider;

        internal static void SetEnabled_OnlyUseExportProviderAttributeCanCall(bool value)
        {
            _enabled = value;
            if (!_enabled)
            {
                _currentExportProvider = null;
                _expectedCatalog = null;
                _expectedProviderForCatalog = null;
            }
        }

        public static ComposableCatalog GetOrCreateAssemblyCatalog(Assembly assembly)
        {
            return GetOrCreateAssemblyCatalog(SpecializedCollections.SingletonEnumerable(assembly));
        }

        public static ComposableCatalog GetOrCreateAssemblyCatalog(IEnumerable<Assembly> assemblies, Resolver resolver = null)
        {
            if (assemblies is ImmutableArray<Assembly> assembliesArray)
            {
                if (assembliesArray == MefHostServices.DefaultAssemblies)
                {
                    return s_defaultHostCatalog;
                }
                else if (assembliesArray == RoslynServices.RemoteHostAssemblies)
                {
                    return s_remoteHostCatalog;
                }
            }

            return CreateAssemblyCatalog(assemblies, resolver);
        }

        private static ComposableCatalog CreateAssemblyCatalog(IEnumerable<Assembly> assemblies, Resolver resolver = null)
        {
            var discovery = resolver == null ? s_partDiscovery : CreatePartDiscovery(resolver);

            // If we run CreatePartsAsync on the test thread we may deadlock since it'll schedule stuff back
            // on the thread.
            var parts = Task.Run(async () => await discovery.CreatePartsAsync(assemblies).ConfigureAwait(false)).Result;

            return ComposableCatalog.Create(resolver ?? Resolver.DefaultInstance).AddParts(parts);
        }

        public static ComposableCatalog CreateTypeCatalog(IEnumerable<Type> types, Resolver resolver = null)
        {
            var discovery = resolver == null ? s_partDiscovery : CreatePartDiscovery(resolver);

            // If we run CreatePartsAsync on the test thread we may deadlock since it'll schedule stuff back
            // on the thread.
            var parts = Task.Run(async () => await discovery.CreatePartsAsync(types).ConfigureAwait(false)).Result;

            return ComposableCatalog.Create(resolver ?? Resolver.DefaultInstance).AddParts(parts);
        }

        public static Resolver CreateResolver()
        {
            // simple assembly loader is stateless, so okay to share
            return new Resolver(SimpleAssemblyLoader.Instance);
        }

        public static PartDiscovery CreatePartDiscovery(Resolver resolver)
        {
            return PartDiscovery.Combine(new AttributedPartDiscoveryV1(resolver), new AttributedPartDiscovery(resolver, isNonPublicSupported: true));
        }

        public static ComposableCatalog WithParts(this ComposableCatalog @this, ComposableCatalog catalog)
        {
            return @this.AddParts(catalog.DiscoveredParts);
        }

        public static ComposableCatalog WithParts(this ComposableCatalog catalog, IEnumerable<Type> types)
        {
            return catalog.WithParts(CreateTypeCatalog(types));
        }

        public static ComposableCatalog WithParts(this ComposableCatalog catalog, params Type[] types)
        {
            return WithParts(catalog, (IEnumerable<Type>)types);
        }

        public static ComposableCatalog WithPart(this ComposableCatalog catalog, Type t)
        {
            return catalog.WithParts(CreateTypeCatalog(SpecializedCollections.SingletonEnumerable(t)));
        }

        /// <summary>
        /// Creates a <see cref="ComposableCatalog"/> derived from <paramref name="catalog"/>, but with all exported
        /// parts assignable to type <paramref name="t"/> removed from the catalog.
        /// </summary>
        public static ComposableCatalog WithoutPartsOfType(this ComposableCatalog catalog, Type t)
        {
            return catalog.WithoutPartsOfTypes(SpecializedCollections.SingletonEnumerable(t));
        }

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

        public static IExportProviderFactory GetOrCreateExportProviderFactory(ComposableCatalog catalog)
        {
            if (catalog == s_defaultHostCatalog)
            {
                return s_defaultHostExportProviderFactory;
            }
            else if (catalog == s_remoteHostCatalog)
            {
                return s_remoteHostExportProviderFactory;
            }

            return CreateExportProviderFactory(catalog);
        }

        private static IExportProviderFactory CreateExportProviderFactory(ComposableCatalog catalog)
        {
            var configuration = CompositionConfiguration.Create(catalog.WithCompositionService());
            var runtimeComposition = RuntimeComposition.CreateRuntimeComposition(configuration);
            var exportProviderFactory = runtimeComposition.CreateExportProviderFactory();
            return new SingleExportProviderFactory(catalog, exportProviderFactory);
        }

        private class SingleExportProviderFactory : IExportProviderFactory
        {
            private readonly ComposableCatalog _catalog;
            private readonly IExportProviderFactory _exportProviderFactory;

            public SingleExportProviderFactory(ComposableCatalog catalog, IExportProviderFactory exportProviderFactory)
            {
                _catalog = catalog;
                _exportProviderFactory = exportProviderFactory;
            }

            public ExportProvider GetOrCreateExportProvider()
            {
                if (!Enabled)
                {
                    // The [UseExportProvider] attribute on tests ensures that the pre- and post-conditions of methods
                    // in this type are met during test conditions.
                    throw new InvalidOperationException($"{nameof(ExportProviderCache)} may only be used from tests marked with {nameof(UseExportProviderAttribute)}");
                }

                var expectedCatalog = Interlocked.CompareExchange(ref _expectedCatalog, _catalog, null) ?? _catalog;
                RequireForSingleExportProvider(expectedCatalog == _catalog);

                var expected = _expectedProviderForCatalog;
                if (expected == null)
                {
                    expected = _exportProviderFactory.CreateExportProvider();
                    expected = Interlocked.CompareExchange(ref _expectedProviderForCatalog, expected, null) ?? expected;
                    Interlocked.CompareExchange(ref _currentExportProvider, expected, null);
                }

                var exportProvider = _currentExportProvider;
                RequireForSingleExportProvider(exportProvider == expected);

                return exportProvider;
            }

            ExportProvider IExportProviderFactory.CreateExportProvider()
            {
                // Currently this implementation deviates from the typical behavior of IExportProviderFactory. For the
                // duration of a single test, an instance of SingleExportProviderFactory will continue returning the
                // same ExportProvider instance each time this method is called.
                //
                // It may be clearer to refactor the implementation to only allow one call to CreateExportProvider in
                // the context of a single test. https://github.com/dotnet/roslyn/issues/25863
                return GetOrCreateExportProvider();
            }

            private static void RequireForSingleExportProvider(bool condition)
            {
                if (!condition)
                {
                    // The ExportProvider provides services that act as singleton instances in the context of an
                    // application (this include cases of multiple exports, where the 'singleton' is the list of all
                    // exports matching the contract). When reasoning about the behavior of test code, it is valuable to
                    // know service instances will be used in a consistent manner throughout the execution of a test,
                    // regardless of whether they are passed as arguments or obtained through requests to the
                    // ExportProvider.
                    //
                    // Restricting a test to a single ExportProvider guarantees that objects that *look* like singletons
                    // will *behave* like singletons for the duration of the test. Each test is expected to create and
                    // use its ExportProvider in a consistent manner.
                    //
                    // When this exception is thrown by a test, it typically means one of the following occurred:
                    //
                    // * A test failed to pass an ExportProvider via an optional argument to a method, resulting in the
                    //   method attempting to create a default ExportProvider which did not match the one assigned to
                    //   the test.
                    // * A test attempted to perform multiple test sequences in the context of a single test method,
                    //   rather than break up the test into distict tests for each case.
                    // * A test referenced different predefined ExportProvider instances within the context of a test.
                    //   Each test is expected to use the same ExportProvider throughout the test.
                    throw new InvalidOperationException($"Only one {nameof(ExportProvider)} can be created in the context of a single test.");
                }
            }
        }

        private class SimpleAssemblyLoader : IAssemblyLoader
        {
            public static readonly IAssemblyLoader Instance = new SimpleAssemblyLoader();

            public Assembly LoadAssembly(AssemblyName assemblyName)
            {
                return Assembly.Load(assemblyName);
            }

            public Assembly LoadAssembly(string assemblyFullName, string codeBasePath)
            {
                var assemblyName = new AssemblyName(assemblyFullName);
                if (!string.IsNullOrEmpty(codeBasePath))
                {
                    assemblyName.CodeBase = codeBasePath;
                }

                return this.LoadAssembly(assemblyName);
            }
        }
    }
}
