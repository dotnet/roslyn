using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Nuget
{
    internal interface IPackageSearchService : IWorkspaceService
    {
        IEnumerable<NugetSearchResult> Search(string name, int arity, CancellationToken cancellationToken);
    }

    internal struct NugetSearchResult
    {
        public readonly IReadOnlyList<string> NameParts;
        public readonly string PackageName;

        public NugetSearchResult(IReadOnlyList<string> nameParts, string packageName)
        {
            NameParts = nameParts;
            PackageName = packageName;
        }
    }
}
