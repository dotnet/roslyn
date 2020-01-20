// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.Symbols;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Assembly symbol referenced by a AssemblyRef for which we couldn't find a matching 
    /// compilation reference but we found one that differs in version. 
    /// Created only for assemblies that require runtime binding redirection policy,
    /// i.e. not for Framework assemblies.
    /// </summary>
    internal struct UnifiedAssembly<TAssemblySymbol>
        where TAssemblySymbol : class, IAssemblySymbolInternal
    {
        /// <summary>
        /// Original reference that was unified to the identity of the <see cref="TargetAssembly"/>.
        /// </summary>
        internal readonly AssemblyIdentity OriginalReference;

        internal readonly TAssemblySymbol TargetAssembly;

        public UnifiedAssembly(TAssemblySymbol targetAssembly, AssemblyIdentity originalReference)
        {
            Debug.Assert(originalReference != null);
            Debug.Assert(targetAssembly != null);

            this.OriginalReference = originalReference;
            this.TargetAssembly = targetAssembly;
        }
    }
}
