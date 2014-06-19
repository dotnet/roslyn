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
    /// <summary>
    /// The base class for language specific assembly managers.
    /// </summary>
    /// <typeparam name="TCompilation">Language specific representation for a compilation</typeparam>
    /// <typeparam name="TAssemblySymbol">Language specific representation for an assembly symbol.</typeparam>
    /// <typeparam name="TModuleSymbol">Language specific representation for a module symbol</typeparam>
    internal abstract partial class CommonAssemblyManager<TCompilation, TAssemblySymbol, TModuleSymbol>
        where TCompilation : CommonCompilation
        where TAssemblySymbol : class, IAssemblySymbol
        where TModuleSymbol : class, IModuleSymbol
    {
        protected abstract CommonMessageProvider MessageProvider { get; }

        protected abstract AssemblyData CreateAssemblyDataForFile(
            Assembly assembly,
            WeakList<IAssemblySymbol> cachedSymbols,
            DocumentationProvider documentationProvider,
            string sourceAssemblySimpleName,
            bool alwaysImportInternameMembers,
            bool embedInteropTypes);

        protected abstract AssemblyData CreateAssemblyDataForCompilation(
            CommonCompilationReference compilationReference);

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

        [DebuggerDisplay("{GetDebugDisplay()}")]
        protected struct ResolvedReference
        {
            private readonly MetadataImageKind kind;
            private readonly int index;
            private readonly ReadOnlyArray<string> aliases;

            public static readonly ResolvedReference Skipped = default(ResolvedReference);

            public ResolvedReference(int index, MetadataImageKind kind, ReadOnlyArray<string> aliases)
            {
                Debug.Assert(index >= 0);

                this.index = index + 1;
                this.kind = kind;
                this.aliases = aliases;
            }

            public ReadOnlyArray<string> Aliases
            {
                get
                {
                    Debug.Assert(aliases.IsNotNull);
                    return aliases;
                }
            }

            public bool IsSkipped
            {
                get
                {
                    return index == 0;
                }
            }

            public MetadataImageKind Kind
            {
                get
                {
                    Debug.Assert(!IsSkipped);
                    return kind;
                }
            }

            public int Index
            {
                get
                {
                    Debug.Assert(!IsSkipped);
                    return index - 1;
                }
            }

            private string GetDebugDisplay()
            {
                return IsSkipped ? "<skipped>" : (kind == MetadataImageKind.Assembly ? "A[" : "M[") + Index + "]: aliases=" + aliases.ToString();
            }
        }

        private struct ReferencedAssemblyIdentity
        {
            public readonly AssemblyIdentity Identity;
            public readonly MetadataReference MetadataReference;

            public ReferencedAssemblyIdentity(AssemblyIdentity identity, MetadataReference metadataReference)
            {
                this.Identity = identity;
                this.MetadataReference = metadataReference;
            }
        }

        /// <summary>
        /// Resolves given metadata references to assemblies and modules.
        /// </summary>
        /// <param name="compilation">The compilation whose references are being resolved.</param>
        /// <param name="references">List where to store resolved references. References from #r directives will follow references passed to the compilation constructor.</param>
        /// <param name="boundReferenceDirectiveMap">Maps #r values to successuflly resolved metadata references. Does not contain values that failed to resolve.</param>
        /// <param name="boundReferenceDirectives">Unique metadata references resolved from #r directives.</param>
        /// <param name="assemblies">List where to store information about resolved assemblies to.</param>
        /// <param name="modules">List where to store information about resolved modules to.</param>
        /// <param name="diagnostics">Diagnostic bag where to report resolution errors.</param>
        /// <returns>
        /// Maps index to <paramref name="references"/> to an index of a resolved assembly or module in <paramref name="assemblies"/> or <paramref name="modules"/>, respectively.
        ///</returns>
        protected ReadOnlyArray<ResolvedReference> ResolveMetadataReferences(
            TCompilation compilation,
            List<MetadataReference> references,
            out IDictionary<string, MetadataReference> boundReferenceDirectiveMap,
            out ReadOnlyArray<MetadataReference> boundReferenceDirectives,
            List<AssemblyData> assemblies,
            List<Module> modules,
            DiagnosticBag diagnostics)
        {
            // Locations of all #r directives in the order they are listed in the references list.
            List<CommonLocation> referenceDirectiveLocations;
            GetCompilationReferences(compilation, diagnostics, references, out boundReferenceDirectiveMap, out referenceDirectiveLocations);

            int externalReferenceCount = compilation.ExternalReferences.Count;
            int referenceCount = references.Count;

            // References originating from #r directives precede references supplied as arguments of the compilation.
            int referenceDirectiveCount = referenceCount - externalReferenceCount;
            Debug.Assert((referenceDirectiveLocations != null ? referenceDirectiveLocations.Count : 0) == referenceDirectiveCount);

            var referenceMap = new ResolvedReference[referenceCount];

            // Maps references that were added to the reference set (i.e. not filtered out as duplicates) to a set of names that 
            // can be used to alias these references. Duplicate assemblies contribute their aliases into this set.
            Dictionary<MetadataReference, ArrayBuilder<string>> aliasMap = null;

            // Used to filter out duplicate references that reference the same file (resolve to the same full normalized path).
            var boundReferences = new Dictionary<MetadataReference, MetadataReference>(MetadataReferenceEqualityComparer.Instance);

            // Used to filter out assemblies that have the same strong or weak identity.
            // Maps simple name to a list of full names.
            Dictionary<string, List<ReferencedAssemblyIdentity>> assemblyReferencesBySimpleName = null;

            ArrayBuilder<MetadataReference> uniqueDirectiveReferences = (referenceDirectiveLocations != null) ? ArrayBuilder<MetadataReference>.GetInstance() : null;

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
                    if (CheckPropertiesConsistency(boundReference, existingReference, diagnostics))
                    {
                        AddAlias(existingReference, boundReference.Properties.Alias, ref aliasMap);
                    }

                    continue;
                }

                boundReferences.Add(boundReference, boundReference);

                CommonLocation location;
                if (referenceIndex < referenceDirectiveCount)
                {
                    location = referenceDirectiveLocations[referenceIndex];
                    uniqueDirectiveReferences.Add(boundReference);
                }
                else
                {
                    location = MessageProvider.NoLocation;
                }

                // compilation reference

                var compilationReference = boundReference as CommonCompilationReference;
                if (compilationReference != null)
                {
                    switch (compilationReference.Properties.Kind)
                    {
                        case MetadataImageKind.Assembly:
                            existingReference = TryAddAssembly(compilationReference.Compilation.Assembly.Identity, boundReference, diagnostics, location, ref assemblyReferencesBySimpleName);
                            if (existingReference != null)
                            {
                                AddAlias(existingReference, boundReference.Properties.Alias, ref aliasMap);
                                continue;
                            }

                            // Note, if SourceAssemblySymbol hasn't been created for 
                            // compilationAssembly.Compilation yet, we want this to happen 
                            // right now. Conveniently, this constructor will trigger creation of the 
                            // SourceAssemblySymbol.
                            var asmData = CreateAssemblyDataForCompilation(compilationReference);
                            AddAssembly(asmData, referenceIndex, assemblies, referenceMap);
                            break;

                        default:
                            throw Contract.Unreachable;
                    }

                    continue;
                }

                // PE reference

                var peReference = (PortableExecutableReference)boundReference;
                Metadata metadata = peReference.GetMetadata(MessageProvider, location, diagnostics);
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
                                Assembly assembly = assemblyMetadata.Assembly;
                                existingReference = TryAddAssembly(assembly.Identity, peReference, diagnostics, location, ref assemblyReferencesBySimpleName);
                                if (existingReference != null)
                                {
                                    AddAlias(existingReference, boundReference.Properties.Alias, ref aliasMap);
                                    continue;
                                }

                                var asmData = CreateAssemblyDataForFile(
                                    assembly,
                                    cachedSymbols,
                                    peReference.GetDocumentationProvider(),
                                    SimpleAssemblyName,
                                    compilation.Options.AlwaysImportInternalMembers,
                                    peReference.Properties.EmbedInteropTypes);

                                AddAssembly(asmData, referenceIndex, assemblies, referenceMap);
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
                            if (moduleMetadata.IsValidModule())
                            {
                                AddModule(moduleMetadata.Module, referenceIndex, modules, referenceMap);
                            }
                            else
                            {
                                diagnostics.Add(MessageProvider.CreateDiagnostic(MessageProvider.ERR_MetadataFileNotModule, location, peReference.Display));
                            }
                            break;

                        default:
                            throw Contract.Unreachable;
                    }
                }
            }

            if (uniqueDirectiveReferences != null)
            {
                uniqueDirectiveReferences.Reverse();
                boundReferenceDirectives = uniqueDirectiveReferences.ToReadOnlyAndFree();
            }
            else
            {
                boundReferenceDirectives = ReadOnlyArray<MetadataReference>.Empty;
            }

            for (int i = 0; i < referenceMap.Length; i++)
            {
                if (!referenceMap[i].IsSkipped)
                {
                    int reversedIndex = (referenceMap[i].Kind == MetadataImageKind.Assembly ? assemblies.Count : modules.Count) - 1 - referenceMap[i].Index;
                    referenceMap[i] = new ResolvedReference(reversedIndex, referenceMap[i].Kind, GetAliases(references[i], aliasMap));
                }
            }

            assemblies.Reverse();
            modules.Reverse();
            return referenceMap.AsReadOnlyWrap();
        }

        private static ReadOnlyArray<string> GetAliases(MetadataReference reference, Dictionary<MetadataReference, ArrayBuilder<string>> aliasMap)
        {
            ArrayBuilder<string> aliases;
            if (aliasMap != null && aliasMap.TryGetValue(reference, out aliases))
            {
                return aliases.ToReadOnlyAndFree();
            }

            if (reference.Properties.Alias != null)
            {
                return ReadOnlyArray.Singleton(reference.Properties.Alias);
            }

            return ReadOnlyArray<string>.Empty;
        }

        /// <summary>
        /// Decides whether 2 references are interchangeable when used in the same compilation.
        /// PE references are interchangeable if they have the same non-null full path, compilation references if they refer to the same compilation.
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

                var px = x as PortableExecutableReference;
                if (px != null)
                {
                    var py = y as PortableExecutableReference;
                    if (py == null)
                    {
                        return false;
                    }

                    return px.FullPath != null && py.FullPath != null && StringComparer.OrdinalIgnoreCase.Equals(px.FullPath, py.FullPath);
                }

                var cy = y as CommonCompilationReference;
                return cy != null && ((CommonCompilationReference)x).Compilation == cy.Compilation;
            }

            public int GetHashCode(MetadataReference reference)
            {
                var compilationReference = reference as CommonCompilationReference;
                if (compilationReference != null)
                {
                    return compilationReference.Compilation.GetHashCode();
                }

                var peReference = (PortableExecutableReference)reference;
                if (peReference.FullPath != null)
                {
                    return StringComparer.OrdinalIgnoreCase.GetHashCode(peReference.FullPath);
                }

                return RuntimeHelpers.GetHashCode(reference);
            }
        }

        private static void AddAlias(MetadataReference primaryReference, string newAlias, ref Dictionary<MetadataReference, ArrayBuilder<string>> aliasMap)
        {
            if (aliasMap == null)
            {
                aliasMap = new Dictionary<MetadataReference, ArrayBuilder<string>>();
            }

            ArrayBuilder<string> aliases;
            if (!aliasMap.TryGetValue(primaryReference, out aliases))
            {
                aliases = ArrayBuilder<string>.GetInstance();
                aliasMap.Add(primaryReference, aliases);

                // even if the alias is null we want to add it since C# then allows to use symbols from the assembly unqualified:
                aliases.Add(primaryReference.Properties.Alias);
            }

            // we could avoid duplicates but there is no need to do so:
            aliases.Add(newAlias);
        }

        private static void AddAssembly(AssemblyData data, int referenceIndex, List<AssemblyData> assemblies, ResolvedReference[] referenceMap)
        {
            referenceMap[referenceIndex] = new ResolvedReference(assemblies.Count, MetadataImageKind.Assembly, ReadOnlyArray<string>.Null);
            assemblies.Add(data);
        }

        private static void AddModule(Module module, int referenceIndex, List<Module> modules, ResolvedReference[] referenceMap)
        {
            referenceMap[referenceIndex] = new ResolvedReference(modules.Count, MetadataImageKind.Module, ReadOnlyArray<string>.Null);
            modules.Add(module);
        }

        // Returns null if an assembly of an equivalent identity has not been added previously, otherwise returns the reference that added it.
        // - Both assembly names are strong (have keys) and are either equal or FX unified 
        // - Both assembly names are weak (no keys) and have the same simple name.
        private MetadataReference TryAddAssembly(AssemblyIdentity identity, MetadataReference boundReference, DiagnosticBag diagnostics, CommonLocation location,
            ref Dictionary<string, List<ReferencedAssemblyIdentity>> referencesBySimpleName)
        {
            if (referencesBySimpleName == null)
            {
                referencesBySimpleName = new Dictionary<string, List<ReferencedAssemblyIdentity>>(StringComparer.OrdinalIgnoreCase);
            }

            List<ReferencedAssemblyIdentity> sameSimpleNameIdentities;
            string simpleName = identity.Name;
            if (!referencesBySimpleName.TryGetValue(simpleName, out sameSimpleNameIdentities))
            {
                referencesBySimpleName.Add(simpleName, new List<ReferencedAssemblyIdentity> { new ReferencedAssemblyIdentity(identity, boundReference) });
                return null;
            }

            ReferencedAssemblyIdentity equivalent = default(ReferencedAssemblyIdentity);
            if (identity.IsStrongName)
            {
                foreach (var other in sameSimpleNameIdentities)
                {
                    // only compare strong with strong (weak is never equivalent to strong and vice versa)
                    if (other.Identity.IsStrongName && identity.IsEquivalent(other.Identity))
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
                sameSimpleNameIdentities.Add(new ReferencedAssemblyIdentity(identity, boundReference));
                return null;
            }

            // equivalent found - ignore and/or report an error:

            if (identity.IsStrongName)
            {
                Debug.Assert(equivalent.Identity.IsStrongName);

                // versions migth have been unified for a Framework assembly:
                if (identity != equivalent.Identity)
                {
                    // Dev11 C# reports an error
                    // Dev11 VB keeps both references in the compilation and reports an ambiguity error when a symbol is used.
                    // BREAKING CHANGE in VB: we report an error for both languages

                    // Multiple assemblies with equivalent identity have been imported: '{0}' and '{1}'. Remove one of the duplicate references.
                    MessageProvider.ReportDuplicateMetadataReferenceStrong(diagnostics, location, boundReference, identity, equivalent.MetadataReference, equivalent.Identity);
                }

                // If the versions match exactly we ignore duplicates w/o reporting errors while 
                // Dev11 C# reports:
                //   error CS1703: An assembly with the same identity '{0}' has already been imported. Try removing one of the duplicate references.
                // Dev11 VB reports:
                //   Fatal error BC2000 : compiler initialization failed unexpectedly: Project already has a reference to assembly System. 
                //   A second reference to 'D:\Temp\System.dll' cannot be added.
            }
            else
            {
                MessageProvider.ReportDuplicateMetadataReferenceWeak(diagnostics, location, boundReference, identity, equivalent.MetadataReference, equivalent.Identity);
            }

            Debug.Assert(equivalent.MetadataReference != null);
            return equivalent.MetadataReference;
        }

        protected void GetCompilationReferences(
            TCompilation compilation,
            DiagnosticBag diagnostics,
            List<MetadataReference> references,
            out IDictionary<string, MetadataReference> boundReferenceDirectives,
            out List<CommonLocation> referenceDirectiveLocations)
        {
            referenceDirectiveLocations = null;
            boundReferenceDirectives = null;

            foreach (var referenceDirective in compilation.ReferenceDirectives)
            {
                // we already successfully bound #r with the same value:
                if (boundReferenceDirectives != null && boundReferenceDirectives.ContainsKey(referenceDirective.File))
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
                    referenceDirectiveLocations = new List<CommonLocation>();
                    boundReferenceDirectives = new Dictionary<string, MetadataReference>();
                }

                references.Add(boundReference);
                referenceDirectiveLocations.Add(referenceDirective.Location);
                boundReferenceDirectives.Add(referenceDirective.File, boundReference);
            }

            if (boundReferenceDirectives == null)
            {
                // no directive references resolved successfully:
                boundReferenceDirectives = SpecializedCollections.EmptyDictionary<string, MetadataReference>();
            }

            // add external reference at the end, so that they are processed first:
            references.AddRange(compilation.ExternalReferences);
        }

        /// <summary>
        /// For each given directive return a bound PE reference, or null if the binding fails.
        /// </summary>
        private PortableExecutableReference ResolveReferenceDirective(string reference, CommonLocation location, TCompilation compilation)
        {
            var tree = location.SourceTree;
            string basePath = (tree != null && tree.FilePath.Length > 0) ? tree.FilePath : null;

            string fullPath = compilation.FileResolver.ResolveMetadataReference(reference, basePath);
            if (fullPath == null)
            {
                return null;
            }

            return compilation.MetadataFileProvider.GetReference(fullPath);
        }

        internal static AssemblyReferenceBinding[] ResolveReferencedAssemblies(ReadOnlyArray<AssemblyIdentity> references, AssemblyData[] definitions)
        {
            var boundReferences = new AssemblyReferenceBinding[references.Count];
            for (int j = 0; j < references.Count; j++)
            {
                boundReferences[j] = ResolveReferencedAssembly(references[j], definitions);
            }

            return boundReferences;
        }

        /// <summary>
        /// Used to match AssemblyRef with AssemblyDef.
        /// </summary>
        /// <param name="definitions">Array of definition identities to match against.</param>
        /// <param name="reference">Reference identity to resolve.</param>
        /// <returns>
        /// Returns an index the reference is bound.
        /// </returns>
        internal static AssemblyReferenceBinding ResolveReferencedAssembly(AssemblyIdentity reference, AssemblyData[] definitions)
        {
            // Dev11 C# compiler allows the versions to not match exactly, assuming that a newer library may be used instead of an older version.
            // For a given reference it finds a definition with the lowest version that is higher then or equal to the reference version.
            // If match.Version != reference.Version a warning is reported.

            // definition with the lowest version higher than reference version, unless exact version found
            int minHigherVersionDefinition = -1;
            int maxLowerVersionDefinition = -1;

            for (int i = 0; i < definitions.Length; i++)
            {
                AssemblyIdentity definition = definitions[i].Identity;

                switch (reference.MatchDefinition(definition))
                {
                    case AssemblyIdentity.MatchResult.NotEquivalent:
                        continue;

                    case AssemblyIdentity.MatchResult.Equivalent:
                    case AssemblyIdentity.MatchResult.EquivalentAfterFrameworkUnification:
                        return new AssemblyReferenceBinding(reference, i);

                    case AssemblyIdentity.MatchResult.EquivalentIgnoringVersion:
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

            return new AssemblyReferenceBinding(reference);
        }
    }
}
