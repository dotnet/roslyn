// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.Composition;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// Manages remote workspaces. Currently supports only a single, primary workspace of kind <see cref="WorkspaceKind.RemoteWorkspace"/>. 
    /// In future it should support workspaces of all kinds.
    /// </summary>
    internal class RemoteWorkspaceManager
    {
        /// <summary>
        /// Default workspace manager used by the product. Tests may specify a custom <see cref="RemoteWorkspaceManager"/>
        /// in order to override workspace services.
        /// </summary>
        internal static readonly RemoteWorkspaceManager Default = new RemoteWorkspaceManager(
            new SolutionAssetCache(cleanupInterval: TimeSpan.FromMinutes(1), purgeAfter: TimeSpan.FromMinutes(3), gcAfter: TimeSpan.FromMinutes(5)));

        internal static readonly ImmutableArray<Assembly> RemoteHostAssemblies =
            MefHostServices.DefaultAssemblies
                .Add(typeof(BrokeredServiceBase).Assembly)
                .Add(typeof(RemoteWorkspacesResources).Assembly);

        private readonly Lazy<RemoteWorkspace> _lazyPrimaryWorkspace;
        internal readonly SolutionAssetCache SolutionAssetCache;

        public RemoteWorkspaceManager(SolutionAssetCache assetCache)
        {
            _lazyPrimaryWorkspace = new Lazy<RemoteWorkspace>(CreatePrimaryWorkspace);
            SolutionAssetCache = assetCache;
        }

        private static ComposableCatalog CreateCatalog(ImmutableArray<Assembly> assemblies)
        {
            var resolver = new Resolver(SimpleAssemblyLoader.Instance);
            var discovery = new AttributedPartDiscovery(resolver, isNonPublicSupported: true);
            var parts = Task.Run(async () => await discovery.CreatePartsAsync(assemblies).ConfigureAwait(false)).GetAwaiter().GetResult();
            return ComposableCatalog.Create(resolver).AddParts(parts);
        }

        private static IExportProviderFactory CreateExportProviderFactory(ComposableCatalog catalog)
        {
            var configuration = CompositionConfiguration.Create(catalog);
            var runtimeComposition = RuntimeComposition.CreateRuntimeComposition(configuration);
            return runtimeComposition.CreateExportProviderFactory();
        }

        private static RemoteWorkspace CreatePrimaryWorkspace()
        {
            var catalog = CreateCatalog(RemoteHostAssemblies);
            var exportProviderFactory = CreateExportProviderFactory(catalog);
            var exportProvider = exportProviderFactory.CreateExportProvider();

            return new RemoteWorkspace(VisualStudioMefHostServices.Create(exportProvider), WorkspaceKind.RemoteWorkspace);
        }

        public virtual RemoteWorkspace GetWorkspace()
            => _lazyPrimaryWorkspace.Value;

        /// <summary>
        /// Not ideal that we exposing the workspace solution, while not ensuring it stays alive for other calls using
        /// the same <paramref name="solutionChecksum"/>). However, this is used by Pythia/Razor/UnitTesting which all
        /// assume they can get that solution instance and use as desired by them.
        /// </summary>
        [Obsolete("Use RunServiceAsync (that is passsed a Solution) instead", error: false)]
        public async ValueTask<Solution> GetSolutionAsync(ServiceBrokerClient client, Checksum solutionChecksum, CancellationToken cancellationToken)
        {
            var assetSource = new SolutionAssetSource(client);
            var workspace = GetWorkspace();
            var assetProvider = workspace.CreateAssetProvider(solutionChecksum, SolutionAssetCache, assetSource);

            var (solution, _) = await workspace.RunWithSolutionAsync(
                assetProvider,
                solutionChecksum,
                static _ => ValueTaskFactory.FromResult(false),
                cancellationToken).ConfigureAwait(false);

            return solution;
        }

        public async ValueTask<T> RunServiceAsync<T>(
            ServiceBrokerClient client,
            Checksum solutionChecksum,
            Func<Solution, ValueTask<T>> implementation,
            CancellationToken cancellationToken)
        {
            var assetSource = new SolutionAssetSource(client);
            var workspace = GetWorkspace();
            var assetProvider = workspace.CreateAssetProvider(solutionChecksum, SolutionAssetCache, assetSource);

            var (_, result) = await workspace.RunWithSolutionAsync(
                assetProvider,
                solutionChecksum,
                implementation,
                cancellationToken).ConfigureAwait(false);

            return result;
        }

        private sealed class SimpleAssemblyLoader : IAssemblyLoader
        {
            public static readonly IAssemblyLoader Instance = new SimpleAssemblyLoader();

            public Assembly LoadAssembly(AssemblyName assemblyName)
                => Assembly.Load(assemblyName);

            public Assembly LoadAssembly(string assemblyFullName, string codeBasePath)
            {
                var assemblyName = new AssemblyName(assemblyFullName);
                if (!string.IsNullOrEmpty(codeBasePath))
                {
                    assemblyName.CodeBase = codeBasePath;
                }

                return LoadAssembly(assemblyName);
            }
        }
    }
}
