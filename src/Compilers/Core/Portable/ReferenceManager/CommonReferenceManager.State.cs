// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Symbols;
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
        internal abstract IEnumerable<KeyValuePair<MetadataReference, IAssemblySymbolInternal>> GetReferencedAssemblies();

        /// <summary>
        /// Enumerates all referenced assemblies and their aliases.
        /// </summary>
        internal abstract IEnumerable<(IAssemblySymbolInternal AssemblySymbol, ImmutableArray<string> Aliases)> GetReferencedAssemblyAliases();

        internal abstract MetadataReference? GetMetadataReference(IAssemblySymbolInternal? assemblySymbol);
        internal abstract ImmutableArray<MetadataReference> ExplicitReferences { get; }
        internal abstract ImmutableDictionary<AssemblyIdentity, PortableExecutableReference?> ImplicitReferenceResolutions { get; }
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
        private Dictionary<MetadataReference, int>? _lazyReferencedAssembliesMap;

        /// <summary>
        /// A map from a net-module metadata reference to the index of the corresponding module
        /// symbol in the source assembly symbol for the current compilation.
        /// </summary>
        /// <remarks>
        /// Subtract one from the index (for the manifest module) to find the corresponding elements
        /// of <see cref="_lazyReferencedModules"/> and <see cref="_lazyReferencedModulesReferences"/>.
        /// </remarks>
        private Dictionary<MetadataReference, int>? _lazyReferencedModuleIndexMap;

        /// <summary>
        /// Maps (containing syntax tree file name, reference string) of #r directive to a resolved metadata reference.
        /// If multiple #r's in the same tree use the same value as a reference the resolved metadata reference is the same as well.
        /// </summary>
        private IDictionary<(string, string), MetadataReference>? _lazyReferenceDirectiveMap;

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

        /// <summary>
        /// Stores the results of implicit reference resolutions.
        /// If <see cref="MetadataReferenceResolver.ResolveMissingAssemblies"/> is true the reference manager attempts to resolve assembly identities,
        /// that do not match any explicit metadata references passed to the compilation (or specified via #r directive).
        /// For each such assembly identity <see cref="MetadataReferenceResolver.ResolveMissingAssembly(MetadataReference, AssemblyIdentity)"/> is called
        /// and its result is captured in this map.
        /// The map also stores failures - the reference is null if the assembly of the given identity is not found by the resolver.
        /// This is important to maintain consistency, especially across multiple submissions (e.g. the reference is not found during compilation of the first submission
        /// but then it is available when the second submission is compiled).
        /// </summary>
        private ImmutableDictionary<AssemblyIdentity, PortableExecutableReference?>? _lazyImplicitReferenceResolutions;

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
        private TAssemblySymbol? _lazyCorLibraryOpt;

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
        /// Aliases used by assemblies referenced directly by the source module of the compilation.
        /// </summary>
        /// <remarks>
        /// Aliases <see cref="_lazyAliasesOfReferencedAssemblies"/>[i] are of an assembly <see cref="_lazyReferencedAssemblies"/>[i].
        /// </remarks>
        private ImmutableArray<ImmutableArray<string>> _lazyAliasesOfReferencedAssemblies;

        /// <summary>
        /// A map capturing <see cref="MetadataReference"/>s that were "merged" to a single referenced assembly
        /// associated with a key in the map.
        /// The keys are a subset of keys from <see cref="_lazyReferencedAssembliesMap"/>.
        /// </summary>
        private ImmutableDictionary<MetadataReference, ImmutableArray<MetadataReference>>? _lazyMergedAssemblyReferencesMap;

        /// <summary>
        /// Unified assemblies referenced directly by the source module of the compilation.
        /// </summary>
        private ImmutableArray<UnifiedAssembly<TAssemblySymbol>> _lazyUnifiedAssemblies;

        public CommonReferenceManager(string simpleAssemblyName, AssemblyIdentityComparer identityComparer, Dictionary<MetadataReference, MetadataOrDiagnostic>? observedMetadata)
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

        internal IDictionary<(string, string), MetadataReference> ReferenceDirectiveMap
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

        internal override ImmutableDictionary<AssemblyIdentity, PortableExecutableReference?> ImplicitReferenceResolutions
        {
            get
            {
                AssertBound();
                return _lazyImplicitReferenceResolutions;
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

        internal TAssemblySymbol? CorLibraryOpt
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

        internal ImmutableDictionary<MetadataReference, ImmutableArray<MetadataReference>> MergedAssemblyReferencesMap
        {
            get
            {
                AssertBound();
                Debug.Assert(_lazyMergedAssemblyReferencesMap != null);
                return _lazyMergedAssemblyReferencesMap;
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
            Debug.Assert(_lazyImplicitReferenceResolutions == null);
            Debug.Assert(_lazyExplicitReferences.IsDefault);
            Debug.Assert(_lazyReferencedModules.IsDefault);
            Debug.Assert(_lazyReferencedModulesReferences.IsDefault);
            Debug.Assert(_lazyReferencedAssemblies.IsDefault);
            Debug.Assert(_lazyAliasesOfReferencedAssemblies.IsDefault);
            Debug.Assert(_lazyMergedAssemblyReferencesMap == null);
            Debug.Assert(_lazyUnifiedAssemblies.IsDefault);
            Debug.Assert(_lazyCorLibraryOpt == null);
        }

        [Conditional("DEBUG")]
        [MemberNotNull(nameof(_lazyReferencedAssembliesMap), nameof(_lazyReferencedModuleIndexMap), nameof(_lazyReferenceDirectiveMap), nameof(_lazyImplicitReferenceResolutions))]
        internal void AssertBound()
        {
            Debug.Assert(_isBound != 0);
            Debug.Assert(_lazyHasCircularReference != ThreeState.Unknown);
            Debug.Assert(_lazyReferencedAssembliesMap != null);
            Debug.Assert(_lazyReferencedModuleIndexMap != null);
            Debug.Assert(_lazyReferenceDirectiveMap != null);
            Debug.Assert(!_lazyDirectiveReferences.IsDefault);
            Debug.Assert(_lazyImplicitReferenceResolutions != null);
            Debug.Assert(!_lazyExplicitReferences.IsDefault);
            Debug.Assert(!_lazyReferencedModules.IsDefault);
            Debug.Assert(!_lazyReferencedModulesReferences.IsDefault);
            Debug.Assert(!_lazyReferencedAssemblies.IsDefault);
            Debug.Assert(!_lazyAliasesOfReferencedAssemblies.IsDefault);
            Debug.Assert(_lazyMergedAssemblyReferencesMap != null);
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
            IDictionary<(string, string), MetadataReference> boundReferenceDirectiveMap,
            ImmutableArray<MetadataReference> directiveReferences,
            ImmutableArray<MetadataReference> explicitReferences,
            ImmutableDictionary<AssemblyIdentity, PortableExecutableReference?> implicitReferenceResolutions,
            bool containsCircularReferences,
            ImmutableArray<Diagnostic> diagnostics,
            TAssemblySymbol? corLibraryOpt,
            ImmutableArray<PEModule> referencedModules,
            ImmutableArray<ModuleReferences<TAssemblySymbol>> referencedModulesReferences,
            ImmutableArray<TAssemblySymbol> referencedAssemblies,
            ImmutableArray<ImmutableArray<string>> aliasesOfReferencedAssemblies,
            ImmutableArray<UnifiedAssembly<TAssemblySymbol>> unifiedAssemblies,
            Dictionary<MetadataReference, ImmutableArray<MetadataReference>>? mergedAssemblyReferencesMapOpt)
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
            _lazyImplicitReferenceResolutions = implicitReferenceResolutions;

            _lazyCorLibraryOpt = corLibraryOpt;
            _lazyReferencedModules = referencedModules;
            _lazyReferencedModulesReferences = referencedModulesReferences;
            _lazyReferencedAssemblies = referencedAssemblies;
            _lazyAliasesOfReferencedAssemblies = aliasesOfReferencedAssemblies;
            _lazyMergedAssemblyReferencesMap = mergedAssemblyReferencesMapOpt?.ToImmutableDictionary() ?? ImmutableDictionary<MetadataReference, ImmutableArray<MetadataReference>>.Empty;
            _lazyUnifiedAssemblies = unifiedAssemblies;
            _lazyHasCircularReference = containsCircularReferences.ToThreeState();

            // once we flip this bit the state of the manager is immutable and available to any readers:
            Interlocked.Exchange(ref _isBound, 1);
        }

        /// <summary>
        /// Global namespaces of assembly references that have been superseded by an assembly reference with a higher version are 
        /// hidden behind <see cref="s_supersededAlias"/> to avoid ambiguity when they are accessed from source.
        /// All existing aliases of a superseded assembly are discarded.
        /// </summary>
        private static readonly ImmutableArray<string> s_supersededAlias = ImmutableArray.Create("<superseded>");

        protected static void BuildReferencedAssembliesAndModulesMaps(
            BoundInputAssembly[] bindingResult,
            ImmutableArray<MetadataReference> references,
            ImmutableArray<ResolvedReference> referenceMap,
            int referencedModuleCount,
            int explicitlyReferencedAssemblyCount,
            IReadOnlyDictionary<string, List<ReferencedAssemblyIdentity>> assemblyReferencesBySimpleName,
            bool supersedeLowerVersions,
            out Dictionary<MetadataReference, int> referencedAssembliesMap,
            out Dictionary<MetadataReference, int> referencedModulesMap,
            out ImmutableArray<ImmutableArray<string>> aliasesOfReferencedAssemblies,
            out Dictionary<MetadataReference, ImmutableArray<MetadataReference>>? mergedAssemblyReferencesMapOpt)
        {
            referencedAssembliesMap = new Dictionary<MetadataReference, int>(referenceMap.Length);
            referencedModulesMap = new Dictionary<MetadataReference, int>(referencedModuleCount);
            var aliasesOfReferencedAssembliesBuilder = ArrayBuilder<ImmutableArray<string>>.GetInstance(referenceMap.Length - referencedModuleCount);
            bool hasRecursiveAliases = false;

            mergedAssemblyReferencesMapOpt = null;

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

                    MetadataReference reference = references[i];
                    referencedAssembliesMap.Add(reference, assemblyIndex);
                    aliasesOfReferencedAssembliesBuilder.Add(referenceMap[i].AliasesOpt);

                    if (!referenceMap[i].MergedReferences.IsEmpty)
                    {
                        (mergedAssemblyReferencesMapOpt ??= new Dictionary<MetadataReference, ImmutableArray<MetadataReference>>()).Add(reference, referenceMap[i].MergedReferences);
                    }

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
                        int assemblyIndex = assemblyReference.Value[i].GetAssemblyIndex(explicitlyReferencedAssemblyCount);
                        aliasesOfReferencedAssembliesBuilder[assemblyIndex] = s_supersededAlias;
                    }
                }
            }

            aliasesOfReferencedAssemblies = aliasesOfReferencedAssembliesBuilder.ToImmutableAndFree();
        }

        /// <summary>
        /// Calculates map from the identities of specified symbols to the corresponding identities in the original EnC baseline metadata.
        /// The map only includes an entry for identities that differ, i.e. for symbols representing assembly references of the current compilation that have different identities 
        /// than the corresponding identity in baseline metadata AssemblyRef table. The key comparer of the map ignores build and revision parts of the version number, 
        /// since these might change if the original version included wildcard.
        /// </summary>
        /// <param name="symbols">Assembly symbols for references of the current compilation.</param>
        /// <param name="originalIdentities">Identities in the baseline. <paramref name="originalIdentities"/>[i] corresponds to <paramref name="symbols"/>[i].</param>
        internal static ImmutableDictionary<AssemblyIdentity, AssemblyIdentity> GetAssemblyReferenceIdentityBaselineMap(ImmutableArray<TAssemblySymbol> symbols, ImmutableArray<AssemblyIdentity> originalIdentities)
        {
            Debug.Assert(originalIdentities.Length == symbols.Length);

            ImmutableDictionary<AssemblyIdentity, AssemblyIdentity>.Builder? lazyBuilder = null;
            for (int i = 0; i < originalIdentities.Length; i++)
            {
                var symbolIdentity = symbols[i].Identity;
                var versionPattern = symbols[i].AssemblyVersionPattern;
                var originalIdentity = originalIdentities[i];

                if (versionPattern is object)
                {
                    Debug.Assert(versionPattern.Build == ushort.MaxValue || versionPattern.Revision == ushort.MaxValue);

                    lazyBuilder = lazyBuilder ?? ImmutableDictionary.CreateBuilder<AssemblyIdentity, AssemblyIdentity>();

                    var sourceIdentity = symbolIdentity.WithVersion(versionPattern);

                    if (lazyBuilder.ContainsKey(sourceIdentity))
                    {
                        // The compilation references multiple assemblies whose versions only differ in auto-generated build and/or revision numbers.
                        throw new NotSupportedException(CodeAnalysisResources.CompilationReferencesAssembliesWithDifferentAutoGeneratedVersion);
                    }

                    lazyBuilder.Add(sourceIdentity, originalIdentity);
                }
                else
                {
                    // by construction of the arguments:
                    Debug.Assert(originalIdentity == symbolIdentity);
                }
            }

            return lazyBuilder?.ToImmutable() ?? ImmutableDictionary<AssemblyIdentity, AssemblyIdentity>.Empty;
        }

        internal static bool CompareVersionPartsSpecifiedInSource(Version version, Version candidateVersion, TAssemblySymbol candidateSymbol)
        {
            // major and minor parts must match exactly

            if (version.Major != candidateVersion.Major || version.Minor != candidateVersion.Minor)
            {
                return false;
            }

            // build and revision parts can differ only if the corresponding source versions were auto-generated:
            var versionPattern = candidateSymbol.AssemblyVersionPattern;
            Debug.Assert(versionPattern is null || versionPattern.Build == ushort.MaxValue || versionPattern.Revision == ushort.MaxValue);

            if ((versionPattern is null || versionPattern.Build < ushort.MaxValue) && version.Build != candidateVersion.Build)
            {
                return false;
            }

            if (versionPattern is null && version.Revision != candidateVersion.Revision)
            {
                return false;
            }

            return true;
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
                        var referenceBinding = bindingResult[assemblyIndex + 1].ReferenceBinding;
                        Debug.Assert(referenceBinding is object);
                        foreach (var binding in referenceBinding)
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

        internal sealed override IEnumerable<KeyValuePair<MetadataReference, IAssemblySymbolInternal>> GetReferencedAssemblies()
        {
            return ReferencedAssembliesMap.Select(ra => KeyValuePairUtil.Create(ra.Key, (IAssemblySymbolInternal)ReferencedAssemblies[ra.Value]));
        }

        internal TAssemblySymbol? GetReferencedAssemblySymbol(MetadataReference reference)
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
        internal override MetadataReference? GetMetadataReference(IAssemblySymbolInternal? assemblySymbol)
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

        internal override IEnumerable<(IAssemblySymbolInternal AssemblySymbol, ImmutableArray<string> Aliases)> GetReferencedAssemblyAliases()
        {
            for (int i = 0; i < ReferencedAssemblies.Length; i++)
            {
                yield return (ReferencedAssemblies[i], AliasesOfReferencedAssemblies[i]);
            }
        }

        public bool DeclarationsAccessibleWithoutAlias(int referencedAssemblyIndex)
        {
            var aliases = AliasesOfReferencedAssemblies[referencedAssemblyIndex];
            return aliases.Length == 0 || aliases.IndexOf(MetadataReferenceProperties.GlobalAlias, StringComparer.Ordinal) >= 0;
        }

        #endregion
    }
}
