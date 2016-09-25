// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.CodeAnalysis.SymbolSearch;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Settings;
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
        private readonly ISymbolSearchUpdateEngine _updateEngine;

        private readonly IPackageInstallerService _installerService;
        private readonly string _localSettingsDirectory;

        [ImportingConstructor]
        public VisualStudioSymbolSearchService(
            VisualStudioWorkspaceImpl workspace,
            VSShell.SVsServiceProvider serviceProvider)
            : this(workspace,
                   workspace.Services.GetService<IPackageInstallerService>(),
                   new ShellSettingsManager(serviceProvider).GetApplicationDataFolder(ApplicationDataFolder.LocalSettings))
        {
        }

        /// <summary>
        /// For testing purposes only.
        /// </summary>
        internal VisualStudioSymbolSearchService(
            Workspace workspace,
            IPackageInstallerService installerService,
            string localSettingsDirectory) 
            : base(workspace, SymbolSearchOptions.Enabled,
                              SymbolSearchOptions.SuggestForTypesInReferenceAssemblies,
                              SymbolSearchOptions.SuggestForTypesInNuGetPackages)
        {
            _updateEngine = workspace.Services.GetService<ISymbolSearchUpdateEngine>();

            _installerService = installerService;
            _localSettingsDirectory = localSettingsDirectory;
        }

        protected override void EnableService()
        {
            // When our service is enabled hook up to package source changes.
            // We need to know when the list of sources have changed so we can
            // kick off the work to process them.
            _installerService.PackageSourcesChanged += OnPackageSourcesChanged;
        }

        private void OnPackageSourcesChanged(object sender, EventArgs e)
        {
            StartWorking();
        }

        protected override void StartWorking()
        {
            // Kick off a database update.  Wait a few seconds before starting so we don't
            // interfere too much with solution loading.
            var sources = _installerService.PackageSources;

            // Always pull down the nuget.org index.  It contains the MS reference assembly index
            // inside of it.
            var allSources = sources.Concat(new PackageSource(
                SymbolSearchUpdateEngine.NugetOrgSource, source: null));
            foreach (var source in allSources)
            {
                Task.Run(() => UpdateSourceInBackgroundAsync(source.Name));
            }
        }

        private Task UpdateSourceInBackgroundAsync(string sourceName)
        {
            return _updateEngine.UpdateContinuouslyAsync(sourceName, _localSettingsDirectory);
        }

        protected override void StopWorking()
        {
            _installerService.PackageSourcesChanged -= OnPackageSourcesChanged;
            _updateEngine.StopUpdates();
        }

        public IEnumerable<PackageWithTypeResult> FindPackagesWithType(
            string source, string name, int arity, CancellationToken cancellationToken)
        {
            var allPackagesWithType = _updateEngine.FindPackagesWithType(source, name, arity, cancellationToken);

            var typesFromPackagesUsedInOtherProjects = new List<PackageWithTypeResult>();
            var typesFromPackagesNotUsedInOtherProjects = new List<PackageWithTypeResult>();

            foreach (var packageWithType in allPackagesWithType)
            {
                var resultList = _installerService.GetInstalledVersions(packageWithType.PackageName).Any()
                    ?  typesFromPackagesUsedInOtherProjects
                    : typesFromPackagesNotUsedInOtherProjects;

                resultList.Add(packageWithType);
            }

            // We always returm types from packages that we've use elsewhere in the project.
            foreach (var type in typesFromPackagesUsedInOtherProjects)
            {
                yield return type;
            }

            // For all other hits include as long as the popularity is high enough.  
            // Popularity ranks are in powers of two.  So if two packages differ by 
            // one rank, then one is at least twice as popular as the next.  Two 
            // ranks would be four times as popular.  Three ranks = 8 times,  etc. 
            // etc.  We keep packages that within 1 rank of the best package we find.
            int? bestRank = null;
            foreach (var packageWithType in typesFromPackagesNotUsedInOtherProjects)
            {
                var rank = packageWithType.Rank;
                bestRank = bestRank == null ? rank : Math.Max(bestRank.Value, rank);

                if (Math.Abs(bestRank.Value - rank) > 1)
                {
                    yield break;
                }

                yield return packageWithType;
            }
        }

        public IEnumerable<ReferenceAssemblyWithTypeResult> FindReferenceAssembliesWithType(
            string name, int arity, CancellationToken cancellationToken)
        {
            return _updateEngine.FindReferenceAssembliesWithType(name, arity, cancellationToken);
        }
    }
}