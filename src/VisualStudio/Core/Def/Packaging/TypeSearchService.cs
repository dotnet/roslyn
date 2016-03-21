// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Elfie.Model;
using Microsoft.CodeAnalysis.HubServices;
using Microsoft.CodeAnalysis.HubServices.SymbolSearch;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.VisualStudio.LanguageServices.HubServices;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell.Settings;
using Newtonsoft.Json.Linq;
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
    internal partial class TypeSearchService : ITypeSearchService
    {
        private readonly IHubClient _hubClient;
        private readonly DirectoryInfo _cacheDirectoryInfo;
        private readonly IPackageInstallerService _installerService;

        public TypeSearchService(
            VSShell.SVsServiceProvider serviceProvider,
            IHubClient hubClient,
            IPackageInstallerService installerService)
        {
            _hubClient = hubClient;
            _installerService = installerService;

            var localSettingsDirectory = new ShellSettingsManager(serviceProvider).GetApplicationDataFolder(ApplicationDataFolder.LocalSettings);

            var _dataFormatVersion = AddReferenceDatabase.TextFileFormatVersion;
            _cacheDirectoryInfo = new DirectoryInfo(Path.Combine(
                localSettingsDirectory, "PackageCache", string.Format(Invariant($"Format{_dataFormatVersion}"))));

            installerService.PackageSourcesChanged += OnPackageSourcesChanged;
            OnPackageSourcesChanged(this, EventArgs.Empty);
        }

        private void OnPackageSourcesChanged(object sender, EventArgs e)
        {
            // Kick off a database update.  Wait a few seconds before starting so we don't
            // interfere too much with solution loading.
            var json = CreateBaseSearchRequest();

            var unused = _hubClient.SendRequestAsync(
                WellKnownHubServiceNames.SymbolSearch,
                nameof(SymbolSearchController.OnConfigurationChanged),
                json,
                CancellationToken.None);
        }

        private JObject CreateBaseSearchRequest()
        {
            var sources = _installerService.PackageSources;
            var json = new JObject(
                new JProperty(HubProtocolConstants.CacheDirectory, _cacheDirectoryInfo.FullName),
                new JProperty(HubProtocolConstants.PackageSources,
                    new JArray(sources.Select(ps => new JObject(new JProperty(ps.Name, ps.Source))))));
            return json;
        }

        public async Task<IEnumerable<PackageWithTypeResult>> FindPackagesWithTypeAsync(
            string source, string name, int arity, CancellationToken cancellationToken)
        {
            var json = CreateBaseSearchRequest();
            json.Add(new[]
            {
                new JProperty(HubProtocolConstants.Source, source),
                new JProperty(HubProtocolConstants.Name, name),
                new JProperty(HubProtocolConstants.Arity, arity)
            });

            var result = await _hubClient.SendRequestAsync(
                WellKnownHubServiceNames.SymbolSearch,
                nameof(SymbolSearchController.FindPackagesWithType),
                json,
                cancellationToken).ConfigureAwait(false);

            if (result == null)
            {
                return null;
            }

            var array = (JArray)result;
            var results = array.OfType<JObject>().Select(j => new PackageWithTypeResult(
                 (string)j.Property(HubProtocolConstants.PackageName),
                 (string)j.Property(HubProtocolConstants.TypeName),
                 (string)j.Property(HubProtocolConstants.Version),
                 GetContainingNamespaceNames(j),
                 (int)j.Property(HubProtocolConstants.Rank))).ToArray();

            return FilterPackageResults(results).ToArray();
        }

        private static string[] GetContainingNamespaceNames(JObject j)
        {
            return j.Property(HubProtocolConstants.ContainingNamespaceNames).Value.Select(t => (string)t).ToArray();
        }

        private IEnumerable<PackageWithTypeResult> FilterPackageResults(IEnumerable<PackageWithTypeResult> results)
        {
            var typesFromPackagesUsedInOtherProjects = new List<PackageWithTypeResult>();
            var typesFromPackagesNotUsedInOtherProjects = new List<PackageWithTypeResult>();

            foreach (var result in results)
            {
                var packageName = result.PackageName;
                if (_installerService.GetInstalledVersions(packageName).Any())
                {
                    typesFromPackagesUsedInOtherProjects.Add(result);
                }
                else
                {
                    typesFromPackagesNotUsedInOtherProjects.Add(result);
                }
            }

            // We always returm types from packages that we've use elsewhere in the project.
            int? bestRank = null;
            foreach (var result in typesFromPackagesUsedInOtherProjects)
            {
                yield return result;
            }

            // For all other hits include as long as the popularity is high enough.  
            // Popularity ranks are in powers of two.  So if two packages differ by 
            // one rank, then one is at least twice as popular as the next.  Two 
            // ranks would be four times as popular.  Three ranks = 8 times,  etc. 
            // etc.  We keep packages that within 1 rank of the best package we find.
            foreach (var result in typesFromPackagesNotUsedInOtherProjects)
            {
                var rank = result.Rank;
                bestRank = bestRank == null ? rank : Math.Max(bestRank.Value, rank);

                if (Math.Abs(bestRank.Value - rank) > 1)
                {
                    yield break;
                }

                yield return result;
            }
        }

        public async Task<IEnumerable<ReferenceAssemblyWithTypeResult>> FindReferenceAssembliesWithTypeAsync(
            string name, int arity, CancellationToken cancellationToken)
        {
            var json = CreateBaseSearchRequest();
            json.Add(new[]
            {
                new JProperty(HubProtocolConstants.Name, name),
                new JProperty(HubProtocolConstants.Arity, arity)
            });

            var result = await _hubClient.SendRequestAsync(
                WellKnownHubServiceNames.SymbolSearch,
                nameof(SymbolSearchController.FindReferenceAssembliesWithType),
                json,
                cancellationToken).ConfigureAwait(false);

            if (result == null)
            {
                return null;
            }

            var array = (JArray)result;
            var q = array.OfType<JObject>().Select(j => new ReferenceAssemblyWithTypeResult(
                 (string)j.Property(HubProtocolConstants.AssemblyName),
                 (string)j.Property(HubProtocolConstants.TypeName),
                 GetContainingNamespaceNames(j)));
            return q.ToArray();
        }
    }
}