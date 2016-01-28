#define ELFIE_ENABLED
using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
#if ELFIE_ENABLED
using Elfie.Model;
using Elfie.Model.Structures;
using Elfie.Model.Tree;
#endif
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Packaging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Nuget
{
    [ExportWorkspaceServiceFactory(typeof(IPackageSearchService)), Shared]
    internal class PackageSearchServiceFactory : IWorkspaceServiceFactory
    {
        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new PackageSearchService(workspaceServices);
        }

        private class PackageSearchService : IPackageSearchService
        {
#if ELFIE_ENABLED
            private static Lazy<IMemberDatabase> s_memberDatabase = new Lazy<IMemberDatabase>(
                () => MemberDatabase.Load(@"C:\Temp\Index.StablePackages.PublicApis.95.ardb"),
                isThreadSafe: true);
#endif

            private HostWorkspaceServices workspaceServices;

            public PackageSearchService(HostWorkspaceServices workspaceServices)
            {
                this.workspaceServices = workspaceServices;
            }

            public IEnumerable<PackageSearchResult> Search(string name, int arity, CancellationToken cancellationToken)
            {
#if ELFIE_ENABLED
                var database = s_memberDatabase.Value;
                var query = new MemberQuery(name, isFullSuffix: true, isFullNamespace: false);

                var symbols = new PartialArray<Symbol>(3);
                if (query.TryFindMembers(database, ref symbols))
                {
                    var result = new List<PackageSearchResult>();
                    foreach (var symbol in symbols)
                    {
                        var nameParts = new List<string>();
                        GetFullName(nameParts, symbol.FullName.Parent);

                        if (nameParts.Count > 0)
                        {
                            yield return new PackageSearchResult(nameParts, symbol.PackageName.ToString());
                        }
                    }
                }
#else
                yield break;
#endif
            }

#if ELFIE_ENABLED
            private void GetFullName(List<string> nameParts, Path8 path)
            {
                if (!path.IsEmpty)
                {
                    GetFullName(nameParts, path.Parent);
                    nameParts.Add(path.Name.ToString());
                }
            }
#endif
        }
    }
}
