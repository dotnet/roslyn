using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.CodeAnalysis.Elfie.Model;
using Microsoft.CodeAnalysis.Elfie.Model.Structures;
using Microsoft.CodeAnalysis.Elfie.Model.Tree;
using Microsoft.CodeAnalysis.HubServices.SymbolSearch.Data;
using Microsoft.VsHub.ServiceModulesCommon;
using Microsoft.VsHub.Services;
using Newtonsoft.Json.Linq;

namespace Microsoft.CodeAnalysis.HubServices.SymbolSearch
{
    public partial class SymbolSearchController : JsonController
    {
        private static ConcurrentDictionary<string, AddReferenceDatabase> _sourceToDatabase = 
            new ConcurrentDictionary<string, AddReferenceDatabase>();

        private readonly ISymbolSearchDelayService _delayService;
        private readonly ISymbolSearchIOService _ioService;
        private readonly ISymbolSearchLogService _logService;
        private readonly ISymbolSearchRemoteControlService _remoteControlService;
        private readonly ISymbolSearchPatchService _patchService;
        private readonly ISymbolSearchDatabaseFactoryService _databaseFactoryService;
        private readonly Func<Exception, bool> _reportAndSwallowException;

        public SymbolSearchController()
            : this(new RemoteControlService(),
                   new LogService(ServiceModulesUtilities.GetService<ILogger>()),
                   new DelayService(),
                   new IOService(),
                   new PatchService(),
                   new DatabaseFactoryService(),
                   // Report all exceptions we encounter, but don't crash on them.
                   FatalError.ReportWithoutCrash,
                   new CancellationTokenSource())
        {
            ServiceModulesUtilities.GetService<IServiceLifetime>().Stopped += OnServiceStopped;
        }

        // For testing purposes.
        internal SymbolSearchController(
            ISymbolSearchRemoteControlService remoteControlService,
            ISymbolSearchLogService logService,
            ISymbolSearchDelayService delayService,
            ISymbolSearchIOService ioService,
            ISymbolSearchPatchService patchService,
            ISymbolSearchDatabaseFactoryService databaseFactoryService,
            Func<Exception, bool> reportAndSwallowException,
            CancellationTokenSource cancellationTokenSource)
        {
            _delayService = delayService;
            _ioService = ioService;
            _logService = logService;
            _remoteControlService = remoteControlService;
            _patchService = patchService;
            _databaseFactoryService = databaseFactoryService;
            _reportAndSwallowException = reportAndSwallowException;

            _cancellationTokenSource = cancellationTokenSource;
            _cancellationToken = _cancellationTokenSource.Token;
        }

        private void OnServiceStopped(object sender, EventArgs e)
        {
            // VSHub is stopping.  Stop all work we're currently doing.
            _cancellationTokenSource.Cancel();
            _sourceToDatabase.Clear();
        }

        private void LogInfo(string text) => _logService.LogInfo(text);

        private void LogException(Exception e, string text) => _logService.LogException(e, text);

        [HttpPost]
        [Route(WellKnownHubServiceNames.SymbolSearch + "/" + nameof(HubProtocolConstants.CancelOperation))]
        public new void CancelOperation(HubDataModel value)
        {
            base.CancelOperation(value);
        }

        [HttpPost]
        [Route(WellKnownHubServiceNames.SymbolSearch + "/" + nameof(OnConfigurationChanged))]
        public HttpResponseMessage OnConfigurationChanged(HubDataModel model)
        {
            return base.ProcessRequest<JObject>(model, (obj, c) =>
            {
                var cacheDirectory = (string)obj.Property(HubProtocolConstants.CacheDirectory);
                var sourceNames = obj.Property(HubProtocolConstants.PackageSources).Value
                                     .OfType<JObject>()
                                     .Select(o => o.Properties().First().Name);

                foreach (var sourceName in sourceNames)
                {
                    // Kick off tasks (in a fire and forget manner) to update each source.
                    var unused = UpdateSourceInBackgroundAsync(cacheDirectory, sourceName);
                }

                return new JObject();
            });
        }

