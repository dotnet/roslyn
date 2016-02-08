using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Packaging
{
    internal interface IPackageSearchService : IWorkspaceService
    {
        IEnumerable<PackageSearchResult> Search(string name, int arity, CancellationToken cancellationToken);
    }

    internal struct PackageSearchResult
    {
        public readonly IReadOnlyList<string> NameParts;
        public readonly string PackageName;

        public PackageSearchResult(IReadOnlyList<string> nameParts, string packageName)
        {
            NameParts = nameParts;
            PackageName = packageName;
        }
    }
}
