using System.Collections.Generic;

#if HUBSERVICES
namespace Microsoft.CodeAnalysis.HubServices.SymbolSearch.Data
#else
namespace Microsoft.CodeAnalysis.Packaging
#endif
{
    internal class PackageWithTypeResult
    {
        public readonly IReadOnlyList<string> ContainingNamespaceNames;
        public readonly string PackageName;
        public readonly string TypeName;
        public readonly string Version;
        public readonly int Rank;

        public PackageWithTypeResult(
            string packageName,
            string typeName,
            string version,
            IReadOnlyList<string> containingNamespaceNames,
            int rank)
        {
            PackageName = packageName;
            TypeName = typeName;
            Version = string.IsNullOrWhiteSpace(version) ? null : version;
            ContainingNamespaceNames = containingNamespaceNames;
            Rank = rank;
        }
    }
}
