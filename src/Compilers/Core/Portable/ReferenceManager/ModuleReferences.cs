// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A record of the assemblies referenced by a module (their identities, symbols, and unification).
    /// </summary>
    internal sealed class ModuleReferences<TAssemblySymbol>
        where TAssemblySymbol : class, IAssemblySymbol
    {
        /// <summary>
        /// Identities of referenced assemblies (those that are or will be emitted to metadata).
        /// </summary>
        /// <remarks>
        /// Names[i] is the identity of assembly Symbols[i].
        /// </remarks>
        public readonly ImmutableArray<AssemblyIdentity> Names;

        /// <summary>
        /// Assembly symbols that the identities are resolved against.
        /// </summary>
        /// <remarks>
        /// Names[i] is the identity of assembly Symbols[i].
        /// Unresolved references are represented as MissingAssemblySymbols.
        /// </remarks>
        public readonly ImmutableArray<TAssemblySymbol> Symbols;

        /// <summary>
        /// A subset of <see cref="Symbols"/> that correspond to references with non-matching (unified) 
        /// version along with unification details.
        /// </summary>
        public readonly ImmutableArray<UnifiedAssembly<TAssemblySymbol>> UnifiedAssemblies;

        public ModuleReferences(
            ImmutableArray<AssemblyIdentity> names,
            ImmutableArray<TAssemblySymbol> symbols,
            ImmutableArray<UnifiedAssembly<TAssemblySymbol>> unifiedAssemblies)
        {
            Debug.Assert(!names.IsDefault);
            Debug.Assert(!symbols.IsDefault);
            Debug.Assert(names.Length == symbols.Length);
            Debug.Assert(!unifiedAssemblies.IsDefault);

            this.Names = names;
            this.Symbols = symbols;
            this.UnifiedAssemblies = unifiedAssemblies;
        }
    }
}
