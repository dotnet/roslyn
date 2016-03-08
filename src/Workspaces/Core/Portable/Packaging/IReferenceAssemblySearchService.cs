using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Packaging
{

    internal interface IReferenceAssemblySearchService : IWorkspaceService
    {
        /// <summary>
        /// Searches for reference assemblies that contain a type with the provided name and arity.
        /// Note: Implementations are free to return the results they feel best for the
        /// given data.  Specifically, they can do exact or fuzzy matching on the name.
        /// They can use or ignore the arity depending on their capabilities. 
        /// 
        /// Implementations should return results in order from best to worst (from their
        /// perspective).
        /// </summary>
        IEnumerable<ReferenceAssemblyWithTypeResult> FindReferenceAssembliesWithType(
            string name, int arity, CancellationToken cancellationToken);
    }

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
