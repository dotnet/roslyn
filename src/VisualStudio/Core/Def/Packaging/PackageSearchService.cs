// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Elfie.Model;
using Microsoft.CodeAnalysis.Elfie.Model.Structures;
using Microsoft.CodeAnalysis.Elfie.Model.Tree;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.Internal.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.Settings;
using static System.FormattableString;
using VSShell = Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Packaging
{
    /// <summary>
    /// A service which enables searching for packages matching certain criteria.
    /// It works against an <see cref="Microsoft.CodeAnalysis.Elfie"/> database to find results.
    /// 
    /// This implementation also spawns a task which will attempt to keep that database up to
    /// date by downloading patches on a daily basis.
    /// </summary>
    internal partial class PackageSearchService : ForegroundThreadAffinitizedObject, IPackageSearchService, IDisposable
    {
        private ConcurrentDictionary<string, AddReferenceDatabase> _sourceToDatabase = new ConcurrentDictionary<string, AddReferenceDatabase>();

        public PackageSearchService(VSShell.SVsServiceProvider serviceProvider, IPackageInstallerService installerService)
            : this(installerService, 
                   CreateRemoteControlService(serviceProvider),
                   new LogService((IVsActivityLog)serviceProvider.GetService(typeof(SVsActivityLog))),
                   new DelayService(),
                   new IOService(),
                   new PatchService(),
                   new DatabaseFactoryService(),
                   new ShellSettingsManager(serviceProvider).GetApplicationDataFolder(ApplicationDataFolder.LocalSettings),
                   // swallow all exceptions
                   e => true,
                   new CancellationTokenSource())
        {
            installerService.PackageSourcesChanged += OnPackageSourcesChanged;
            OnPackageSourcesChanged(this, EventArgs.Empty);
        }

        private static IPackageSearchRemoteControlService CreateRemoteControlService(VSShell.SVsServiceProvider serviceProvider)
        {
            var vsService = serviceProvider.GetService(typeof(SVsRemoteControlService));
            if (vsService == null)
            {
                // If we can't access the file update service, then there's nothing we can do.
                return null;
            }

            return new RemoteControlService(vsService);
        }

        /// <summary>
        /// For testing purposes only.
        /// </summary>
        internal PackageSearchService(
            IPackageInstallerService installerService,
            IPackageSearchRemoteControlService remoteControlService,
            IPackageSearchLogService logService,
            IPackageSearchDelayService delayService,
            IPackageSearchIOService ioService,
            IPackageSearchPatchService patchService,
            IPackageSearchDatabaseFactoryService databaseFactoryService,
            string localSettingsDirectory,
            Func<Exception, bool> swallowException,
            CancellationTokenSource cancellationTokenSource)
        {
            if (remoteControlService == null)
            {
                // If we can't access the file update service, then there's nothing we can do.
                return;
            }

            _installerService = installerService;
            _delayService = delayService;
            _ioService = ioService;
            _logService = logService;
            _remoteControlService = remoteControlService;
            _patchService = patchService;
            _databaseFactoryService = databaseFactoryService;
            _swallowException = swallowException;

            _cacheDirectoryInfo = new DirectoryInfo(Path.Combine(
                localSettingsDirectory, "PackageCache", string.Format(Invariant($"Format{_dataFormatVersion}"))));
            // _databaseFileInfo = new FileInfo(Path.Combine(_cacheDirectoryInfo.FullName, "NuGetCache.txt"));

            _cancellationTokenSource = cancellationTokenSource;
            _cancellationToken = _cancellationTokenSource.Token;
        }

        public IEnumerable<PackageWithTypeResult> FindPackagesWithType(
            string source, string name, int arity, CancellationToken cancellationToken)
        {
            //if (!StringComparer.OrdinalIgnoreCase.Equals(source, NugetOrgSource))
            //{
            //    // We only support searching nuget.org
            //    yield break;
            //}

            AddReferenceDatabase database;
            if (!_sourceToDatabase.TryGetValue(source, out database))
            {
                // Don't have a database to search.  
                yield break;
            }

            if (name == "var")
            {
                // never find anything named 'var'.
                yield break;
            }

            var query = new MemberQuery(name, isFullSuffix: true, isFullNamespace: false);

            var symbols = new PartialArray<Symbol>(100);

            if (query.TryFindMembers(database, ref symbols))
            {
                // Don't return nested types.  Currently their value does not seem worth
                // it given all the extra stuff we'd have to plumb through.  Namely 
                // going down the "using static" code path and whatnot.
                var types = new List<Symbol>(
                    from symbol in symbols
                    where this.IsType(symbol) && !this.IsType(symbol.Parent())
                    select symbol);

                var typesFromPackagesUsedInOtherProjects = new List<Symbol>();
                var typesFromPackagesNotUsedInOtherProjects = new List<Symbol>();

                foreach (var type in types)
                {
                    var packageName = type.PackageName.ToString();
                    if (_installerService.GetInstalledVersions(packageName).Any())
                    {
                        typesFromPackagesUsedInOtherProjects.Add(type);
                    }
                    else
                    {
                        typesFromPackagesNotUsedInOtherProjects.Add(type);
                    }
                }

                var result = new List<Symbol>();

                // We always returm types from packages that we've use elsewhere in the project.
                int? bestRank = null;
                foreach (var type in typesFromPackagesUsedInOtherProjects)
                {
                    if (type.PackageName.ToString() != MicrosoftAssemblyReferencesName)
                    {
                        var rank = GetRank(type);
                        bestRank = bestRank == null ? rank : Math.Max(bestRank.Value, rank);
                    }

                    yield return CreateResult(type);
                }

                // For all other hits include as long as the popularity is high enough.  
                // Popularity ranks are in powers of two.  So if two packages differ by 
                // one rank, then one is at least twice as popular as the next.  Two 
                // ranks would be four times as popular.  Three ranks = 8 times,  etc. 
                // etc.  We keep packages that within 1 rank of the best package we find.
                //
                // Note: we only do rankings for nuget packages.  Results from reference 
                // assemblies are always returned.
                foreach (var type in typesFromPackagesNotUsedInOtherProjects)
                {
                    if (type.PackageName.ToString() != MicrosoftAssemblyReferencesName)
                    {
                        var rank = GetRank(type);
                        bestRank = bestRank == null ? rank : Math.Max(bestRank.Value, rank);

                        if (Math.Abs(bestRank.Value - rank) > 1)
                        {
                            yield break;
                        }
                    }

                    yield return CreateResult(type);
                }
            }
        }

        private PackageWithTypeResult CreateResult(Symbol type)
        {
            var nameParts = new List<string>();
            GetFullName(nameParts, type.FullName.Parent);

            var packageName = type.PackageName.ToString();
            return new PackageWithTypeResult(
                isDesktopFramework: packageName == MicrosoftAssemblyReferencesName,
                packageName: packageName, 
                assemblyName: type.AssemblyNameWithoutExtension.ToString(),
                typeName: type.Name.ToString(), 
                containingNamespaceNames: nameParts);
        }

        private int GetRank(Symbol symbol)
        {
            Symbol rankingSymbol;
            int rank;
            if (!TryGetRankingSymbol(symbol, out rankingSymbol) ||
                !int.TryParse(rankingSymbol.Name.ToString(), out rank))
            {
                return 0;
            }

            return rank;
        }

        private bool TryGetRankingSymbol(Symbol symbol, out Symbol rankingSymbol)
        {
            for (var current = symbol; current.IsValid; current = current.Parent())
            {
                if (current.Type == SymbolType.Package)
                {
                    return TryGetRankingSymbolForPackage(current, out rankingSymbol);
                }
            }

            rankingSymbol = default(Symbol);
            return false;
        }

        private bool TryGetRankingSymbolForPackage(Symbol package, out Symbol rankingSymbol)
        {
            for (var child = package.FirstChild(); child.IsValid; child = child.NextSibling())
            {
                if (child.Type == SymbolType.PopularityRank)
                {
                    rankingSymbol = child;
                    return true;
                }
            }

            rankingSymbol = default(Symbol);
            return false;
        }

        private bool IsType(Symbol symbol)
        {
            return symbol.Type.IsType();
        }

        private void GetFullName(List<string> nameParts, Path8 path)
        {
            if (!path.IsEmpty)
            {
                GetFullName(nameParts, path.Parent);
                nameParts.Add(path.Name.ToString());
            }
        }
    }
}