        [HttpPost]
        [Route(WellKnownHubServiceNames.SymbolSearch + "/" + nameof(FindPackagesWithType))]
        public HttpResponseMessage FindPackagesWithType(HubDataModel model)
        {
            return ProcessRequest<JObject>(model, (obj, c) =>
            {
                var results = FindPackagesWithType(
                    (string)obj.Property(HubProtocolConstants.Source),
                    (string)obj.Property(HubProtocolConstants.Name),
                    (int)obj.Property(HubProtocolConstants.Arity),
                    c);

            return new JArray(results.Select(r => new JObject(
                new JProperty(HubProtocolConstants.Version, r.Version),
                new JProperty(HubProtocolConstants.TypeName, r.TypeName),
                new JProperty(HubProtocolConstants.PackageName, r.PackageName),
                new JProperty(HubProtocolConstants.Rank, r.Rank),
                new JProperty(HubProtocolConstants.ContainingNamespaceNames, new JArray(r.ContainingNamespaceNames.ToArray())))));
            });
        }

        private IEnumerable<PackageWithTypeResult> FindPackagesWithType(
            string source, string name, int arity, CancellationToken cancellationToken)
        {
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
                var types = FilterToViableTypes(symbols);

                foreach (var type in types)
                {
                    // Ignore any reference assembly results.
                    if (type.PackageName.ToString() != MicrosoftAssemblyReferencesName)
                    {
                        yield return CreateResult(database, type);
                    }
                }
            }
        }

        [HttpPost]
        [Route(WellKnownHubServiceNames.SymbolSearch + "/" + nameof(FindReferenceAssembliesWithType))]
        public HttpResponseMessage FindReferenceAssembliesWithType(HubDataModel model)
        {
            return ProcessRequest<JObject>(model, (obj, c) =>
            {
                var results = FindReferenceAssembliesWithType(
                    (string)obj.Property(HubProtocolConstants.Name),
                    (int)obj.Property(HubProtocolConstants.Arity),
                    c);

                return new JArray(results.Select(r => new JObject(
                    new JProperty(HubProtocolConstants.TypeName, r.TypeName),
                    new JProperty(HubProtocolConstants.AssemblyName, r.AssemblyName),
                    new JProperty(HubProtocolConstants.ContainingNamespaceNames, new JArray(r.ContainingNamespaceNames.ToArray())))));
            });
        }

        private IEnumerable<ReferenceAssemblyWithTypeResult> FindReferenceAssembliesWithType(
            string name, int arity, CancellationToken cancellationToken)
        {
            // Our reference assembly data is stored in the nuget.org DB.
            AddReferenceDatabase database;
            if (!_sourceToDatabase.TryGetValue(NugetOrgSource, out database))
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
                var types = FilterToViableTypes(symbols);

                foreach (var type in types)
                {
                    // Only look at reference assembly results.
                    if (type.PackageName.ToString() == MicrosoftAssemblyReferencesName)
                    {
                        var nameParts = new List<string>();
                        GetFullName(nameParts, type.FullName.Parent);
                        yield return new ReferenceAssemblyWithTypeResult(
                            type.AssemblyName.ToString(), type.Name.ToString(), containingNamespaceNames: nameParts);
                    }
                }
            }
        }

        private List<Symbol> FilterToViableTypes(PartialArray<Symbol> symbols)
        {
            // Don't return nested types.  Currently their value does not seem worth
            // it given all the extra stuff we'd have to plumb through.  Namely 
            // going down the "using static" code path and whatnot.
            return new List<Symbol>(
                from symbol in symbols
                where this.IsType(symbol) && !this.IsType(symbol.Parent())
                select symbol);
        }

        private PackageWithTypeResult CreateResult(AddReferenceDatabase database, Symbol type)
        {
            var nameParts = new List<string>();
            GetFullName(nameParts, type.FullName.Parent);

            var packageName = type.PackageName.ToString();

            var version = database.GetPackageVersion(type.Index).ToString();

            return new PackageWithTypeResult(
                packageName: packageName,
                typeName: type.Name.ToString(),
                version: version,
                containingNamespaceNames: nameParts,
                rank: GetRank(type));
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
                if (current.Type == SymbolType.Package || current.Type == SymbolType.Version)
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