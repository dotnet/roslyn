using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Elfie.Model;
using Elfie.Model.Structures;
using Elfie.Model.Tree;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Nuget;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Nuget
{
    [ExportWorkspaceServiceFactory(typeof(INugetSearchService)), Shared]
    internal class NugetSearchServiceFactory : IWorkspaceServiceFactory
    {
        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new NugetSearchService(workspaceServices);
        }

        private class NugetSearchService : INugetSearchService
        {
            private static Lazy<IMemberDatabase> s_memberDatabase = new Lazy<IMemberDatabase>(
                () => MemberDatabase.LoadWithDiagnostics(@"C:\Temp\NuGet.All.PublicTypesOnly.ardb"),
                isThreadSafe: true);

            private HostWorkspaceServices workspaceServices;

            public NugetSearchService(HostWorkspaceServices workspaceServices)
            {
                this.workspaceServices = workspaceServices;
            }

            public IEnumerable<NugetSearchResult> Search(string name, int arity, CancellationToken cancellationToken)
            {
                var database = s_memberDatabase.Value;
                var query = new MemberQuery(name, isFullSuffix: true, isFullNamespace: false);

                var symbols = new PartialArray<Symbol>(3);
                if (query.TryFindMembers(database, ref symbols))
                {
                    var result = new List<NugetSearchResult>();
                    foreach (var symbol in symbols)
                    {
                        var nameParts = new List<string>();
                        GetFullName(nameParts, symbol.FullName.Parent);

                        if (nameParts.Count > 0)
                        {
                            yield return new NugetSearchResult(nameParts, symbol.PackageName.ToString());
                        }
                    }
                }
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
}
