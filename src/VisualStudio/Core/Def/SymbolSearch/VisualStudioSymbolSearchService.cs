// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SymbolSearch;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Storage;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.Settings;
using Roslyn.Utilities;
using VSShell = Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.SymbolSearch
{
    /// <summary>
    /// A service which enables searching for packages matching certain criteria.
    /// It works against an <see cref="Microsoft.CodeAnalysis.Elfie"/> database to find results.
    /// 
    /// This implementation also spawns a task which will attempt to keep that database up to
    /// date by downloading patches on a daily basis.
    /// </summary>
    [ExportWorkspaceService(typeof(ISymbolSearchService), ServiceLayer.Host), Shared]
    internal partial class VisualStudioSymbolSearchService : AbstractDelayStartedService, ISymbolSearchService
    {
        private readonly SemaphoreSlim _gate = new(initialCount: 1);

        // Note: A remote engine is disposable as it maintains a connection with ServiceHub,
        // but we want to keep it alive until the VS is closed, so we don't dispose it.
        private ISymbolSearchUpdateEngine _lazyUpdateEngine;

        private readonly SVsServiceProvider _serviceProvider;
        private readonly IPackageInstallerService _installerService;

        private string _localSettingsDirectory;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioSymbolSearchService(
            IThreadingContext threadingContext,
            IAsynchronousOperationListenerProvider listenerProvider,
            VisualStudioWorkspaceImpl workspace,
            IGlobalOptionService globalOptions,
            VSShell.SVsServiceProvider serviceProvider)
            : base(threadingContext,
                   globalOptions,
                   workspace,
                   listenerProvider,
                   SymbolSearchGlobalOptionsStorage.Enabled,
                   ImmutableArray.Create(SymbolSearchOptionsStorage.SearchReferenceAssemblies, SymbolSearchOptionsStorage.SearchNuGetPackages))
        {
            _serviceProvider = serviceProvider;
            _installerService = workspace.Services.GetService<IPackageInstallerService>();
        }

        protected override async Task EnableServiceAsync(CancellationToken cancellationToken)
        {
            await this.ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            _localSettingsDirectory = new ShellSettingsManager(_serviceProvider).GetApplicationDataFolder(ApplicationDataFolder.LocalSettings);

            // When our service is enabled hook up to package source changes.
            // We need to know when the list of sources have changed so we can
            // kick off the work to process them.
            _installerService.PackageSourcesChanged += OnPackageSourcesChanged;

            // Kick off the initial work to pull down the nuget index.
            this.StartWorking();
        }

        private void OnPackageSourcesChanged(object sender, EventArgs e)
            => StartWorking();

        private void StartWorking()
        {
            // Always pull down the nuget.org index.  It contains the MS reference assembly index
            // inside of it.
            var cancellationToken = ThreadingContext.DisposalToken;
            Task.Run(() => UpdateSourceInBackgroundAsync(PackageSourceHelper.NugetOrgSourceName, cancellationToken), cancellationToken);
        }

        private async Task<ISymbolSearchUpdateEngine> GetEngineAsync(CancellationToken cancellationToken)
        {
            using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                return _lazyUpdateEngine ??= await SymbolSearchUpdateEngineFactory.CreateEngineAsync(
                    Workspace, FileDownloader.Factory.Instance, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task UpdateSourceInBackgroundAsync(string sourceName, CancellationToken cancellationToken)
        {
            var engine = await GetEngineAsync(cancellationToken).ConfigureAwait(false);
            await engine.UpdateContinuouslyAsync(sourceName, _localSettingsDirectory, cancellationToken).ConfigureAwait(false);
        }

        public async ValueTask<ImmutableArray<PackageWithTypeResult>> FindPackagesWithTypeAsync(
            string source, string name, int arity, CancellationToken cancellationToken)
        {
            var engine = await GetEngineAsync(cancellationToken).ConfigureAwait(false);
            var allPackagesWithType = await engine.FindPackagesWithTypeAsync(
                source, name, arity, cancellationToken).ConfigureAwait(false);

            return FilterAndOrderPackages(allPackagesWithType);
        }

        public async ValueTask<ImmutableArray<PackageWithAssemblyResult>> FindPackagesWithAssemblyAsync(
            string source, string assemblyName, CancellationToken cancellationToken)
        {
            var engine = await GetEngineAsync(cancellationToken).ConfigureAwait(false);
            var allPackagesWithAssembly = await engine.FindPackagesWithAssemblyAsync(
                source, assemblyName, cancellationToken).ConfigureAwait(false);

            return FilterAndOrderPackages(allPackagesWithAssembly);
        }

        private ImmutableArray<TPackageResult> FilterAndOrderPackages<TPackageResult>(
            ImmutableArray<TPackageResult> allPackages) where TPackageResult : PackageResult
        {
            // The ranking threshold under while we start aggressively filtering out packages if they don't have a high
            // enough rank.  Above this and we will always include the item as it's shown more than enough usage to
            // indicate it's a high value, highly used package.  Note: the reason for this is that some minor packages
            // include copies of types within them).  So we don't want to clutter the display with redundant duplicate
            // matches from rarely used packages that are extremely unlikely to be relevant.  Once the package is highly
            // used though, it's def likely that this could be a viable match.
            //
            // The 25 number was picked as it's equivalent to >25m downloads from nuget, which def seems a reasonable
            // signal that this is an important package.
            const int RankThreshold = 25;

            var packagesUsedInOtherProjects = new List<TPackageResult>();
            var packagesNotUsedInOtherProjects = new List<TPackageResult>();

            foreach (var package in allPackages)
            {
                var resultList = _installerService.GetInstalledVersions(package.PackageName).Any()
                    ? packagesUsedInOtherProjects
                    : packagesNotUsedInOtherProjects;

                resultList.Add(package);
            }

            var result = ArrayBuilder<TPackageResult>.GetInstance();

            // We always return types from packages that we've use elsewhere in the project.
            result.AddRange(packagesUsedInOtherProjects);

            // For all other hits include as long as the popularity is high enough.  
            // Popularity ranks are in powers of two.  So if two packages differ by 
            // one rank, then one is at least twice as popular as the next.  Two 
            // ranks would be four times as popular.  Three ranks = 8 times,  etc. 
            // etc.  We keep packages that within 1 rank of the best package we find.
            var bestRank = packagesUsedInOtherProjects.LastOrDefault()?.Rank;
            foreach (var packageWithType in packagesNotUsedInOtherProjects)
            {
                var rank = packageWithType.Rank;
                bestRank = bestRank == null ? rank : Math.Max(bestRank.Value, rank);

                if (rank < RankThreshold && Math.Abs(bestRank.Value - rank) > 1)
                    break;

                result.Add(packageWithType);
            }

            return result.ToImmutableAndFree();
        }

        public async ValueTask<ImmutableArray<ReferenceAssemblyWithTypeResult>> FindReferenceAssembliesWithTypeAsync(
            string name, int arity, CancellationToken cancellationToken)
        {
            var engine = await GetEngineAsync(cancellationToken).ConfigureAwait(false);
            return await engine.FindReferenceAssembliesWithTypeAsync(
                name, arity, cancellationToken).ConfigureAwait(false);
        }
    }
}
