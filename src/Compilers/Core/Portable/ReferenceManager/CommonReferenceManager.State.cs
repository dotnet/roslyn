// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
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
        /// - <see cref="Compilation.RetargetingAssemblySymbols"/>
        /// 
        /// All the above data should be updated at once while holding this lock.
        /// Once lazyAssemblySymbol is set the Compilation.referenceManager field and ReferenceManager
        /// state should not change.
        /// </summary>
        internal static object SymbolCacheAndReferenceManagerStateGuard = new object();

        /// <summary>
        /// Enumerates all referenced assemblies.
        /// </summary>
        internal abstract IEnumerable<KeyValuePair<MetadataReference, IAssemblySymbol>> GetReferencedAssemblies();
       
        /// <summary>
        /// Enumerates all referenced assemblies and their aliases.
        /// </summary>
        internal abstract IEnumerable<ValueTuple<IAssemblySymbol, ImmutableArray<string>>> GetReferencedAssemblyAliases();

        internal abstract MetadataReference GetMetadataReference(IAssemblySymbol assemblySymbol);
        internal abstract ImmutableArray<MetadataReference> ExplicitReferences { get; }
        internal abstract ImmutableArray<MetadataReference> ImplicitReferences { get; }
        internal abstract IEnumerable<KeyValuePair<AssemblyIdentity, PortableExecutableReference>> GetImplicitlyResolvedAssemblyReferences();
    }

    internal partial class CommonReferenceManager<TCompilation, TAssemblySymbol> : CommonReferenceManager
    {
        /// <summary>
        /// If the compilation being built represents an assembly its assembly name.
        /// If the compilation being built represents a module, the name of the 
        /// containing assembly or <see cref="Compilation.UnspecifiedModuleAssemblyName"/>
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
        /// May be shared across multiple Reference Managers.
        /// Access only under lock(<see cref="ObservedMetadata"/>).
        /// </summary>
        internal readonly Dictionary<MetadataReference, MetadataOrDiagnostic> ObservedMetadata;

        /// <summary>
        /// Once this is non-zero the state of the manager is fully initialized and immutable.
        /// </summary>
        private int _isBound;

        /// <summary>
        /// True if the compilation has a reference that refers back to the assembly being compiled.
        /// </summary>
        /// <remarks>
        /// If we have a circular reference the bound references can't be shared with other compilations.
        /// </remarks>
        private ThreeState _lazyHasCircularReference;

        /// <summary>
        /// A map from a metadata reference to an index to <see cref="_lazyReferencedAssemblies"/> array. Do not access
        /// directly, use <see cref="_lazyReferencedAssembliesMap"/> property instead.
        /// </summary>
        private Dictionary<MetadataReference, int> _lazyReferencedAssembliesMap;

        /// <summary>
        /// A map from a net-module metadata reference to the index of the corresponding module
        /// symbol in the source assembly symbol for the current compilation.
        /// </summary>
        /// <remarks>
        /// Subtract one from the index (for the manifest module) to find the corresponding elements
        /// of <see cref="_lazyReferencedModules"/> and <see cref="_lazyReferencedModulesReferences"/>.
        /// </remarks>
        private Dictionary<MetadataReference, int> _lazyReferencedModuleIndexMap;

        /// <summary>
        /// Maps (containing syntax tree file name, reference string) of #r directive to a resolved metadata reference.
        /// If multiple #r's in the same tree use the same value as a reference the resolved metadata reference is the same as well.
        /// </summary>
        private IDictionary<ValueTuple<string, string>, MetadataReference> _lazyReferenceDirectiveMap;

        /// <summary>
        /// Array of unique bound #r references.
        /// </summary>
        /// <remarks>
        /// The references are in the order they appear in syntax trees. This order is currently preserved 
        /// as syntax trees are added or removed, but we might decide to share reference manager between compilations
        /// with different order of #r's. It doesn't seem this would be an issue since all #r's within the compilation
        /// have the same "priority" with respect to each other.
        /// </remarks>
        private ImmutableArray<MetadataReference> _lazyDirectiveReferences;

        private ImmutableArray<MetadataReference> _lazyExplicitReferences;
        private ImmutableArray<MetadataReference> _lazyImplicitReferences;

        /// <summary>
        /// Diagnostics produced during reference resolution and binding.
        /// </summary>
        /// <remarks>
        /// When reporting diagnostics be sure not to include any information that can't be shared among 
        /// compilations that share the same reference manager (such as full identity of the compilation, 
        /// simple assembly name is ok).
        /// </remarks>
        private ImmutableArray<Diagnostic> _lazyDiagnostics;

        /// <summary>
        /// COR library symbol, or null if the compilation itself is the COR library.
        /// </summary>
        /// <remarks>
        /// If the compilation being built is the COR library we don't want to store its source assembly symbol 
        /// here since we wouldn't be able to share the state among subsequent compilations that are derived from it
        /// (each of them has its own source assembly symbol).
        /// </remarks>
        private TAssemblySymbol _lazyCorLibraryOpt;

        /// <summary>
        /// Standalone modules referenced by the compilation (doesn't include the manifest module of the compilation).
        /// </summary>
        /// <remarks>
        /// <see cref="_lazyReferencedModules"/>[i] corresponds to <see cref="_lazyReferencedModulesReferences"/>[i].
        /// </remarks>
        private ImmutableArray<PEModule> _lazyReferencedModules;

        /// <summary>
        /// References of standalone modules referenced by the compilation (doesn't include the manifest module of the compilation).
        /// </summary>
        /// <remarks>
        /// <see cref="_lazyReferencedModules"/>[i] corresponds to <see cref="_lazyReferencedModulesReferences"/>[i].
        /// </remarks>
        private ImmutableArray<ModuleReferences<TAssemblySymbol>> _lazyReferencedModulesReferences;

        /// <summary>
        /// Assemblies referenced directly by the source module of the compilation.
        /// </summary>
        private ImmutableArray<TAssemblySymbol> _lazyReferencedAssemblies;

        /// <summary>
        /// Assemblies referenced directly by the source module of the compilation.
        /// </summary>
        /// <remarks>
        /// Aliases <see cref="_lazyAliasesOfReferencedAssemblies"/>[i] are of an assembly <see cref="_lazyReferencedAssemblies"/>[i].
        /// </remarks>
        private ImmutableArray<ImmutableArray<string>> _lazyAliasesOfReferencedAssemblies;

        /// <summary>
        /// Unified assemblies referenced directly by the source module of the compilation.
        /// </summary>
        private ImmutableArray<UnifiedAssembly<TAssemblySymbol>> _lazyUnifiedAssemblies;

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
                return _lazyDiagnostics;
            }
        }

        internal bool HasCircularReference
        {
            get
            {
                AssertBound();
                return _lazyHasCircularReference == ThreeState.True;
            }
        }

        internal Dictionary<MetadataReference, int> ReferencedAssembliesMap
        {
            get
            {
                AssertBound();
                return _lazyReferencedAssembliesMap;
            }
        }

        internal Dictionary<MetadataReference, int> ReferencedModuleIndexMap
        {
            get
            {
                AssertBound();
                return _lazyReferencedModuleIndexMap;
            }
        }

        internal IDictionary<ValueTuple<string, string>, MetadataReference> ReferenceDirectiveMap
        {
            get
            {
                AssertBound();
                return _lazyReferenceDirectiveMap;
            }
        }

        internal ImmutableArray<MetadataReference> DirectiveReferences
        {
            get
            {
                AssertBound();
                return _lazyDirectiveReferences;
            }
        }

        internal override ImmutableArray<MetadataReference> ImplicitReferences
        {
            get
            {
                AssertBound();
                return _lazyImplicitReferences;
            }
        }

        internal override ImmutableArray<MetadataReference> ExplicitReferences
        {
            get
            {
                AssertBound();
                return _lazyExplicitReferences;
            }
        }

        #region Symbols necessary to set up source assembly and module

        internal TAssemblySymbol CorLibraryOpt
        {
            get
            {
                AssertBound();
                return _lazyCorLibraryOpt;
            }
        }

        internal ImmutableArray<PEModule> ReferencedModules
        {
            get
            {
                AssertBound();
                return _lazyReferencedModules;
            }
        }

        internal ImmutableArray<ModuleReferences<TAssemblySymbol>> ReferencedModulesReferences
        {
            get
            {
                AssertBound();
                return _lazyReferencedModulesReferences;
            }
        }

        internal ImmutableArray<TAssemblySymbol> ReferencedAssemblies
        {
            get
            {
                AssertBound();
                return _lazyReferencedAssemblies;
            }
        }

        internal ImmutableArray<ImmutableArray<string>> AliasesOfReferencedAssemblies
        {
            get
            {
                AssertBound();
                return _lazyAliasesOfReferencedAssemblies;
            }
        }

        internal ImmutableArray<UnifiedAssembly<TAssemblySymbol>> UnifiedAssemblies
        {
            get
            {
                AssertBound();
                return _lazyUnifiedAssemblies;
            }
        }

        #endregion

        /// <summary>
        /// Call only while holding <see cref="CommonReferenceManager.SymbolCacheAndReferenceManagerStateGuard"/>.
        /// </summary>
        [Conditional("DEBUG")]
        internal void AssertUnbound()
        {
            Debug.Assert(_isBound == 0);
            Debug.Assert(_lazyHasCircularReference == ThreeState.Unknown);
            Debug.Assert(_lazyReferencedAssembliesMap == null);
            Debug.Assert(_lazyReferencedModuleIndexMap == null);
            Debug.Assert(_lazyReferenceDirectiveMap == null);
            Debug.Assert(_lazyDirectiveReferences.IsDefault);
            Debug.Assert(_lazyImplicitReferences.IsDefault);
            Debug.Assert(_lazyExplicitReferences.IsDefault);
            Debug.Assert(_lazyReferencedModules.IsDefault);
            Debug.Assert(_lazyReferencedModulesReferences.IsDefault);
            Debug.Assert(_lazyReferencedAssemblies.IsDefault);
            Debug.Assert(_lazyAliasesOfReferencedAssemblies.IsDefault);
            Debug.Assert(_lazyUnifiedAssemblies.IsDefault);
            Debug.Assert(_lazyCorLibraryOpt == null);
        }

        [Conditional("DEBUG")]
        internal void AssertBound()
        {
            Debug.Assert(_isBound != 0);
            Debug.Assert(_lazyHasCircularReference != ThreeState.Unknown);
            Debug.Assert(_lazyReferencedAssembliesMap != null);
            Debug.Assert(_lazyReferencedModuleIndexMap != null);
            Debug.Assert(_lazyReferenceDirectiveMap != null);
            Debug.Assert(!_lazyDirectiveReferences.IsDefault);
            Debug.Assert(!_lazyImplicitReferences.IsDefault);
            Debug.Assert(!_lazyExplicitReferences.IsDefault);
            Debug.Assert(!_lazyReferencedModules.IsDefault);
            Debug.Assert(!_lazyReferencedModulesReferences.IsDefault);
            Debug.Assert(!_lazyReferencedAssemblies.IsDefault);
            Debug.Assert(!_lazyAliasesOfReferencedAssemblies.IsDefault);
            Debug.Assert(!_lazyUnifiedAssemblies.IsDefault);

            // lazyCorLibrary is null if the compilation is corlib
            Debug.Assert(_lazyReferencedAssemblies.Length == 0 || _lazyCorLibraryOpt != null);
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
                return _isBound != 0;
            }
        }

        /// <summary>
        /// Call only while holding <see cref="CommonReferenceManager.SymbolCacheAndReferenceManagerStateGuard"/>.
        /// </summary>
        internal void InitializeNoLock(
            Dictionary<MetadataReference, int> referencedAssembliesMap,
            Dictionary<MetadataReference, int> referencedModulesMap,
            IDictionary<ValueTuple<string, string>, MetadataReference> boundReferenceDirectiveMap,
            ImmutableArray<MetadataReference> directiveReferences,
            ImmutableArray<MetadataReference> explicitReferences,
            ImmutableArray<MetadataReference> implicitReferences,
            bool containsCircularReferences,
            ImmutableArray<Diagnostic> diagnostics,
            TAssemblySymbol corLibraryOpt,
            ImmutableArray<PEModule> referencedModules,
            ImmutableArray<ModuleReferences<TAssemblySymbol>> referencedModulesReferences,
            ImmutableArray<TAssemblySymbol> referencedAssemblies,
            ImmutableArray<ImmutableArray<string>> aliasesOfReferencedAssemblies,
            ImmutableArray<UnifiedAssembly<TAssemblySymbol>> unifiedAssemblies)
        {
            AssertUnbound();

            Debug.Assert(referencedModules.Length == referencedModulesReferences.Length);
            Debug.Assert(referencedModules.Length == referencedModulesMap.Count);
            Debug.Assert(referencedAssemblies.Length == aliasesOfReferencedAssemblies.Length);

            _lazyReferencedAssembliesMap = referencedAssembliesMap;
            _lazyReferencedModuleIndexMap = referencedModulesMap;
            _lazyDiagnostics = diagnostics;
            _lazyReferenceDirectiveMap = boundReferenceDirectiveMap;
            _lazyDirectiveReferences = directiveReferences;
            _lazyExplicitReferences = explicitReferences;
            _lazyImplicitReferences = implicitReferences;

            _lazyCorLibraryOpt = corLibraryOpt;
            _lazyReferencedModules = referencedModules;
            _lazyReferencedModulesReferences = referencedModulesReferences;
            _lazyReferencedAssemblies = referencedAssemblies;
            _lazyAliasesOfReferencedAssemblies = aliasesOfReferencedAssemblies;
            _lazyUnifiedAssemblies = unifiedAssemblies;
            _lazyHasCircularReference = containsCircularReferences.ToThreeState();

            // once we flip this bit the state of the manager is immutable and available to any readers:
            Interlocked.Exchange(ref _isBound, 1);
        }

        /// <summary>
        /// Global namespaces of assembly references that have been superseded by an assembly reference with a higher version are 
        /// hidden behind <see cref="SupersededAlias"/> to avoid ambiguity when they are accessed from source.
        /// All existing aliases of a superseded assembly are discarded.
        /// </summary>
        private static readonly ImmutableArray<string> SupersededAlias = ImmutableArray.Create("<superseded>");

        protected static void BuildReferencedAssembliesAndModulesMaps(
            BoundInputAssembly[] bindingResult,
            ImmutableArray<MetadataReference> references,
            ImmutableArray<ResolvedReference> referenceMap,
            int referencedModuleCount,
            int explicitlyReferencedAsemblyCount,
            IReadOnlyDictionary<string, List<ReferencedAssemblyIdentity>> assemblyReferencesBySimpleName,
            bool supersedeLowerVersions,
            out Dictionary<MetadataReference, int> referencedAssembliesMap,
            out Dictionary<MetadataReference, int> referencedModulesMap,
            out ImmutableArray<ImmutableArray<string>> aliasesOfReferencedAssemblies)
        {
            referencedAssembliesMap = new Dictionary<MetadataReference, int>(referenceMap.Length);
            referencedModulesMap = new Dictionary<MetadataReference, int>(referencedModuleCount);
            var aliasesOfReferencedAssembliesBuilder = ArrayBuilder<ImmutableArray<string>>.GetInstance(referenceMap.Length - referencedModuleCount);
            bool hasRecursiveAliases = false;

            for (int i = 0; i < referenceMap.Length; i++)
            {
                if (referenceMap[i].IsSkipped)
                {
                    continue;
                }

                if (referenceMap[i].Kind == MetadataImageKind.Module)
                {
                    // add 1 for the manifest module:
                    int moduleIndex = 1 + referenceMap[i].Index;
                    referencedModulesMap.Add(references[i], moduleIndex);
                }
                else
                {
                    // index into assembly data array
                    int assemblyIndex = referenceMap[i].Index;
                    Debug.Assert(aliasesOfReferencedAssembliesBuilder.Count == assemblyIndex);

                    referencedAssembliesMap.Add(references[i], assemblyIndex);
                    aliasesOfReferencedAssembliesBuilder.Add(referenceMap[i].AliasesOpt);

                    hasRecursiveAliases |= !referenceMap[i].RecursiveAliasesOpt.IsDefault;
                }
            }

            if (hasRecursiveAliases)
            {
                PropagateRecursiveAliases(bindingResult, referenceMap, aliasesOfReferencedAssembliesBuilder);
            }

            Debug.Assert(!aliasesOfReferencedAssembliesBuilder.Any(a => a.IsDefault));

            if (supersedeLowerVersions)
            {
                foreach (var assemblyReference in assemblyReferencesBySimpleName)
                {
                    // the item in the list is the highest version, by construction
                    for (int i = 1; i < assemblyReference.Value.Count; i++)
                    {
                        int assemblyIndex = assemblyReference.Value[i].GetAssemblyIndex(explicitlyReferencedAsemblyCount);
                        aliasesOfReferencedAssembliesBuilder[assemblyIndex] = SupersededAlias;
                    }
                }
            }

            aliasesOfReferencedAssemblies = aliasesOfReferencedAssembliesBuilder.ToImmutableAndFree();
        }

        // #r references are recursive, their aliases should be merged into all their dependencies.
        //
        // For example, if a compilation has a reference to LibA with alias A and the user #r's LibB with alias B,
        // which references LibA, LibA should be available under both aliases A and B. B is usually "global",
        // which means LibA namespaces should become available to the compilation without any qualification when #r LibB 
        // is encountered.
        // 
        // Pairs: (assembly index -- index into bindingResult array; index of the #r reference in referenceMap array).
        private static void PropagateRecursiveAliases(
            BoundInputAssembly[] bindingResult,
            ImmutableArray<ResolvedReference> referenceMap,
            ArrayBuilder<ImmutableArray<string>> aliasesOfReferencedAssembliesBuilder)
        {
            var assemblyIndicesToProcess = ArrayBuilder<int>.GetInstance();
            var visitedAssemblies = BitVector.Create(bindingResult.Length);

            // +1 for assembly being built
            Debug.Assert(bindingResult.Length == aliasesOfReferencedAssembliesBuilder.Count + 1);

            foreach (ResolvedReference reference in referenceMap)
            {
                if (!reference.IsSkipped && !reference.RecursiveAliasesOpt.IsDefault)
                {
                    var recursiveAliases = reference.RecursiveAliasesOpt;

                    Debug.Assert(reference.Kind == MetadataImageKind.Assembly);
                    visitedAssemblies.Clear();
                    
                    Debug.Assert(assemblyIndicesToProcess.Count == 0);
                    assemblyIndicesToProcess.Add(reference.Index);

                    while (assemblyIndicesToProcess.Count > 0)
                    {
                        int assemblyIndex = assemblyIndicesToProcess.Pop();
                        visitedAssemblies[assemblyIndex] = true;

                        // merge aliases:
                        aliasesOfReferencedAssembliesBuilder[assemblyIndex] = MergedAliases.Merge(aliasesOfReferencedAssembliesBuilder[assemblyIndex], recursiveAliases);

                        // push dependencies onto the stack:
                        // +1 for the assembly being built:
                        foreach (var binding in bindingResult[assemblyIndex + 1].ReferenceBinding)
                        {
                            if (binding.IsBound)
                            {
                                // -1 for the assembly being built:
                                int dependentAssemblyIndex = binding.DefinitionIndex - 1;
                                if (!visitedAssemblies[dependentAssemblyIndex])
                                {
                                    assemblyIndicesToProcess.Add(dependentAssemblyIndex);
                                }
                            }
                        }
                    }
                }
            }

            for (int i = 0; i < aliasesOfReferencedAssembliesBuilder.Count; i++)
            {
                if (aliasesOfReferencedAssembliesBuilder[i].IsDefault)
                {
                    aliasesOfReferencedAssembliesBuilder[i] = ImmutableArray<string>.Empty;
                }
            }

            assemblyIndicesToProcess.Free();
        }

        #region Compilation APIs Implementation

        // for testing purposes
        internal IEnumerable<string> ExternAliases => AliasesOfReferencedAssemblies.SelectMany(aliases => aliases);

        internal sealed override IEnumerable<KeyValuePair<MetadataReference, IAssemblySymbol>> GetReferencedAssemblies()
        {
            return ReferencedAssembliesMap.Select(ra => KeyValuePair.Create(ra.Key, (IAssemblySymbol)ReferencedAssemblies[ra.Value]));
        }

        internal TAssemblySymbol GetReferencedAssemblySymbol(MetadataReference reference)
        {
            int index;
            return ReferencedAssembliesMap.TryGetValue(reference, out index) ? ReferencedAssemblies[index] : null;
        }

        internal int GetReferencedModuleIndex(MetadataReference reference)
        {
            int index;
            return ReferencedModuleIndexMap.TryGetValue(reference, out index) ? index : -1;
        }

        /// <summary>
        /// Gets the <see cref="MetadataReference"/> that corresponds to the assembly symbol. 
        /// </summary>
        internal override MetadataReference GetMetadataReference(IAssemblySymbol assemblySymbol)
        {
            foreach (var entry in ReferencedAssembliesMap)
            {
                if ((object)ReferencedAssemblies[entry.Value] == assemblySymbol)
                {
                    return entry.Key;
                }
            }

            return null;
        }

        internal override IEnumerable<ValueTuple<IAssemblySymbol, ImmutableArray<string>>> GetReferencedAssemblyAliases()
        {
            for (int i = 0; i < ReferencedAssemblies.Length; i++)
            {
                yield return ValueTuple.Create((IAssemblySymbol)ReferencedAssemblies[i], AliasesOfReferencedAssemblies[i]);
            }
        }

        public bool DeclarationsAccessibleWithoutAlias(int referencedAssemblyIndex)
        {
            var aliases = AliasesOfReferencedAssemblies[referencedAssemblyIndex];
            return aliases.Length == 0 || aliases.IndexOf(MetadataReferenceProperties.GlobalAlias, StringComparer.Ordinal) >= 0;
        }

        internal override IEnumerable<KeyValuePair<AssemblyIdentity, PortableExecutableReference>> GetImplicitlyResolvedAssemblyReferences()
        {
            foreach (PortableExecutableReference reference in ImplicitReferences)
            {
                yield return KeyValuePair.Create(ReferencedAssemblies[ReferencedAssembliesMap[reference]].Identity, reference);
            }
        }

        #endregion
    }
}
