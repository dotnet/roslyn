// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.UnitTests.Remote;
using Microsoft.VisualStudio.Composition;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public static class ExportProviderCache
    {
        private static readonly PartDiscovery s_partDiscovery = CreatePartDiscovery(Resolver.DefaultInstance);

        private static readonly TestComposition s_defaultHostExportProviderComposition = TestComposition.Empty
            .AddAssemblies(MefHostServices.DefaultAssemblies)
            .AddParts(typeof(TestSerializerService.Factory));
        private static readonly Scope _localCompositionScope = new Scope("local");
        private static readonly Scope _remoteCompositionScope = new Scope("remote");

        internal static bool Enabled { get; private set; }

        internal static ExportProvider? LocalExportProviderForCleanup => _localCompositionScope.CurrentExportProvider;
        internal static ExportProvider? RemoteExportProviderForCleanup => _remoteCompositionScope.CurrentExportProvider;

        internal static void SetEnabled_OnlyUseExportProviderAttributeCanCall(bool value)
        {
            Enabled = value;
            if (!Enabled)
            {
                _localCompositionScope.Clear();
                _remoteCompositionScope.Clear();
            }
        }

        /// <summary>
        /// Use to create <see cref="IExportProviderFactory"/> for default instances of <see cref="MefHostServices"/>.
        /// </summary>
        public static IExportProviderFactory GetOrCreateExportProviderFactory(IEnumerable<Assembly> assemblies)
        {
            if (assemblies is ImmutableArray<Assembly> assembliesArray &&
                assembliesArray == MefHostServices.DefaultAssemblies)
            {
                return s_defaultHostExportProviderComposition.ExportProviderFactory;
            }

            return CreateExportProviderFactory(CreateAssemblyCatalog(assemblies), isRemoteHostComposition: false);
        }

        public static ComposableCatalog CreateAssemblyCatalog(IEnumerable<Assembly> assemblies, Resolver? resolver = null)
        {
            var discovery = resolver == null ? s_partDiscovery : CreatePartDiscovery(resolver);

            // If we run CreatePartsAsync on the test thread we may deadlock since it'll schedule stuff back
            // on the thread.
            var parts = Task.Run(async () => await discovery.CreatePartsAsync(assemblies).ConfigureAwait(false)).Result;

            return ComposableCatalog.Create(resolver ?? Resolver.DefaultInstance).AddParts(parts);
        }

        public static ComposableCatalog CreateTypeCatalog(IEnumerable<Type> types, Resolver? resolver = null)
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

        public static IExportProviderFactory CreateExportProviderFactory(ComposableCatalog catalog, bool isRemoteHostComposition)
        {
            var scope = isRemoteHostComposition ? _remoteCompositionScope : _localCompositionScope;
            var configuration = CompositionConfiguration.Create(catalog.WithCompositionService());
            var runtimeComposition = RuntimeComposition.CreateRuntimeComposition(configuration);
            var exportProviderFactory = runtimeComposition.CreateExportProviderFactory();

            return new SingleExportProviderFactory(scope, catalog, configuration, exportProviderFactory);
        }

        private sealed class SingleExportProviderFactory : IExportProviderFactory
        {
            private readonly Scope _scope;
            private readonly ComposableCatalog _catalog;
            private readonly CompositionConfiguration _configuration;
            private readonly IExportProviderFactory _exportProviderFactory;

            public SingleExportProviderFactory(Scope scope, ComposableCatalog catalog, CompositionConfiguration configuration, IExportProviderFactory exportProviderFactory)
            {
                _scope = scope;
                _catalog = catalog;
                _configuration = configuration;
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

                var expectedCatalog = Interlocked.CompareExchange(ref _scope.ExpectedCatalog, _catalog, null) ?? _catalog;
                RequireForSingleExportProvider(expectedCatalog == _catalog);

                var expected = _scope.ExpectedProviderForCatalog;
                if (expected == null)
                {
                    foreach (var errorCollection in _configuration.CompositionErrors)
                    {
                        foreach (var error in errorCollection)
                        {
                            foreach (var part in error.Parts)
                            {
                                foreach (var (importBinding, exportBindings) in part.SatisfyingExports)
                                {
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

                    expected = _exportProviderFactory.CreateExportProvider();
                    expected = Interlocked.CompareExchange(ref _scope.ExpectedProviderForCatalog, expected, null) ?? expected;
                    Interlocked.CompareExchange(ref _scope.CurrentExportProvider, expected, null);
                }

                var exportProvider = _scope.CurrentExportProvider;
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

            private void RequireForSingleExportProvider([DoesNotReturnIf(false)] bool condition)
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
                    // A test that validates remote services is allowed to create a couple of ExportProviders:
                    // one for local workspace and the other for the remote one. 
                    //
                    // When this exception is thrown by a test, it typically means one of the following occurred:
                    //
                    // * A test failed to pass an ExportProvider via an optional argument to a method, resulting in the
                    //   method attempting to create a default ExportProvider which did not match the one assigned to
                    //   the test.
                    // * A test attempted to perform multiple test sequences in the context of a single test method,
                    //   rather than break up the test into distinct tests for each case.
                    // * A test referenced different predefined ExportProvider instances within the context of a test.
                    //   Each test is expected to use the same ExportProvider throughout the test.
                    throw new InvalidOperationException($"Only one {_scope.Name} {nameof(ExportProvider)} can be created in the context of a single test.");
                }
            }
        }

        private sealed class Scope
        {
            public readonly string Name;
            public ExportProvider? CurrentExportProvider;
            public ComposableCatalog? ExpectedCatalog;
            public ExportProvider? ExpectedProviderForCatalog;

            public Scope(string name)
            {
                Name = name;
            }

            public void Clear()
            {
                CurrentExportProvider = null;
                ExpectedCatalog = null;
                ExpectedProviderForCatalog = null;
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
#pragma warning disable SYSLIB0044
                    assemblyName.CodeBase = codeBasePath;
#pragma warning restore SYSLIB0044
                }

                return this.LoadAssembly(assemblyName);
            }
        }
    }
}
