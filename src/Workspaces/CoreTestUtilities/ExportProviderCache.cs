// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

        // Cache the catalog and export provider factory for DesktopMefHostServices.DefaultAssemblies
        private static readonly ComposableCatalog s_desktopHostCatalog =
            CreateAssemblyCatalog(DesktopMefHostServices.DefaultAssemblies);

        private static readonly IExportProviderFactory s_desktopHostExportProviderFactory =
            CreateExportProviderFactory(s_desktopHostCatalog);

        // Cache the catalog and export provider factory for RoslynServices.RemoteHostAssemblies
        private static readonly ComposableCatalog s_remoteHostCatalog =
            CreateAssemblyCatalog(RoslynServices.RemoteHostAssemblies);

        private static readonly IExportProviderFactory s_remoteHostExportProviderFactory =
            CreateExportProviderFactory(s_remoteHostCatalog);

        private static bool _enabled;

        private static ExportProvider _currentExportProvider;
        private static ComposableCatalog _expectedCatalog;
        private static ExportProvider _expectedProviderForCatalog;

        internal static bool EnabledViaUseExportProviderAttributeOnly
        {
            get
            {
                return _enabled;
            }

            set
            {
                _enabled = value;
                if (!_enabled)
                {
                    _currentExportProvider = null;
                    _expectedCatalog = null;
                    _expectedProviderForCatalog = null;
                }
            }
        }

        internal static ExportProvider ExportProviderForCleanup => _currentExportProvider;

        public static ComposableCatalog CreateAssemblyCatalog(Assembly assembly)
        {
            return CreateAssemblyCatalog(SpecializedCollections.SingletonEnumerable(assembly));
        }

        public static ComposableCatalog CreateAssemblyCatalog(IEnumerable<Assembly> assemblies, Resolver resolver = null)
        {
            if (assemblies is ImmutableArray<Assembly> assembliesArray)
            {
                if (s_defaultHostCatalog != null && assembliesArray == MefHostServices.DefaultAssemblies)
                {
                    return s_defaultHostCatalog;
                }
                else if (s_desktopHostCatalog != null && assembliesArray == DesktopMefHostServices.DefaultAssemblies)
                {
                    return s_desktopHostCatalog;
                }
                else if (s_remoteHostCatalog != null && assembliesArray == RoslynServices.RemoteHostAssemblies)
                {
                    return s_remoteHostCatalog;
                }
            }

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

        public static IExportProviderFactory CreateExportProviderFactory(ComposableCatalog catalog)
        {
            if (s_defaultHostExportProviderFactory != null && catalog == s_defaultHostCatalog)
            {
                return s_defaultHostExportProviderFactory;
            }
            else if (s_desktopHostExportProviderFactory != null && catalog == s_desktopHostCatalog)
            {
                return s_desktopHostExportProviderFactory;
            }
            else if (s_remoteHostExportProviderFactory != null && catalog == s_remoteHostCatalog)
            {
                return s_remoteHostExportProviderFactory;
            }

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

            public ExportProvider CreateExportProvider()
            {
                if (!_enabled)
                {
                    throw new InvalidOperationException($"{nameof(ExportProviderCache)} may only be used from tests marked with {nameof(UseExportProviderAttribute)}");
                }

                var expectedCatalog = Interlocked.CompareExchange(ref _expectedCatalog, _catalog, null) ?? _catalog;
                if (expectedCatalog != _catalog)
                {
                    throw new InvalidOperationException($"Only one {nameof(ExportProvider)} can be created for a single test.");
                }

                var expected = _expectedProviderForCatalog;
                if (expected == null)
                {
                    expected = _exportProviderFactory.CreateExportProvider();
                    expected = Interlocked.CompareExchange(ref _expectedProviderForCatalog, expected, null) ?? expected;
                    Interlocked.CompareExchange(ref _currentExportProvider, expected, null);
                }

                var exportProvider = _currentExportProvider;
                if (exportProvider != expected)
                {
                    throw new InvalidOperationException($"Only one {nameof(ExportProvider)} can be created in the context of a single test.");
                }

                return exportProvider;
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
