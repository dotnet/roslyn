using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Roslyn.Compilers.Common;
using Roslyn.Utilities;
using Roslyn.Compilers.MetadataReader;
using System.IO;
using System.Runtime.CompilerServices;

namespace Roslyn.Compilers
{
    partial class CommonAssemblyManager<TCompilation, TAssemblySymbol, TModuleSymbol>
    {
        internal struct ReferencedAssembly
        {
            public readonly TAssemblySymbol Symbol;

            // All aliases given to this symbol via metadata references, may contain duplicates
            public readonly ReadOnlyArray<string> Aliases;

            public ReferencedAssembly(TAssemblySymbol symbol, ReadOnlyArray<string> aliases)
            {
                Debug.Assert(symbol != null && aliases.IsNotNull);

                this.Symbol = symbol;
                this.Aliases = aliases;
            }
        }

        internal readonly string SimpleAssemblyName;

        /// <summary>
        /// True if the compilation has a reference that refers back to the assembly being compiled.
        /// </summary>
        /// <remarks>
        /// If we have a circular reference the bound references can't be shared with other compilations.
        /// </remarks>
        private ThreeState lazyHasCircularReference;

        /// <summary>
        /// A map from a metadata reference to an AssemblySymbol used for it. Do not access
        /// directly, use <see cref="P:ReferencedAssembliesMap"/> property instead.
        /// </summary>
        private Dictionary<MetadataReference, ReferencedAssembly> lazyReferencedAssembliesMap;

        /// <summary>
        /// A map from a net-module metadata reference to a module symbol used for it. The
        /// module symbol is one of the modules contained in m_AssemblySymbol.Modules list. 
        /// </summary>
        private Dictionary<MetadataReference, TModuleSymbol> lazyReferencedModulesMap;

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
        private ReadOnlyArray<MetadataReference> lazyDirectiveReferences;

        /// <summary>
        /// Diagnostics produced during reference resolution and binding.
        /// </summary>
        /// <remarks>
        /// When reporting diagnostics be sure not to include any information that can't be shared among 
        /// compilations that share the same refernce manager (such as full identity of the compilation, 
        /// simple assembly name is ok).
        /// </remarks>
        private DiagnosticBag lazyDiagnostics;

        /// <summary>
        /// COR library symbol. 
        /// </summary>
        private TAssemblySymbol lazyCorLibrary;

        /// <summary>
        /// Standalone modules referenced by the compilation (doesn't include the manifest module of the compilation).
        /// </summary>
        private ReadOnlyArray<TModuleSymbol> lazyReferencedModules;

        /// <summary>
        /// Assemblies referenced directly by the source module of the compilation.
        /// </summary>
        private ReadOnlyArray<TAssemblySymbol> lazyReferencedAssemblies;

        /// <summary>
        /// Unified assemblies referenced directly by the source module of the compilation.
        /// </summary>
        private ReadOnlyArray<UnifiedAssembly> lazyUnifiedAssemblies;

        public CommonAssemblyManager(string simpleAssemblyName)
        {
            this.SimpleAssemblyName = simpleAssemblyName;
        }

        internal DiagnosticBag Diagnostics
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

