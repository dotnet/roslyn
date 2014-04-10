// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    using MetadataOrDiagnostic = System.Object;

    internal abstract class CommonReferenceManager
    {
        /// <summary>
        /// Must be acquired whenever the following data are about to be modified:
        /// - Compilation.lazyAssemblySymbol
        /// - Compilation.referenceManager
        /// - ReferenceManager state
        /// - <see cref="AssemblyMetadata.CachedSymbols"/>
        /// - <see cref="F:Compilation.retargetingAssemblySymbols"/>
        /// 
        /// All the above data should be updated at once while holding this lock.
        /// Once lazyAssemblySymbol is set the Compilation.referenceManager field and ReferenceManager
        /// state should not change.
        /// </summary>
        internal static object SymbolCacheAndReferenceManagerStateGuard = new CommonLock();

        /// <summary>
        /// Enumerates all referenced assemblies.
        /// </summary>
        internal abstract IEnumerable<KeyValuePair<MetadataReference, IAssemblySymbol>> GetReferencedAssemblies();

        internal abstract bool TryGetReferencedAssemblySymbol(MetadataReference reference, out IAssemblySymbol symbol, out ImmutableArray<string> aliases);
    }

    partial class CommonReferenceManager<TCompilation, TAssemblySymbol> : CommonReferenceManager
    {
        internal struct ReferencedAssembly
        {
            public readonly TAssemblySymbol Symbol;

            // All aliases given to this symbol via metadata references, may contain duplicates
            public readonly ImmutableArray<string> Aliases;

            public ReferencedAssembly(TAssemblySymbol symbol, ImmutableArray<string> aliases)
            {
                Debug.Assert(symbol != null && !aliases.IsDefault);

                this.Symbol = symbol;
                this.Aliases = aliases;
            }

            public bool DeclarationsAccessibleWithoutAlias()
            {
                return Aliases.Length == 0 || Aliases.IndexOf(MetadataReferenceProperties.GlobalAlias) >= 0;
            }
        }

        /// <summary>
        /// If the compilation being built represents an assembly its assembly name.
        /// If the compilation being built represents a module, the name of the 
        /// containing assembly or <see cref="F:Compilation.UnspecifiedModuleAssemblyName"/>
        /// if not specified (/moduleassemblyname command line option).
        /// </summary>
        internal readonly string SimpleAssemblyName;

        /// <summary>
        /// Used to compares assembly identities. 
        /// May implement unification and portability policies specific to the target platform.
        /// </summary>
        internal readonly AssemblyIdentityComparer IdentityComparer;

        /// <summary>
        /// Metadata observed by the compiler.
        /// May be shared accoss multiple Reference Managers.
        /// Access only under lock(<see cref="ObservedMetadata"/>).
        /// </summary>
        internal readonly Dictionary<MetadataReference, MetadataOrDiagnostic> ObservedMetadata;

        /// <summary>
        /// Once this is non-zero the state of the manager is fully initialized and immutable.
        /// </summary>
        private int isBound;

        /// <summary>
        /// True if the compilation has a reference that refers back to the assembly being compiled.
        /// </summary>
        /// <remarks>
        /// If we have a circular reference the bound references can't be shared with other compilations.
        /// </remarks>
        private ThreeState lazyHasCircularReference;

        /// <summary>
        /// A map from a metadata reference to an AssemblySymbol used for it. Do not access
        /// directly, use <see cref="ReferencedAssembliesMap"/> property instead.
        /// </summary>
        private Dictionary<MetadataReference, ReferencedAssembly> lazyReferencedAssembliesMap;

        /// <summary>
        /// A map from a net-module metadata reference to the index of the corresponding module
        /// symbol in the source assembly symbol for the current compilation.
        /// </summary>
        /// <remarks>
        /// Subtract one from the index (for the manifest module) to find the corresponding elements
        /// of lazyReferencedModules and lazyReferencedModulesReferences.
        /// </remarks>
        private Dictionary<MetadataReference, int> lazyReferencedModuleIndexMap;

        /// <summary>
        /// Maps reference string used in #r directive to a resolved metadata reference.
        /// If multiple #r's use the same value as a reference the resolved metadata reference is the same as well.
        /// </summary>
        private IDictionary<string, MetadataReference> lazyReferenceDirectiveMap;

        /// <summary>
        /// Array of unique bound #r references.
        /// </summary>
        /// <remarks>
        /// The references are in the order they appear in syntax trees. This order is currently preserved 
        /// as syntax trees are added or removed, but we might decide to share reference manager between compilations
        /// with different order of #r's. It doesn't seem this would be an issue since all #r's within the compilation
        /// has the same "priority" with respect to each other.
        /// </remarks>
        private ImmutableArray<MetadataReference> lazyDirectiveReferences;

        /// <summary>
        /// Diagnostics produced during reference resolution and binding.
        /// </summary>
        /// <remarks>
        /// When reporting diagnostics be sure not to include any information that can't be shared among 
        /// compilations that share the same reference manager (such as full identity of the compilation, 
        /// simple assembly name is ok).
        /// </remarks>
        private ImmutableArray<Diagnostic> lazyDiagnostics;

        /// <summary>
        /// COR library symbol, or null if the compilation itself is the COR library.
        /// </summary>
        /// <remarks>
        /// If the compilation being built is the COR library we don't want to store its source assembly symbol 
        /// here since we wouldn't be able to share the state among subsequent compilations that are derived from it
        /// (each of them has its own source assembly symbol).
        /// </remarks>
        private TAssemblySymbol lazyCorLibraryOpt;

        /// <summary>
        /// Standalone modules referenced by the compilation (doesn't include the manifest module of the compilation).
        /// </summary>
        /// <remarks>
        /// lazyReferencedModules[i] corresponds to lazyReferencedModulesReferences[i].
        /// </remarks>
        private ImmutableArray<PEModule> lazyReferencedModules;

        /// <summary>
        /// References of standalone modules referenced by the compilation (doesn't include the manifest module of the compilation).
        /// </summary>
        /// <remarks>
        /// lazyReferencedModules[i] corresponds to lazyReferencedModulesReferences[i].
        /// </remarks>
        private ImmutableArray<ModuleReferences<TAssemblySymbol>> lazyReferencedModulesReferences;

        /// <summary>
        /// Assemblies referenced directly by the source module of the compilation.
        /// </summary>
        private ImmutableArray<TAssemblySymbol> lazyReferencedAssemblies;

        /// <summary>
        /// Unified assemblies referenced directly by the source module of the compilation.
        /// </summary>
        private ImmutableArray<UnifiedAssembly<TAssemblySymbol>> lazyUnifiedAssemblies;

        public CommonReferenceManager(string simpleAssemblyName, AssemblyIdentityComparer identityComparer, Dictionary<MetadataReference, MetadataOrDiagnostic> observedMetadata)
        {
            Debug.Assert(simpleAssemblyName != null);
            Debug.Assert(identityComparer != null);

            this.SimpleAssemblyName = simpleAssemblyName;
            this.IdentityComparer = identityComparer;
            this.ObservedMetadata = observedMetadata ?? new Dictionary<MetadataReference, MetadataOrDiagnostic>();
        }

        internal ImmutableArray<Diagnostic> Diagnostics
        {
            get
            {
                AssertBound();
                return lazyDiagnostics;
            }
        }

        internal bool HasCircularReference
        {
            get
            {
                AssertBound();
                return lazyHasCircularReference == ThreeState.True;
            }
        }

        internal Dictionary<MetadataReference, ReferencedAssembly> ReferencedAssembliesMap
        {
            get
            {
                AssertBound();
                return lazyReferencedAssembliesMap;
            }
        }

        internal Dictionary<MetadataReference, int> ReferencedModuleIndexMap
        {
            get
            {
                AssertBound();
                return lazyReferencedModuleIndexMap;
            }
        }

        internal IDictionary<string, MetadataReference> ReferenceDirectiveMap
        {
            get
            {
                AssertBound();
                return lazyReferenceDirectiveMap;
            }
        }

        internal ImmutableArray<MetadataReference> DirectiveReferences
        {
            get
            {
                AssertBound();
                return lazyDirectiveReferences;
            }
        }

        #region Symbols necessary to set up source assembly and module

        internal TAssemblySymbol CorLibraryOpt
        {
            get
            {
                AssertBound();
                return lazyCorLibraryOpt;
            }
        }

        internal ImmutableArray<PEModule> ReferencedModules
        {
            get
            {
                AssertBound();
                return lazyReferencedModules;
            }
        }

        internal ImmutableArray<ModuleReferences<TAssemblySymbol>> ReferencedModulesReferences
        {
            get
            {
                AssertBound();
                return lazyReferencedModulesReferences;
            }
        }

        internal ImmutableArray<TAssemblySymbol> ReferencedAssemblies
        {
            get
            {
                AssertBound();
                return lazyReferencedAssemblies;
            }
        }

        internal ImmutableArray<UnifiedAssembly<TAssemblySymbol>> UnifiedAssemblies
        {
            get
            {
                AssertBound();
                return lazyUnifiedAssemblies;
            }
        }

        #endregion

        /// <summary>
        /// Call only while holding <see cref="P:SymbolCacheAndReferenceManagerStateGuard"/>.
        /// </summary>
        [Conditional("DEBUG")]
        internal void AssertUnbound()
        {
            Debug.Assert(isBound == 0);
            Debug.Assert(lazyHasCircularReference == ThreeState.Unknown);
            Debug.Assert(lazyReferencedAssembliesMap == null);
            Debug.Assert(lazyReferencedModuleIndexMap == null);
            Debug.Assert(lazyReferenceDirectiveMap == null);
            Debug.Assert(lazyDirectiveReferences.IsDefault);
            Debug.Assert(lazyReferencedModules.IsDefault);
            Debug.Assert(lazyReferencedModulesReferences.IsDefault);
            Debug.Assert(lazyReferencedAssemblies.IsDefault);
            Debug.Assert(lazyUnifiedAssemblies.IsDefault);
            Debug.Assert(lazyCorLibraryOpt == null);
        }

        [Conditional("DEBUG")]
        internal void AssertBound()
        {
            Debug.Assert(isBound != 0);
            Debug.Assert(lazyHasCircularReference != ThreeState.Unknown);
            Debug.Assert(lazyReferencedAssembliesMap != null);
            Debug.Assert(lazyReferencedModuleIndexMap != null);
            Debug.Assert(lazyReferenceDirectiveMap != null);
            Debug.Assert(!lazyDirectiveReferences.IsDefault);
            Debug.Assert(!lazyReferencedModules.IsDefault);
            Debug.Assert(!lazyReferencedModulesReferences.IsDefault);
            Debug.Assert(!lazyReferencedAssemblies.IsDefault);
            Debug.Assert(!lazyUnifiedAssemblies.IsDefault);

            // lazyCorLibrary is null if the compilation is corlib
            Debug.Assert(lazyReferencedAssemblies.Length == 0 || lazyCorLibraryOpt != null);
        }

        [Conditional("DEBUG")]
        internal void AssertCanReuseForCompilation(TCompilation compilation)
        {
            Debug.Assert(compilation.MakeSourceAssemblySimpleName() == this.SimpleAssemblyName);
        }

        internal bool IsBound
        {
            get
            {
                return isBound != 0;
            }
        }

        /// <summary>
        /// Call only while holding <see cref="P:SymbolCacheAndReferenceManagerStateGuard"/>.
        /// </summary>
        internal void InitializeNoLock(
            Dictionary<MetadataReference, ReferencedAssembly> referencedAssembliesMap,
            Dictionary<MetadataReference, int> referencedModulesMap,
            IDictionary<string, MetadataReference> boundReferenceDirectiveMap,
            ImmutableArray<MetadataReference> boundReferenceDirectives,
            bool containsCircularReferences,
            ImmutableArray<Diagnostic> diagnostics,
            TAssemblySymbol corLibraryOpt,
            ImmutableArray<PEModule> referencedModules,
            ImmutableArray<ModuleReferences<TAssemblySymbol>> referencedModulesReferences,
            ImmutableArray<TAssemblySymbol> referencedAssemblies,
            ImmutableArray<UnifiedAssembly<TAssemblySymbol>> unifiedAssemblies)
        {
            AssertUnbound();

            Debug.Assert(referencedModules.Length == referencedModulesReferences.Length);
            Debug.Assert(referencedModules.Length == referencedModulesMap.Count);

            this.lazyReferencedAssembliesMap = referencedAssembliesMap;
            this.lazyReferencedModuleIndexMap = referencedModulesMap;
            this.lazyDiagnostics = diagnostics;
            this.lazyReferenceDirectiveMap = boundReferenceDirectiveMap;
            this.lazyDirectiveReferences = boundReferenceDirectives;

            this.lazyCorLibraryOpt = corLibraryOpt;
            this.lazyReferencedModules = referencedModules;
            this.lazyReferencedModulesReferences = referencedModulesReferences;
            this.lazyReferencedAssemblies = referencedAssemblies;
            this.lazyUnifiedAssemblies = unifiedAssemblies;
            this.lazyHasCircularReference = containsCircularReferences.ToThreeState();

            // once we flip this bit the state of the manager is immutable and available to any readers:
            Interlocked.Exchange(ref isBound, 1);
        }

        #region Compilation APIs Implementation

        // for testing purposes
        internal IEnumerable<string> ExternAliases
        {
            get
            {
                return ReferencedAssembliesMap.Values.SelectMany(entry => entry.Aliases);
            }
        }

        internal sealed override IEnumerable<KeyValuePair<MetadataReference, IAssemblySymbol>> GetReferencedAssemblies()
        {
            return ReferencedAssembliesMap.Select(ra => KeyValuePair.Create(ra.Key, (IAssemblySymbol)ra.Value.Symbol));
        }

        internal sealed override bool TryGetReferencedAssemblySymbol(MetadataReference reference, out IAssemblySymbol symbol, out ImmutableArray<string> aliases)
        {
            ReferencedAssembly result;
            if (ReferencedAssembliesMap.TryGetValue(reference, out result))
            {
                symbol = result.Symbol;
                aliases = result.Aliases;
                return true;
            }

            symbol = null;
            aliases = default(ImmutableArray<string>);
            return false;
        }

        internal TAssemblySymbol GetReferencedAssemblySymbol(MetadataReference reference)
        {
            ReferencedAssembly result;
            return ReferencedAssembliesMap.TryGetValue(reference, out result) ? result.Symbol : null;
        }

        internal int GetReferencedModuleIndex(MetadataReference reference)
        {
            int index;
            return ReferencedModuleIndexMap.TryGetValue(reference, out index) ? index : -1;
        }

        #endregion
    }
}
