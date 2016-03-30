using System.Collections.Generic;

#if HUBSERVICES
namespace Microsoft.CodeAnalysis.HubServices.SymbolSearch.Data
#else
namespace Microsoft.CodeAnalysis.Packaging
#endif
{
    internal class ReferenceAssemblyWithTypeResult
    {
        public readonly IReadOnlyList<string> ContainingNamespaceNames;
        public readonly string AssemblyName;
        public readonly string TypeName;

        public ReferenceAssemblyWithTypeResult(
            string assemblyName,
            string typeName,
            IReadOnlyList<string> containingNamespaceNames)
        {
            AssemblyName = assemblyName;
            TypeName = typeName;
            ContainingNamespaceNames = containingNamespaceNames;
        }
    }
}
