// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.AspNetCore.Internal.EmbeddedLanguages;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.Composition;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote;

/// <summary>
/// Manages remote workspaces. Currently supports only a single, primary workspace of kind <see
/// cref="WorkspaceKind.RemoteWorkspace"/>. In future it should support workspaces of all kinds.
/// </summary>
internal class RemoteWorkspaceManager
{
    internal static readonly ImmutableArray<Assembly> RemoteHostAssemblies =
        MefHostServices.DefaultAssemblies
            .Add(typeof(AspNetCoreEmbeddedLanguageClassifier).Assembly)
            .Add(typeof(BrokeredServiceBase).Assembly)
            .Add(typeof(RazorAnalyzerAssemblyResolver).Assembly)
            .Add(typeof(RemoteWorkspacesResources).Assembly);

    /// <summary>
    /// Default workspace manager used by the product. Tests may specify a custom <see
    /// cref="RemoteWorkspaceManager"/> in order to override workspace services.
    /// </summary>
    /// <remarks>
    /// The general thinking behind these timings is that we don't want to be too aggressive constantly waking up
    /// and cleaning purging items from the cache.  But we also don't want to wait an excessive amount of time,
    /// allowing it to get too full.
    /// <para>
    /// Also note that the asset cache will not remove items associated with the <see
    /// cref="Workspace.CurrentSolution"/> of the workspace it is created against (as well as any recent in-flight
    /// solutions).  This ensures that the assets associated with the solution that most closely corresponds to what
    /// the user is working with will stay pinned on the remote side and not get purged just because the user
    /// stopped interactive for a while.  This ensures the next sync (which likely overlaps heavily with the current
    /// solution) will not force the same assets to be resent.
    /// </para>
    /// <list type="bullet">
    /// <item>CleanupInterval=30s gives what feels to be a reasonable non-aggressive amount of time to let the cache
    /// do its job, while also making sure several times a minute it is scanned for things that can be
    /// dropped.</item>
    /// <item>PurgeAfter=1m effectively states that an item will be dumped from the cache if not used in the last
    /// minute.  This seems reasonable for keeping around all the parts of the current solutions in use, while
    /// allowing values from the past, or values removed from the solution to not persist too long.</item>
    /// <item>GcAfter=1m means that we'll force some GCs to happen after that amount of time of *non-activity*.  In
    /// other words, as long as OOP is being touched for operations, we will avoid doing the GCs.
    /// </item>
    /// </list>
    /// </remarks>
    internal static readonly RemoteWorkspaceManager Default = new(
        workspace => new SolutionAssetCache(workspace, cleanupInterval: TimeSpan.FromSeconds(30), purgeAfter: TimeSpan.FromMinutes(1)));

    private readonly RemoteWorkspace _workspace;
    internal readonly SolutionAssetCache SolutionAssetCache;

    public RemoteWorkspaceManager(Func<RemoteWorkspace, SolutionAssetCache> createAssetCache)
        : this(createAssetCache, CreatePrimaryWorkspace())
    {
    }

    public RemoteWorkspaceManager(
        Func<RemoteWorkspace, SolutionAssetCache> createAssetCache,
        RemoteWorkspace workspace)
    {
        _workspace = workspace;
        SolutionAssetCache = createAssetCache(workspace);
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

        return new RemoteWorkspace(VisualStudioMefHostServices.Create(exportProvider));
    }

    public RemoteWorkspace GetWorkspace() => _workspace;

    /// <summary>
    /// Not ideal that we exposing the workspace solution, while not ensuring it stays alive for other calls using
    /// the same <paramref name="solutionChecksum"/>). However, this is used by Pythia/Razor/UnitTesting which all
    /// assume they can get that solution instance and use as desired by them.
    /// </summary>
    [Obsolete("Use RunServiceAsync (that is passed a Solution) instead", error: false)]
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

        public Assembly LoadAssembly(string assemblyFullName, string? codeBasePath)
        {
            var assemblyName = new AssemblyName(assemblyFullName);
            if (!string.IsNullOrEmpty(codeBasePath))
            {
#pragma warning disable SYSLIB0044 // https://github.com/dotnet/roslyn/issues/71510
                assemblyName.CodeBase = codeBasePath;
#pragma warning restore SYSLIB0044
            }

            return LoadAssembly(assemblyName);
        }
    }
}
