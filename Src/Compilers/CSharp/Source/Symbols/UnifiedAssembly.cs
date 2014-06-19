using System.Diagnostics;

namespace Roslyn.Compilers.CSharp
{
    /// <summary>
    /// Assembyl symbol referenced by a AssemblyRef for which we couldn't find a matching 
    /// compilation reference but we found one that differs in version.
    /// </summary>
    internal struct UnifiedAssembly
    {
        /// <summary>
        /// Original reference that was unified to the identity of the <see cref="P:TargetAssembly"/>.
        /// </summary>
        internal readonly AssemblyIdentity OriginalReference;

        internal readonly AssemblySymbol TargetAssembly;

        public UnifiedAssembly(AssemblySymbol targetAssembly, AssemblyIdentity originalReference)
        {
            Debug.Assert(originalReference != null);
            Debug.Assert(targetAssembly != null);

            this.OriginalReference = originalReference;
            this.TargetAssembly = targetAssembly;
        }
    }
}
