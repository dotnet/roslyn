// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Collections;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    using MetadataOrDiagnostic = System.Object;

    /// <summary>
    /// The base class for language specific assembly managers.
    /// </summary>
    /// <typeparam name="TCompilation">Language specific representation for a compilation</typeparam>
    /// <typeparam name="TAssemblySymbol">Language specific representation for an assembly symbol.</typeparam>
    internal abstract partial class CommonReferenceManager<TCompilation, TAssemblySymbol>
        where TCompilation : Compilation
        where TAssemblySymbol : class, IAssemblySymbol
    {
        protected abstract CommonMessageProvider MessageProvider { get; }

        protected abstract AssemblyData CreateAssemblyDataForFile(
            PEAssembly assembly,
            WeakList<IAssemblySymbol> cachedSymbols,
            DocumentationProvider documentationProvider,
            string sourceAssemblySimpleName,
            MetadataImportOptions importOptions,
            bool embedInteropTypes);

        protected abstract AssemblyData CreateAssemblyDataForCompilation(
            CompilationReference compilationReference);

        /// <summary>
        /// Checks if the properties of <paramref name="duplicateReference"/> are compatible with properties of <paramref name="primaryReference"/>.
        /// Reports inconsistencies to the given diagnostic bag.
        /// </summary>
        /// <returns>True if the properties are compatible and hence merged, false if the duplicate reference should not merge it's properties with primary reference.</returns>
        protected abstract bool CheckPropertiesConsistency(MetadataReference primaryReference, MetadataReference duplicateReference, DiagnosticBag diagnostics);

        /// <summary>
        /// Called to compare two weakly named identities with the same name.
        /// </summary>
        protected abstract bool WeakIdentityPropertiesEquivalent(AssemblyIdentity identity1, AssemblyIdentity identity2);

        [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
        protected struct ResolvedReference
        {
            private readonly MetadataImageKind _kind;
            private readonly int _index;
            private readonly ImmutableArray<string> _aliasesOpt;
            private readonly ImmutableArray<string> _recursiveAliasesOpt;

            // uninitialized aliases
            public ResolvedReference(int index, MetadataImageKind kind)
            {
                Debug.Assert(index >= 0);
                _index = index + 1;
                _kind = kind;
            }

            // initialized aliases
            public ResolvedReference(int index, MetadataImageKind kind, ImmutableArray<string> aliasesOpt, ImmutableArray<string> recursiveAliasesOpt)
                : this(index, kind)
            {
                // We have to have non-default aliases (empty are ok). We can have both recursive and non-recursive aliases if two references were merged.
                Debug.Assert(!aliasesOpt.IsDefault || !recursiveAliasesOpt.IsDefault);

                _aliasesOpt = aliasesOpt;
                _recursiveAliasesOpt = recursiveAliasesOpt;
            }

            private bool IsUninitialized => _aliasesOpt.IsDefault && _recursiveAliasesOpt.IsDefault;

            /// <summary>
            /// Aliases that should be applied to the referenced assembly. 
            /// Empty array means {"global"} (all namespaces and types in the global namespace of the assembly are accessible without qualification).
            /// Null if not applicable (the reference only has recursive aliases).
            /// </summary>
            public ImmutableArray<string> AliasesOpt
            {
                get
                {
                    Debug.Assert(!IsUninitialized);
                    return _aliasesOpt;
                }
            }

            /// <summary>
            /// Aliases that should be applied recursively to all dependent assemblies. 
            /// Empty array means {"global"} (all namespaces and types in the global namespace of the assembly are accessible without qualification).
            /// Null if not applicable (the reference only has simple aliases).
            /// </summary>
            public ImmutableArray<string> RecursiveAliasesOpt
            {
                get
                {
                    Debug.Assert(!IsUninitialized);
                    return _recursiveAliasesOpt;
                }
            }

            /// <summary>
            /// default(<see cref="ResolvedReference"/>) is considered skipped.
            /// </summary>
            public bool IsSkipped
            {
                get
                {
                    return _index == 0;
                }
            }

            public MetadataImageKind Kind
            {
                get
                {
                    Debug.Assert(!IsSkipped);
                    return _kind;
                }
            }

            /// <summary>
            /// Index into an array of assemblies (not including the assembly being built) or an array of modules, depending on <see cref="Kind"/>.
            /// </summary>
            public int Index
            {
                get
                {
                    Debug.Assert(!IsSkipped);
                    return _index - 1;
                }
            }

            private string GetDebuggerDisplay()
            {
                return IsSkipped ? "<skipped>" : $"{(_kind == MetadataImageKind.Assembly ? "A" : "M")}[{Index}]:{DisplayAliases(_aliasesOpt, "aliases")}{DisplayAliases(_recursiveAliasesOpt, "recursive-aliases")}";
            }

            private static string DisplayAliases(ImmutableArray<string> aliasesOpt, string name)
            {
                return aliasesOpt.IsDefault ? "" : $" {name} = '{string.Join("','", aliasesOpt)}'";
            }
        }

        protected struct ReferencedAssemblyIdentity
        {
            public readonly AssemblyIdentity Identity;
            public readonly MetadataReference Reference;

            /// <summary>
            /// non-negative: Index into the array of all (explicitly and implicitly) referenced assemblies.
            /// negative: ExplicitlyReferencedAssemblies.Count + RelativeAssemblyIndex is an index into the array of assemblies.
            /// </summary>
            public readonly int RelativeAssemblyIndex;

            public int GetAssemblyIndex(int explicitlyReferencedAssemblyCount) => 
                RelativeAssemblyIndex >= 0 ? RelativeAssemblyIndex : explicitlyReferencedAssemblyCount + RelativeAssemblyIndex;

            public ReferencedAssemblyIdentity(AssemblyIdentity identity, MetadataReference reference, int relativeAssemblyIndex)
            {
                Identity = identity;
                Reference = reference;
                RelativeAssemblyIndex = relativeAssemblyIndex;
            }
        }

        /// <summary>
        /// Resolves given metadata references to assemblies and modules.
        /// </summary>
        /// <param name="compilation">The compilation whose references are being resolved.</param>
        /// <param name="assemblyReferencesBySimpleName">
        /// Used to filter out assemblies that have the same strong or weak identity.
        /// Maps simple name to a list of identities. The highest version of each name is the first.
        /// </param>
        /// <param name="references">List where to store resolved references. References from #r directives will follow references passed to the compilation constructor.</param>
        /// <param name="boundReferenceDirectiveMap">Maps #r values to successfully resolved metadata references. Does not contain values that failed to resolve.</param>
        /// <param name="boundReferenceDirectives">Unique metadata references resolved from #r directives.</param>
        /// <param name="assemblies">List where to store information about resolved assemblies to.</param>
        /// <param name="modules">List where to store information about resolved modules to.</param>
        /// <param name="diagnostics">Diagnostic bag where to report resolution errors.</param>
        /// <returns>
        /// Maps index to <paramref name="references"/> to an index of a resolved assembly or module in <paramref name="assemblies"/> or <paramref name="modules"/>, respectively.
        ///</returns>
        protected ImmutableArray<ResolvedReference> ResolveMetadataReferences(
            TCompilation compilation,
            [Out] Dictionary<string, List<ReferencedAssemblyIdentity>> assemblyReferencesBySimpleName,
            out ImmutableArray<MetadataReference> references,
            out IDictionary<ValueTuple<string, string>, MetadataReference> boundReferenceDirectiveMap,
            out ImmutableArray<MetadataReference> boundReferenceDirectives,
            out ImmutableArray<AssemblyData> assemblies,
            out ImmutableArray<PEModule> modules,
            DiagnosticBag diagnostics)
        {
            // Locations of all #r directives in the order they are listed in the references list.
            ImmutableArray<Location> referenceDirectiveLocations;
            GetCompilationReferences(compilation, diagnostics, out references, out boundReferenceDirectiveMap, out referenceDirectiveLocations);

            // References originating from #r directives precede references supplied as arguments of the compilation.
            int referenceCount = references.Length;
            int referenceDirectiveCount = (referenceDirectiveLocations != null ? referenceDirectiveLocations.Length : 0);

            var referenceMap = new ResolvedReference[referenceCount];

            // Maps references that were added to the reference set (i.e. not filtered out as duplicates) to a set of names that 
            // can be used to alias these references. Duplicate assemblies contribute their aliases into this set.
            Dictionary<MetadataReference, MergedAliases> lazyAliasMap = null;

            // Used to filter out duplicate references that reference the same file (resolve to the same full normalized path).
            var boundReferences = new Dictionary<MetadataReference, MetadataReference>(MetadataReferenceEqualityComparer.Instance);
            
            ArrayBuilder<MetadataReference> uniqueDirectiveReferences = (referenceDirectiveLocations != null) ? ArrayBuilder<MetadataReference>.GetInstance() : null;
            var assembliesBuilder = ArrayBuilder<AssemblyData>.GetInstance();
            ArrayBuilder<PEModule> lazyModulesBuilder = null;

            bool supersedeLowerVersions = compilation.IsSubmission;

            // When duplicate references with conflicting EmbedInteropTypes flag are encountered,
            // VB uses the flag from the last one, C# reports an error. We need to enumerate in reverse order
            // so that we find the one that matters first.
            for (int referenceIndex = referenceCount - 1; referenceIndex >= 0; referenceIndex--)
            {
                var boundReference = references[referenceIndex];
                if (boundReference == null)
                {
                    continue;
                }

                // add bound reference if it doesn't exist yet, merging aliases:
                MetadataReference existingReference;
                if (boundReferences.TryGetValue(boundReference, out existingReference))
                {
                    // merge properties of compilation-based references if the underlying compilations are the same
                    if ((object)boundReference != existingReference)
                    {
                        MergeReferenceProperties(existingReference, boundReference, diagnostics, ref lazyAliasMap);
                    }

                    continue;
                }

                boundReferences.Add(boundReference, boundReference);

                Location location;
                if (referenceIndex < referenceDirectiveCount)
                {
                    location = referenceDirectiveLocations[referenceIndex];
                    uniqueDirectiveReferences.Add(boundReference);
                }
                else
                {
                    location = Location.None;
                }

                // compilation reference

                var compilationReference = boundReference as CompilationReference;
                if (compilationReference != null)
                {
                    switch (compilationReference.Properties.Kind)
                    {
                        case MetadataImageKind.Assembly:
                            existingReference = TryAddAssembly(
                                compilationReference.Compilation.Assembly.Identity, 
                                boundReference,
                                -assembliesBuilder.Count - 1,
                                diagnostics,
                                location,
                                assemblyReferencesBySimpleName,
                                supersedeLowerVersions);

                            if (existingReference != null)
                            {
                                MergeReferenceProperties(existingReference, boundReference, diagnostics, ref lazyAliasMap);
                                continue;
                            }

                            // Note, if SourceAssemblySymbol hasn't been created for 
                            // compilationAssembly.Compilation yet, we want this to happen 
                            // right now. Conveniently, this constructor will trigger creation of the 
                            // SourceAssemblySymbol.
                            var asmData = CreateAssemblyDataForCompilation(compilationReference);
                            AddAssembly(asmData, referenceIndex, referenceMap, assembliesBuilder);
                            break;

                        default:
                            throw ExceptionUtilities.UnexpectedValue(compilationReference.Properties.Kind);
                    }

                    continue;
                }

                // PE reference

                var peReference = (PortableExecutableReference)boundReference;
                Metadata metadata = GetMetadata(peReference, MessageProvider, location, diagnostics);
                Debug.Assert(metadata != null || diagnostics.HasAnyErrors());

                if (metadata != null)
                {
                    Debug.Assert(metadata != null);

                    switch (peReference.Properties.Kind)
                    {
                        case MetadataImageKind.Assembly:
                            var assemblyMetadata = (AssemblyMetadata)metadata;
                            WeakList<IAssemblySymbol> cachedSymbols = assemblyMetadata.CachedSymbols;

                            if (assemblyMetadata.IsValidAssembly())
                            {
                                PEAssembly assembly = assemblyMetadata.GetAssembly();
                                existingReference = TryAddAssembly(
                                    assembly.Identity, 
                                    peReference, 
                                    -assembliesBuilder.Count - 1,
                                    diagnostics,
                                    location,
                                    assemblyReferencesBySimpleName,
                                    supersedeLowerVersions);

                                if (existingReference != null)
                                {
                                    MergeReferenceProperties(existingReference, boundReference, diagnostics, ref lazyAliasMap);
                                    continue;
                                }

                                var asmData = CreateAssemblyDataForFile(
                                    assembly,
                                    cachedSymbols,
                                    peReference.DocumentationProvider,
                                    SimpleAssemblyName,
                                    compilation.Options.MetadataImportOptions,
                                    peReference.Properties.EmbedInteropTypes);

                                AddAssembly(asmData, referenceIndex, referenceMap, assembliesBuilder);
                            }
                            else
                            {
                                diagnostics.Add(MessageProvider.CreateDiagnostic(MessageProvider.ERR_MetadataFileNotAssembly, location, peReference.Display));
                            }

                            // asmData keeps strong ref after this point
                            GC.KeepAlive(assemblyMetadata);
                            break;

                        case MetadataImageKind.Module:
                            var moduleMetadata = (ModuleMetadata)metadata;
                            if (moduleMetadata.Module.IsLinkedModule)
                            {
                                // We don't support netmodules since some checks in the compiler need information from the full PE image
                                // (Machine, Bit32Required, PE image hash).
                                if (!moduleMetadata.Module.IsEntireImageAvailable)
                                {
                                    diagnostics.Add(MessageProvider.CreateDiagnostic(MessageProvider.ERR_LinkedNetmoduleMetadataMustProvideFullPEImage, location, peReference.Display));
                                }

                                AddModule(moduleMetadata.Module, referenceIndex, referenceMap, ref lazyModulesBuilder);
                            }
                            else
                            {
                                diagnostics.Add(MessageProvider.CreateDiagnostic(MessageProvider.ERR_MetadataFileNotModule, location, peReference.Display));
                            }
                            break;

                        default:
                            throw ExceptionUtilities.UnexpectedValue(peReference.Properties.Kind);
                    }
                }
            }

            if (uniqueDirectiveReferences != null)
            {
                uniqueDirectiveReferences.ReverseContents();
                boundReferenceDirectives = uniqueDirectiveReferences.ToImmutableAndFree();
            }
            else
            {
                boundReferenceDirectives = ImmutableArray<MetadataReference>.Empty;
            }

            // We enumerated references in reverse order in the above code
            // and thus assemblies and modules in the builders are reversed.
            // Fix up all the indices and reverse the builder content now to get 
            // the ordering matching the references.
            // 
            // Also fills in aliases.

            for (int i = 0; i < referenceMap.Length; i++)
            {
                if (!referenceMap[i].IsSkipped)
                {
                    int count = (referenceMap[i].Kind == MetadataImageKind.Assembly) ? assembliesBuilder.Count : lazyModulesBuilder?.Count ?? 0;

                    int reversedIndex = count - 1 - referenceMap[i].Index;
                    referenceMap[i] = GetResolvedReferenceAndFreePropertyMapEntry(references[i], reversedIndex, referenceMap[i].Kind, lazyAliasMap);
                }
            }

            assembliesBuilder.ReverseContents();
            assemblies = assembliesBuilder.ToImmutableAndFree();
            
            if (lazyModulesBuilder == null)
            {
                modules = ImmutableArray<PEModule>.Empty;
            }
            else
            {
                lazyModulesBuilder.ReverseContents();
                modules = lazyModulesBuilder.ToImmutableAndFree();
            }

            return ImmutableArray.CreateRange(referenceMap);
        }

        private static ResolvedReference GetResolvedReferenceAndFreePropertyMapEntry(MetadataReference reference, int index, MetadataImageKind kind, Dictionary<MetadataReference, MergedAliases> propertyMapOpt)
        {
            ImmutableArray<string> aliasesOpt, recursiveAliasesOpt;

            MergedAliases mergedProperties;
            if (propertyMapOpt != null && propertyMapOpt.TryGetValue(reference, out mergedProperties))
            {
                aliasesOpt = mergedProperties.AliasesOpt?.ToImmutableAndFree() ?? default(ImmutableArray<string>);
                recursiveAliasesOpt = mergedProperties.RecursiveAliasesOpt?.ToImmutableAndFree() ?? default(ImmutableArray<string>);
            }
            else if (reference.Properties.HasRecursiveAliases)
            {
                aliasesOpt = default(ImmutableArray<string>);
                recursiveAliasesOpt = reference.Properties.Aliases;
            }
            else
            {
                aliasesOpt = reference.Properties.Aliases;
                recursiveAliasesOpt = default(ImmutableArray<string>);
            }

            return new ResolvedReference(index, kind, aliasesOpt, recursiveAliasesOpt);
        }

        /// <summary>
        /// Creates or gets metadata for PE reference.
        /// </summary>
        /// <remarks>
        /// If any of the following exceptions: <see cref="BadImageFormatException"/>, <see cref="FileNotFoundException"/>, <see cref="IOException"/>,
        /// are thrown while reading the metadata file, the exception is caught and an appropriate diagnostic stored in <paramref name="diagnostics"/>.
        /// </remarks>
        private Metadata GetMetadata(PortableExecutableReference peReference, CommonMessageProvider messageProvider, Location location, DiagnosticBag diagnostics)
        {
            Metadata existingMetadata;

            lock (ObservedMetadata)
            {
                if (TryGetObservedMetadata(peReference, diagnostics, out existingMetadata))
                {
                    return existingMetadata;
                }
            }

            Metadata newMetadata;
            Diagnostic newDiagnostic = null;
            try
            {
                newMetadata = peReference.GetMetadata();

                // make sure basic structure of the PE image is valid:
                var assemblyMetadata = newMetadata as AssemblyMetadata;
                if (assemblyMetadata != null)
                {
                    bool dummy = assemblyMetadata.IsValidAssembly();
                }
                else
                {
                    bool dummy = ((ModuleMetadata)newMetadata).Module.IsLinkedModule;
                }
            }
            catch (Exception e) when (e is BadImageFormatException || e is IOException)
            {
                newDiagnostic = PortableExecutableReference.ExceptionToDiagnostic(e, messageProvider, location, peReference.Display, peReference.Properties.Kind);
                newMetadata = null;
            }

            lock (ObservedMetadata)
            {
                if (TryGetObservedMetadata(peReference, diagnostics, out existingMetadata))
                {
                    return existingMetadata;
                }

                if (newDiagnostic != null)
                {
                    diagnostics.Add(newDiagnostic);
                }

                ObservedMetadata.Add(peReference, (MetadataOrDiagnostic)newMetadata ?? newDiagnostic);
                return newMetadata;
            }
        }

        private bool TryGetObservedMetadata(PortableExecutableReference peReference, DiagnosticBag diagnostics, out Metadata metadata)
        {
            MetadataOrDiagnostic existing;
            if (ObservedMetadata.TryGetValue(peReference, out existing))
            {
                Debug.Assert(existing is Metadata || existing is Diagnostic);

                metadata = existing as Metadata;
                if (metadata == null)
                {
                    diagnostics.Add((Diagnostic)existing);
                }

                return true;
            }

            metadata = null;
            return false;
        }

        /// <summary>
        /// Determines whether references are the same. Compilation references are the same if they refer to the same compilation.
        /// Otherwise, references are represented by their object identities.
        /// </summary>
        internal sealed class MetadataReferenceEqualityComparer : IEqualityComparer<MetadataReference>
        {
            internal static readonly MetadataReferenceEqualityComparer Instance = new MetadataReferenceEqualityComparer();

            public bool Equals(MetadataReference x, MetadataReference y)
            {
                if (ReferenceEquals(x, y))
                {
                    return true;
                }

                var cx = x as CompilationReference;
                if (cx != null)
                {
                    var cy = y as CompilationReference;
                    if (cy != null)
                    {
                        return (object)cx.Compilation == cy.Compilation;
                    }
                }

                return false;
            }

            public int GetHashCode(MetadataReference reference)
            {
                var compilationReference = reference as CompilationReference;
                if (compilationReference != null)
                {
                    return RuntimeHelpers.GetHashCode(compilationReference.Compilation);
                }

                return RuntimeHelpers.GetHashCode(reference);
            }
        }

        /// <summary>
        /// Merges aliases of the first observed reference (<paramref name="primaryReference"/>) with aliases specified for an equivalent reference (<paramref name="newReference"/>).
        /// Empty alias list is considered to be the same as a list containing "global", since in both cases C# allows unqualified access to the symbols.
        /// </summary>
        private void MergeReferenceProperties(MetadataReference primaryReference, MetadataReference newReference, DiagnosticBag diagnostics, ref Dictionary<MetadataReference, MergedAliases> lazyAliasMap)
        {
            if (!CheckPropertiesConsistency(newReference, primaryReference, diagnostics))
            {
                return;
            }

            if (lazyAliasMap == null)
            {
                lazyAliasMap = new Dictionary<MetadataReference, MergedAliases>();
            }

            MergedAliases mergedAliases;
            if (!lazyAliasMap.TryGetValue(primaryReference, out mergedAliases))
            {
                mergedAliases = new MergedAliases();
                lazyAliasMap.Add(primaryReference, mergedAliases);
                mergedAliases.Merge(primaryReference);
            }

            mergedAliases.Merge(newReference);
        }

        /// <remarks>
        /// Caller is responsible for freeing any allocated ArrayBuilders.
        /// </remarks>
        private static void AddAssembly(AssemblyData data, int referenceIndex, ResolvedReference[] referenceMap, ArrayBuilder<AssemblyData> assemblies)
        {
            // aliases will be filled in later:
            referenceMap[referenceIndex] = new ResolvedReference(assemblies.Count, MetadataImageKind.Assembly);
            assemblies.Add(data);
        }

        /// <remarks>
        /// Caller is responsible for freeing any allocated ArrayBuilders.
        /// </remarks>
        private static void AddModule(PEModule module, int referenceIndex, ResolvedReference[] referenceMap, ref ArrayBuilder<PEModule> modules)
        {
            if (modules == null)
            {
                modules = ArrayBuilder<PEModule>.GetInstance();
            }

            referenceMap[referenceIndex] = new ResolvedReference(modules.Count, MetadataImageKind.Module);
            modules.Add(module);
        }

        /// <summary>
        /// Returns null if an assembly of an equivalent identity has not been added previously, otherwise returns the reference that added it.
        /// Two identities are considered equivalent if
        /// - both assembly names are strong (have keys) and are either equal or FX unified 
        /// - both assembly names are weak (no keys) and have the same simple name.
        /// </summary>
        private MetadataReference TryAddAssembly(
            AssemblyIdentity identity, 
            MetadataReference reference, 
            int assemblyIndex, 
            DiagnosticBag diagnostics, 
            Location location,
            Dictionary<string, List<ReferencedAssemblyIdentity>> referencesBySimpleName,
            bool supersedeLowerVersions)
        {
            var referencedAssembly = new ReferencedAssemblyIdentity(identity, reference, assemblyIndex);

            List<ReferencedAssemblyIdentity> sameSimpleNameIdentities;
            if (!referencesBySimpleName.TryGetValue(identity.Name, out sameSimpleNameIdentities))
            {
                referencesBySimpleName.Add(identity.Name, new List<ReferencedAssemblyIdentity> { referencedAssembly });
                return null;
            }
            
            if (supersedeLowerVersions)
            {
                foreach (var other in sameSimpleNameIdentities)
                {
                    if (identity.Version == other.Identity.Version)
                    {
                        return other.Reference;
                    }
                }

                // Keep all versions of the assembly and the first identity in the list the one with the highest version:
                if (sameSimpleNameIdentities[0].Identity.Version > identity.Version)
                {
                    sameSimpleNameIdentities.Add(referencedAssembly);
                }
                else
                {
                    sameSimpleNameIdentities.Add(sameSimpleNameIdentities[0]);
                    sameSimpleNameIdentities[0] = referencedAssembly;
                }

                return null;
            }

            ReferencedAssemblyIdentity equivalent = default(ReferencedAssemblyIdentity);
            if (identity.IsStrongName)
            {
                foreach (var other in sameSimpleNameIdentities)
                {
                    // Only compare strong with strong (weak is never equivalent to strong and vice versa).
                    // In order to eliminate duplicate references we need to try to match their identities in both directions since 
                    // ReferenceMatchesDefinition is not necessarily symmetric.
                    // (e.g. System.Numerics.Vectors, Version=4.1+ matches System.Numerics.Vectors, Version=4.0, but not the other way around.)
                    if (other.Identity.IsStrongName && 
                        IdentityComparer.ReferenceMatchesDefinition(identity, other.Identity) &&
                        IdentityComparer.ReferenceMatchesDefinition(other.Identity, identity))
                    {
                        equivalent = other;
                        break;
                    }
                }
            }
            else
            {
                foreach (var other in sameSimpleNameIdentities)
                {
                    // only compare weak with weak
                    if (!other.Identity.IsStrongName && WeakIdentityPropertiesEquivalent(identity, other.Identity))
                    {
                        equivalent = other;
                        break;
                    }
                }
            }

            if (equivalent.Identity == null)
            {
                sameSimpleNameIdentities.Add(referencedAssembly);
                return null;
            }

            // equivalent found - ignore and/or report an error:

            if (identity.IsStrongName)
            {
                Debug.Assert(equivalent.Identity.IsStrongName);

                // versions might have been unified for a Framework assembly:
                if (identity != equivalent.Identity)
                {
                    // Dev12 C# reports an error
                    // Dev12 VB keeps both references in the compilation and reports an ambiguity error when a symbol is used.
                    // BREAKING CHANGE in VB: we report an error for both languages

                    // Multiple assemblies with equivalent identity have been imported: '{0}' and '{1}'. Remove one of the duplicate references.
                    MessageProvider.ReportDuplicateMetadataReferenceStrong(diagnostics, location, reference, identity, equivalent.Reference, equivalent.Identity);
                }
                // If the versions match exactly we ignore duplicates w/o reporting errors while 
                // Dev12 C# reports:
                //   error CS1703: An assembly with the same identity '{0}' has already been imported. Try removing one of the duplicate references.
                // Dev12 VB reports:
                //   Fatal error BC2000 : compiler initialization failed unexpectedly: Project already has a reference to assembly System. 
                //   A second reference to 'D:\Temp\System.dll' cannot be added.
            }
            else
            {
                Debug.Assert(!equivalent.Identity.IsStrongName);

                // Dev12 reports an error for all weak-named assemblies, even if the versions are the same.
                // We treat assemblies with the same name and version equal even if they don't have a strong name.
                // This change allows us to de-duplicate #r references based on identities rather than full paths,
                // and is closer to platforms that don't support strong names and consider name and version enough
                // to identify an assembly. An identity without version is considered to have version 0.0.0.0.

                if (identity != equivalent.Identity)
                {
                    MessageProvider.ReportDuplicateMetadataReferenceWeak(diagnostics, location, reference, identity, equivalent.Reference, equivalent.Identity);
                }
            }

            Debug.Assert(equivalent.Reference != null);
            return equivalent.Reference;
        }

        protected void GetCompilationReferences(
            TCompilation compilation,
            DiagnosticBag diagnostics,
            out ImmutableArray<MetadataReference> references,
            out IDictionary<ValueTuple<string, string>, MetadataReference> boundReferenceDirectives,
            out ImmutableArray<Location> referenceDirectiveLocations)
        {
            boundReferenceDirectives = null;

            ArrayBuilder<MetadataReference> referencesBuilder = ArrayBuilder<MetadataReference>.GetInstance();
            ArrayBuilder<Location> referenceDirectiveLocationsBuilder = null;

            try
            {
                foreach (var referenceDirective in compilation.ReferenceDirectives)
                {
                    if (compilation.Options.MetadataReferenceResolver == null)
                    {
                        diagnostics.Add(MessageProvider.CreateDiagnostic(MessageProvider.ERR_MetadataReferencesNotSupported, referenceDirective.Location));
                        break;
                    }

                    // we already successfully bound #r with the same value:
                    if (boundReferenceDirectives != null && boundReferenceDirectives.ContainsKey(ValueTuple.Create(referenceDirective.Location.SourceTree.FilePath, referenceDirective.File)))
                    {
                        continue;
                    }

                    MetadataReference boundReference = ResolveReferenceDirective(referenceDirective.File, referenceDirective.Location, compilation);
                    if (boundReference == null)
                    {
                        diagnostics.Add(MessageProvider.CreateDiagnostic(MessageProvider.ERR_MetadataFileNotFound, referenceDirective.Location, referenceDirective.File));
                        continue;
                    }

                    if (boundReferenceDirectives == null)
                    {
                        boundReferenceDirectives = new Dictionary<ValueTuple<string, string>, MetadataReference>();
                        referenceDirectiveLocationsBuilder = ArrayBuilder<Location>.GetInstance();
                    }

                    referencesBuilder.Add(boundReference);
                    referenceDirectiveLocationsBuilder.Add(referenceDirective.Location);
                    boundReferenceDirectives.Add(ValueTuple.Create(referenceDirective.Location.SourceTree.FilePath, referenceDirective.File), boundReference);
                }

                // add external reference at the end, so that they are processed first:
                referencesBuilder.AddRange(compilation.ExternalReferences);

                // Add all explicit references of the previous script compilation.
                var previousScriptCompilation = compilation.ScriptCompilationInfo?.PreviousScriptCompilation;
                if (previousScriptCompilation != null)
                {
                    referencesBuilder.AddRange(previousScriptCompilation.GetBoundReferenceManager().ExplicitReferences);
                }

                if (boundReferenceDirectives == null)
                {
                    // no directive references resolved successfully:
                    boundReferenceDirectives = SpecializedCollections.EmptyDictionary<ValueTuple<string, string>, MetadataReference>();
                }

                references = referencesBuilder.ToImmutable();
                referenceDirectiveLocations = referenceDirectiveLocationsBuilder?.ToImmutableAndFree() ?? ImmutableArray<Location>.Empty;
            }
            finally
            {
                // Put this in a finally because we have tests that (intentionally) cause ResolveReferenceDirective to throw and 
                // we don't want to clutter the test output with leak reports.
                referencesBuilder.Free();
            }
        }

        /// <summary>
        /// For each given directive return a bound PE reference, or null if the binding fails.
        /// </summary>
        private static PortableExecutableReference ResolveReferenceDirective(string reference, Location location, TCompilation compilation)
        {
            var tree = location.SourceTree;
            string basePath = (tree != null && tree.FilePath.Length > 0) ? tree.FilePath : null;

            // checked earlier:
            Debug.Assert(compilation.Options.MetadataReferenceResolver != null);

            var references = compilation.Options.MetadataReferenceResolver.ResolveReference(reference, basePath, MetadataReferenceProperties.Assembly.WithRecursiveAliases(true));
            if (references.IsDefaultOrEmpty)
            {
                return null;
            }

            if (references.Length > 1)
            {
                // TODO: implement
                throw new NotSupportedException();
            }

            return references[0];
        }

        internal static AssemblyReferenceBinding[] ResolveReferencedAssemblies(
            ImmutableArray<AssemblyIdentity> references,
            ImmutableArray<AssemblyData> definitions,
            int definitionStartIndex,
            AssemblyIdentityComparer assemblyIdentityComparer)
        {
            var boundReferences = new AssemblyReferenceBinding[references.Length];
            for (int j = 0; j < references.Length; j++)
            {
                boundReferences[j] = ResolveReferencedAssembly(references[j], definitions, definitionStartIndex, assemblyIdentityComparer);
            }

            return boundReferences;
        }

        /// <summary>
        /// Used to match AssemblyRef with AssemblyDef.
        /// </summary>
        /// <param name="definitions">Array of definition identities to match against.</param>
        /// <param name="definitionStartIndex">An index of the first definition to consider, <paramref name="definitions"/> preceding this index are ignored.</param>
        /// <param name="reference">Reference identity to resolve.</param>
        /// <param name="assemblyIdentityComparer">Assembly identity comparer.</param>
        /// <returns>
        /// Returns an index the reference is bound.
        /// </returns>
        internal static AssemblyReferenceBinding ResolveReferencedAssembly(
            AssemblyIdentity reference,
            ImmutableArray<AssemblyData> definitions,
            int definitionStartIndex,
            AssemblyIdentityComparer assemblyIdentityComparer)
        {
            // Dev11 C# compiler allows the versions to not match exactly, assuming that a newer library may be used instead of an older version.
            // For a given reference it finds a definition with the lowest version that is higher then or equal to the reference version.
            // If match.Version != reference.Version a warning is reported.

            // definition with the lowest version higher than reference version, unless exact version found
            int minHigherVersionDefinition = -1;
            int maxLowerVersionDefinition = -1;

            // Skip assembly being built for now; it will be considered at the very end:
            bool resolveAgainstAssemblyBeingBuilt = definitionStartIndex == 0;
            definitionStartIndex = Math.Max(definitionStartIndex, 1);

            for (int i = definitionStartIndex; i < definitions.Length; i++)
            {
                AssemblyIdentity definition = definitions[i].Identity;

                switch (assemblyIdentityComparer.Compare(reference, definition))
                {
                    case AssemblyIdentityComparer.ComparisonResult.NotEquivalent:
                        continue;

                    case AssemblyIdentityComparer.ComparisonResult.Equivalent:
                        return new AssemblyReferenceBinding(reference, i);

                    case AssemblyIdentityComparer.ComparisonResult.EquivalentIgnoringVersion:
                        if (reference.Version < definition.Version)
                        {
                            // Refers to an older assembly than we have
                            if (minHigherVersionDefinition == -1 || definition.Version < definitions[minHigherVersionDefinition].Identity.Version)
                            {
                                minHigherVersionDefinition = i;
                            }
                        }
                        else
                        {
                            Debug.Assert(reference.Version > definition.Version);

                            // Refers to a newer assembly than we have
                            if (maxLowerVersionDefinition == -1 || definition.Version > definitions[maxLowerVersionDefinition].Identity.Version)
                            {
                                maxLowerVersionDefinition = i;
                            }
                        }

                        continue;

                    default:
                        throw ExceptionUtilities.Unreachable;
                }
            }

            // we haven't found definition that matches the reference

            if (minHigherVersionDefinition != -1)
            {
                return new AssemblyReferenceBinding(reference, minHigherVersionDefinition, versionDifference: +1);
            }

            if (maxLowerVersionDefinition != -1)
            {
                return new AssemblyReferenceBinding(reference, maxLowerVersionDefinition, versionDifference: -1);
            }

            // Handle cases where Windows.winmd is a runtime substitute for a
            // reference to a compile-time winmd. This is for scenarios such as a
            // debugger EE which constructs a compilation from the modules of
            // the running process where Windows.winmd loaded at runtime is a
            // substitute for a collection of Windows.*.winmd compile-time references.
            if (reference.IsWindowsComponent())
            {
                for (int i = definitionStartIndex; i < definitions.Length; i++)
                {
                    if (definitions[i].Identity.IsWindowsRuntime())
                    {
                        return new AssemblyReferenceBinding(reference, i);
                    }
                }
            }

            // In the IDE it is possible the reference we're looking for is a
            // compilation reference to a source assembly. However, if the reference
            // is of ContentType WindowsRuntime then the compilation will never
            // match since all C#/VB WindowsRuntime compilations output .winmdobjs,
            // not .winmds, and the ContentType of a .winmdobj is Default.
            // If this is the case, we want to ignore the ContentType mismatch and
            // allow the compilation to match the reference.
            if (reference.ContentType == AssemblyContentType.WindowsRuntime)
            {
                for (int i = definitionStartIndex; i < definitions.Length; i++)
                {
                    var definition = definitions[i].Identity;
                    var sourceCompilation = definitions[i].SourceCompilation;
                    if (definition.ContentType == AssemblyContentType.Default &&
                        sourceCompilation?.Options.OutputKind == OutputKind.WindowsRuntimeMetadata &&
                        AssemblyIdentityComparer.SimpleNameComparer.Equals(reference.Name, definition.Name) &&
                        reference.Version.Equals(definition.Version) &&
                        reference.IsRetargetable == definition.IsRetargetable &&
                        AssemblyIdentityComparer.CultureComparer.Equals(reference.CultureName, definition.CultureName) &&
                        AssemblyIdentity.KeysEqual(reference, definition))
                    {
                        return new AssemblyReferenceBinding(reference, i);
                    }
                }
            }

            // As in the native compiler (see IMPORTER::MapAssemblyRefToAid), we compare against the
            // compilation (i.e. source) assembly as a last resort.  We follow the native approach of
            // skipping the public key comparison since we have yet to compute it.
            if (resolveAgainstAssemblyBeingBuilt &&
                AssemblyIdentityComparer.SimpleNameComparer.Equals(reference.Name, definitions[0].Identity.Name))
            {
                Debug.Assert(definitions[0].Identity.PublicKeyToken.IsEmpty);
                return new AssemblyReferenceBinding(reference, 0);
            }

            return new AssemblyReferenceBinding(reference);
        }
    }
}