        internal Dictionary<MetadataReference, TModuleSymbol> ReferencedModulesMap
        {
            get
            {
                AssertBound();
                return lazyReferencedModulesMap;
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

        internal ReadOnlyArray<MetadataReference> DirectiveReferences
        {
            get
            {
                AssertBound();
                return lazyDirectiveReferences;
            }
        }

        #region Symbols necessary to set up source assembly and module

        internal TAssemblySymbol CorLibrary
        {
            get
            {
                AssertBound();
                return lazyCorLibrary;
            }
        }

        internal ReadOnlyArray<TModuleSymbol> ReferencedModules
        {
            get
            {
                AssertBound();
                return lazyReferencedModules;
            }
        }

        internal ReadOnlyArray<TAssemblySymbol> ReferencedAssemblies
        {
            get
            {
                AssertBound();
                return lazyReferencedAssemblies;
            }
        }

        internal ReadOnlyArray<UnifiedAssembly> UnifiedAssemblies
        {
            get
            {
                AssertBound();
                return lazyUnifiedAssemblies;
            }
        }

        #endregion

        [Conditional("DEBUG")]
        internal void AssertUnbound()
        {
            Debug.Assert(lazyHasCircularReference == ThreeState.Unknown);
            Debug.Assert(lazyReferencedAssembliesMap == null);
            Debug.Assert(lazyReferencedModulesMap == null);
            Debug.Assert(lazyReferenceDirectiveMap == null);
            Debug.Assert(lazyDirectiveReferences.IsNull);
            Debug.Assert(lazyCorLibrary == null);
            Debug.Assert(lazyReferencedModules.IsNull);
            Debug.Assert(lazyReferencedAssemblies.IsNull);
            Debug.Assert(lazyUnifiedAssemblies.IsNull);
        }

        [Conditional("DEBUG")]
        internal void AssertBound()
        {
            Debug.Assert(lazyHasCircularReference != ThreeState.Unknown);
            Debug.Assert(lazyReferencedAssembliesMap != null);
            Debug.Assert(lazyReferencedModulesMap != null);
            Debug.Assert(lazyReferenceDirectiveMap != null);
            Debug.Assert(lazyDirectiveReferences.IsNotNull);
            Debug.Assert(lazyCorLibrary != null);
            Debug.Assert(lazyReferencedModules.IsNotNull);
            Debug.Assert(lazyReferencedModules.IsNotNull);
            Debug.Assert(lazyReferencedAssemblies.IsNotNull);
            Debug.Assert(lazyUnifiedAssemblies.IsNotNull);
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
                return lazyHasCircularReference != ThreeState.Unknown;
            }
        }

        /// <summary>
        /// Call only while holding MetadataCache.CacheLockObject.
        /// </summary>
        internal void Initialize(
            Dictionary<MetadataReference, ReferencedAssembly> referencedAssembliesMap,
            Dictionary<MetadataReference, TModuleSymbol> referencedModulesMap,
            IDictionary<string, MetadataReference> boundReferenceDirectiveMap,
            ReadOnlyArray<MetadataReference> boundReferenceDirectives,
            bool containsCircularReferences,
            DiagnosticBag diagnostics,
            TAssemblySymbol corLibrary,
            ReadOnlyArray<TModuleSymbol> referencedModules,
            ReadOnlyArray<TAssemblySymbol> referencedAssemblies,
            ReadOnlyArray<UnifiedAssembly> unifiedAssemblies)
        {
            this.lazyReferencedAssembliesMap = referencedAssembliesMap;
            this.lazyReferencedModulesMap = referencedModulesMap;
            this.lazyDiagnostics = diagnostics;
            this.lazyReferenceDirectiveMap = boundReferenceDirectiveMap;
            this.lazyDirectiveReferences = boundReferenceDirectives;

            this.lazyCorLibrary = corLibrary;
            this.lazyReferencedModules = referencedModules;
            this.lazyReferencedAssemblies = referencedAssemblies;
            this.lazyUnifiedAssemblies = unifiedAssemblies;

            this.lazyHasCircularReference = containsCircularReferences.ToThreeState();
        }

        #region Compilation APIs Implementation

        // for testing purposes
        internal IEnumerable<string> ExternAliases
        {
            get
            {
                return ReferencedAssembliesMap.Values.SelectMany(entry => entry.Aliases.AsEnumerable());
            }
        }

        internal TAssemblySymbol GetReferencedAssemblySymbol(MetadataReference reference)
        {
            if (reference == null)
            {
                throw new ArgumentNullException("reference");
            }

            ReferencedAssembly result;
            return ReferencedAssembliesMap.TryGetValue(reference, out result) ? result.Symbol : null;
        }

        internal TModuleSymbol GetReferencedModuleSymbol(MetadataReference reference)
        {
            if (reference == null)
            {
                throw new ArgumentNullException("reference");
            }

            TModuleSymbol result;
            return ReferencedModulesMap.TryGetValue(reference, out result) ? result : null;
        }

        #endregion
    }
}
