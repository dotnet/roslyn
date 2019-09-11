// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal partial class CommonReferenceManager<TCompilation, TAssemblySymbol>
    {
        /// <summary>
        /// For the given set of AssemblyData objects, do the following:
        ///    1) Resolve references from each assembly against other assemblies in the set.
        ///    2) Choose suitable AssemblySymbol instance for each AssemblyData object.
        ///
        /// The first element (index==0) of the assemblies array represents the assembly being built.
        /// One can think about the rest of the items in assemblies array as assembly references given to the compiler to
        /// build executable for the assembly being built. 
        /// </summary>
        /// <param name="compilation">Compilation.</param>
        /// <param name="explicitAssemblies">
        /// An array of <see cref="AssemblyData"/> objects describing assemblies, for which this method should
        /// resolve references and find suitable AssemblySymbols. The first slot contains the assembly being built.
        /// </param>
        /// <param name="explicitModules">
        /// An array of <see cref="PEModule"/> objects describing standalone modules referenced by the compilation.
        /// </param>
        /// <param name="explicitReferences">
        /// An array of references passed to the compilation and resolved from #r directives.
        /// May contain references that were skipped during resolution (they don't have a corresponding explicit assembly).
        /// </param>
        /// <param name="explicitReferenceMap">
        /// Maps index to <paramref name="explicitReferences"/> to an index of a resolved assembly or module in <paramref name="explicitAssemblies"/> or modules.
        /// </param>
        /// <param name="resolverOpt">
        /// Reference resolver used to look up missing assemblies.
        /// </param>
        /// <param name="supersedeLowerVersions">
        /// Hide lower versions of dependencies that have multiple versions behind an alias.
        /// </param>
        /// <param name="assemblyReferencesBySimpleName">
        /// Used to filter out assemblies that have the same strong or weak identity.
        /// Maps simple name to a list of identities. The highest version of each name is the first.
        /// </param>
        /// <param name="importOptions">
        /// Import options applied to implicitly resolved references.
        /// </param>
        /// <param name="allAssemblies">
        /// Updated array <paramref name="explicitAssemblies"/> with resolved implicitly referenced assemblies appended.
        /// </param>
        /// <param name="implicitlyResolvedReferences">
        /// Implicitly resolved references.
        /// </param>
        /// <param name="implicitlyResolvedReferenceMap">
        /// Maps indices of implicitly resolved references to the corresponding indices of resolved assemblies in <paramref name="allAssemblies"/> (explicit + implicit).
        /// </param>
        /// <param name="implicitReferenceResolutions">
        /// Map of implicit reference resolutions performed in the preceding script compilation. 
        /// Output contains additional implicit resolutions performed during binding of this script compilation references.
        /// </param>
        /// <param name="resolutionDiagnostics">
        /// Any diagnostics reported while resolving missing assemblies.
        /// </param>
        /// <param name="hasCircularReference">
        /// True if the assembly being compiled is indirectly referenced through some of its own references.
        /// </param>
        /// <param name="corLibraryIndex">
        /// The definition index of the COR library.
        /// </param>
        /// <return>
        /// An array of <see cref="BoundInputAssembly"/> structures describing the result. It has the same amount of items as
        /// the input assemblies array, <see cref="BoundInputAssembly"/> for each input AssemblyData object resides
        /// at the same position.
        /// 
        /// Each <see cref="BoundInputAssembly"/> contains the following data:
        /// 
        /// -    Suitable AssemblySymbol instance for the corresponding assembly, 
        ///     null reference if none is available/found. Always null for the first element, which corresponds to the assembly being built.
        ///
        /// -    Result of resolving assembly references of the corresponding assembly 
        ///     against provided set of assembly definitions. Essentially, this is an array returned by
        ///     <see cref="AssemblyData.BindAssemblyReferences(ImmutableArray{AssemblyData}, AssemblyIdentityComparer)"/> method.
        /// </return>
        protected BoundInputAssembly[] Bind(
            TCompilation compilation,
            ImmutableArray<AssemblyData> explicitAssemblies,
            ImmutableArray<PEModule> explicitModules,
            ImmutableArray<MetadataReference> explicitReferences,
            ImmutableArray<ResolvedReference> explicitReferenceMap,
            MetadataReferenceResolver resolverOpt,
            MetadataImportOptions importOptions,
            bool supersedeLowerVersions,
            [In, Out] Dictionary<string, List<ReferencedAssemblyIdentity>> assemblyReferencesBySimpleName,
            out ImmutableArray<AssemblyData> allAssemblies,
            out ImmutableArray<MetadataReference> implicitlyResolvedReferences,
            out ImmutableArray<ResolvedReference> implicitlyResolvedReferenceMap,
            ref ImmutableDictionary<AssemblyIdentity, PortableExecutableReference> implicitReferenceResolutions,
            [In, Out] DiagnosticBag resolutionDiagnostics,
            out bool hasCircularReference,
            out int corLibraryIndex)
        {
            Debug.Assert(explicitAssemblies[0] is AssemblyDataForAssemblyBeingBuilt);
            Debug.Assert(explicitReferences.Length == explicitReferenceMap.Length);

            var referenceBindings = ArrayBuilder<AssemblyReferenceBinding[]>.GetInstance();
            try
            {
                // Based on assembly identity, for each assembly, 
                // bind its references against the other assemblies we have.
                for (int i = 0; i < explicitAssemblies.Length; i++)
                {
                    referenceBindings.Add(explicitAssemblies[i].BindAssemblyReferences(explicitAssemblies, IdentityComparer));
                }

                if (resolverOpt?.ResolveMissingAssemblies == true)
                {
                    ResolveAndBindMissingAssemblies(
                        compilation,
                        explicitAssemblies,
                        explicitModules,
                        explicitReferences,
                        explicitReferenceMap,
                        resolverOpt,
                        importOptions,
                        supersedeLowerVersions,
                        referenceBindings,
                        assemblyReferencesBySimpleName,
                        out allAssemblies,
                        out implicitlyResolvedReferences,
                        out implicitlyResolvedReferenceMap,
                        ref implicitReferenceResolutions,
                        resolutionDiagnostics);
                }
                else
                {
                    allAssemblies = explicitAssemblies;
                    implicitlyResolvedReferences = ImmutableArray<MetadataReference>.Empty;
                    implicitlyResolvedReferenceMap = ImmutableArray<ResolvedReference>.Empty;
                }

                // implicitly resolved missing assemblies were added to both referenceBindings and assemblies:
                Debug.Assert(referenceBindings.Count == allAssemblies.Length);

                hasCircularReference = CheckCircularReference(referenceBindings);
                corLibraryIndex = IndexOfCorLibrary(explicitAssemblies, assemblyReferencesBySimpleName, supersedeLowerVersions);

                // For each assembly, locate AssemblySymbol with similar reference resolution
                // What does similar mean?
                // Similar means: 
                // 1) The same references are resolved against the assemblies that we are given 
                //   (or were found during implicit assembly resolution).
                // 2) The same assembly is used as the COR library.

                var boundInputs = new BoundInputAssembly[referenceBindings.Count];
                for (int i = 0; i < referenceBindings.Count; i++)
                {
                    boundInputs[i].ReferenceBinding = referenceBindings[i];
                }

                var candidateInputAssemblySymbols = new TAssemblySymbol[allAssemblies.Length];

                // If any assembly from assemblies array refers back to assemblyBeingBuilt,
                // we know that we cannot reuse symbols for any assemblies containing NoPia
                // local types. Because we cannot reuse symbols for assembly referring back
                // to assemblyBeingBuilt.
                if (!hasCircularReference)
                {
                    // Deal with assemblies containing NoPia local types.
                    if (ReuseAssemblySymbolsWithNoPiaLocalTypes(boundInputs, candidateInputAssemblySymbols, allAssemblies, corLibraryIndex))
                    {
                        return boundInputs;
                    }
                }

                // NoPia shortcut either didn't apply or failed, go through general process 
                // of matching candidates.

                ReuseAssemblySymbols(boundInputs, candidateInputAssemblySymbols, allAssemblies, corLibraryIndex);

                return boundInputs;
            }
            finally
            {
                referenceBindings.Free();
            }
        }

        private void ResolveAndBindMissingAssemblies(
            TCompilation compilation,
            ImmutableArray<AssemblyData> explicitAssemblies,
            ImmutableArray<PEModule> explicitModules,
            ImmutableArray<MetadataReference> explicitReferences,
            ImmutableArray<ResolvedReference> explicitReferenceMap,
            MetadataReferenceResolver resolver,
            MetadataImportOptions importOptions,
            bool supersedeLowerVersions,
            [In, Out] ArrayBuilder<AssemblyReferenceBinding[]> referenceBindings,
            [In, Out] Dictionary<string, List<ReferencedAssemblyIdentity>> assemblyReferencesBySimpleName,
            out ImmutableArray<AssemblyData> allAssemblies,
            out ImmutableArray<MetadataReference> metadataReferences,
            out ImmutableArray<ResolvedReference> resolvedReferences,
            ref ImmutableDictionary<AssemblyIdentity, PortableExecutableReference> implicitReferenceResolutions,
            DiagnosticBag resolutionDiagnostics)
        {
            Debug.Assert(explicitAssemblies[0] is AssemblyDataForAssemblyBeingBuilt);
            Debug.Assert(referenceBindings.Count == explicitAssemblies.Length);
            Debug.Assert(explicitReferences.Length == explicitReferenceMap.Length);

            // -1 for assembly being built:
            int totalReferencedAssemblyCount = explicitAssemblies.Length - 1;

            var implicitAssemblies = ArrayBuilder<AssemblyData>.GetInstance();

            // assembly identities whose resolution failed for all attempted requesting references:
            var resolutionFailures = PooledHashSet<AssemblyIdentity>.GetInstance();

            var metadataReferencesBuilder = ArrayBuilder<MetadataReference>.GetInstance();

            Dictionary<MetadataReference, MergedAliases> lazyAliasMap = null;

            // metadata references and corresponding bindings of their references, used to calculate a fixed point:
            var referenceBindingsToProcess = ArrayBuilder<(MetadataReference, ArraySegment<AssemblyReferenceBinding>)>.GetInstance();

            // collect all missing identities, resolve the assemblies and bind their references against explicit definitions:
            GetInitialReferenceBindingsToProcess(explicitModules, explicitReferences, explicitReferenceMap, referenceBindings, totalReferencedAssemblyCount, referenceBindingsToProcess);

            // NB: includes the assembly being built:
            int explicitAssemblyCount = explicitAssemblies.Length;

            try
            {
                while (referenceBindingsToProcess.Count > 0)
                {
                    var (requestingReference, bindings) = referenceBindingsToProcess.Pop();

                    foreach (var binding in bindings)
                    {
                        // only attempt to resolve unbound references (regardless of version difference of the bound ones)
                        if (binding.IsBound)
                        {
                            continue;
                        }

                        if (!TryResolveMissingReference(
                            requestingReference,
                            binding.ReferenceIdentity,
                            ref implicitReferenceResolutions,
                            resolver,
                            resolutionDiagnostics,
                            out AssemblyIdentity resolvedAssemblyIdentity,
                            out AssemblyMetadata resolvedAssemblyMetadata,
                            out PortableExecutableReference resolvedReference))
                        {
                            resolutionFailures.Add(binding.ReferenceIdentity);
                            continue;
                        }

                        // one attempt for resolution succeeded (the attempt is cached in implicitReferenceResolutions, so further attempts won't fail and add it back):
                        resolutionFailures.Remove(binding.ReferenceIdentity);

                        // The resolver may return different version than we asked for, so it may happen that 
                        // it returns the same identity for two different input identities (e.g. if a higher version 
                        // of an assembly is available than what the assemblies reference: "A, v1" -> "A, v3" and "A, v2" -> "A, v3").
                        // If such case occurs merge the properties (aliases) of the resulting references in the same way we do
                        // during initial explicit references resolution.

                        // -1 for assembly being built:
                        int index = explicitAssemblyCount - 1 + metadataReferencesBuilder.Count;

                        var existingReference = TryAddAssembly(resolvedAssemblyIdentity, resolvedReference, index, resolutionDiagnostics, Location.None, assemblyReferencesBySimpleName, supersedeLowerVersions);
                        if (existingReference != null)
                        {
                            MergeReferenceProperties(existingReference, resolvedReference, resolutionDiagnostics, ref lazyAliasMap);
                            continue;
                        }

                        metadataReferencesBuilder.Add(resolvedReference);

                        var data = CreateAssemblyDataForResolvedMissingAssembly(resolvedAssemblyMetadata, resolvedReference, importOptions);
                        implicitAssemblies.Add(data);

                        var referenceBinding = data.BindAssemblyReferences(explicitAssemblies, IdentityComparer);
                        referenceBindings.Add(referenceBinding);
                        referenceBindingsToProcess.Push((resolvedReference, new ArraySegment<AssemblyReferenceBinding>(referenceBinding)));
                    }
                }

                // record failures for resolution in subsequent submissions: 
                foreach (var assemblyIdentity in resolutionFailures)
                {
                    implicitReferenceResolutions = implicitReferenceResolutions.Add(assemblyIdentity, null);
                }

                if (implicitAssemblies.Count == 0)
                {
                    Debug.Assert(lazyAliasMap == null);

                    resolvedReferences = ImmutableArray<ResolvedReference>.Empty;
                    metadataReferences = ImmutableArray<MetadataReference>.Empty;
                    allAssemblies = explicitAssemblies;
                    return;
                }

                // Rebind assembly references that were initially missing. All bindings established above
                // are against explicitly specified references.

                allAssemblies = explicitAssemblies.AddRange(implicitAssemblies);

                for (int bindingsIndex = 0; bindingsIndex < referenceBindings.Count; bindingsIndex++)
                {
                    var referenceBinding = referenceBindings[bindingsIndex];

                    for (int i = 0; i < referenceBinding.Length; i++)
                    {
                        var binding = referenceBinding[i];

                        // We don't rebind references bound to a non-matching version of a reference that was explicitly
                        // specified, even if we have a better version now.
                        if (binding.IsBound)
                        {
                            continue;
                        }

                        // We only need to resolve against implicitly resolved assemblies,
                        // since we already resolved against explicitly specified ones.
                        referenceBinding[i] = ResolveReferencedAssembly(
                            binding.ReferenceIdentity,
                            allAssemblies,
                            explicitAssemblyCount,
                            IdentityComparer);
                    }
                }

                UpdateBindingsOfAssemblyBeingBuilt(referenceBindings, explicitAssemblyCount, implicitAssemblies);

                metadataReferences = metadataReferencesBuilder.ToImmutable();
                resolvedReferences = ToResolvedAssemblyReferences(metadataReferences, lazyAliasMap, explicitAssemblyCount);
            }
            finally
            {
                implicitAssemblies.Free();
                referenceBindingsToProcess.Free();
                metadataReferencesBuilder.Free();
                resolutionFailures.Free();
            }
        }

        private void GetInitialReferenceBindingsToProcess(
            ImmutableArray<PEModule> explicitModules,
            ImmutableArray<MetadataReference> explicitReferences,
            ImmutableArray<ResolvedReference> explicitReferenceMap,
            ArrayBuilder<AssemblyReferenceBinding[]> referenceBindings,
            int totalReferencedAssemblyCount,
            [Out]ArrayBuilder<(MetadataReference, ArraySegment<AssemblyReferenceBinding>)> result)
        {
            Debug.Assert(result.Count == 0);

            // maps module index to explicitReferences index
            var explicitModuleToReferenceMap = CalculateModuleToReferenceMap(explicitModules, explicitReferenceMap);

            // add module bindings of assembly being built:
            var bindingsOfAssemblyBeingBuilt = referenceBindings[0];
            int bindingIndex = totalReferencedAssemblyCount;
            for (int moduleIndex = 0; moduleIndex < explicitModules.Length; moduleIndex++)
            {
                var moduleReference = explicitReferences[explicitModuleToReferenceMap[moduleIndex]];
                var moduleBindingsCount = explicitModules[moduleIndex].ReferencedAssemblies.Length;

                result.Add(
                    (moduleReference,
                     new ArraySegment<AssemblyReferenceBinding>(bindingsOfAssemblyBeingBuilt, bindingIndex, moduleBindingsCount)));

                bindingIndex += moduleBindingsCount;
            }

            Debug.Assert(bindingIndex == bindingsOfAssemblyBeingBuilt.Length);

            // the first binding is for the assembly being built, all its references are bound or added above
            for (int referenceIndex = 0; referenceIndex < explicitReferenceMap.Length; referenceIndex++)
            {
                var explicitReferenceMapping = explicitReferenceMap[referenceIndex];
                if (explicitReferenceMapping.IsSkipped || explicitReferenceMapping.Kind == MetadataImageKind.Module)
                {
                    continue;
                }

                // +1 for the assembly being built
                result.Add(
                    (explicitReferences[referenceIndex],
                     new ArraySegment<AssemblyReferenceBinding>(referenceBindings[explicitReferenceMapping.Index + 1])));
            }

            // we have a reference binding for each module and for each referenced assembly:
            Debug.Assert(result.Count == explicitModules.Length + totalReferencedAssemblyCount);
        }

        private static ImmutableArray<int> CalculateModuleToReferenceMap(ImmutableArray<PEModule> modules, ImmutableArray<ResolvedReference> resolvedReferences)
        {
            if (modules.Length == 0)
            {
                return ImmutableArray<int>.Empty;
            }

            var result = ArrayBuilder<int>.GetInstance(modules.Length);
            result.ZeroInit(modules.Length);

            for (int i = 0; i < resolvedReferences.Length; i++)
            {
                var resolvedReference = resolvedReferences[i];
                if (!resolvedReference.IsSkipped && resolvedReference.Kind == MetadataImageKind.Module)
                {
                    result[resolvedReference.Index] = i;
                }
            }

            return result.ToImmutableAndFree();
        }

        private static ImmutableArray<ResolvedReference> ToResolvedAssemblyReferences(
            ImmutableArray<MetadataReference> references,
            Dictionary<MetadataReference, MergedAliases> propertyMapOpt,
            int explicitAssemblyCount)
        {
            var result = ArrayBuilder<ResolvedReference>.GetInstance(references.Length);
            for (int i = 0; i < references.Length; i++)
            {
                // -1 for assembly being built
                result.Add(GetResolvedReferenceAndFreePropertyMapEntry(references[i], explicitAssemblyCount - 1 + i, MetadataImageKind.Assembly, propertyMapOpt));
            }

            return result.ToImmutableAndFree();
        }

        private static void UpdateBindingsOfAssemblyBeingBuilt(ArrayBuilder<AssemblyReferenceBinding[]> referenceBindings, int explicitAssemblyCount, ArrayBuilder<AssemblyData> implicitAssemblies)
        {
            var referenceBindingsOfAssemblyBeingBuilt = referenceBindings[0];

            // add implicitly resolved assemblies to the bindings of the assembly being built:
            var bindingsOfAssemblyBeingBuilt = ArrayBuilder<AssemblyReferenceBinding>.GetInstance(referenceBindingsOfAssemblyBeingBuilt.Length + implicitAssemblies.Count);

            // add bindings for explicitly specified assemblies (-1 for the assembly being built):
            bindingsOfAssemblyBeingBuilt.AddRange(referenceBindingsOfAssemblyBeingBuilt, explicitAssemblyCount - 1);

            // add bindings for implicitly resolved assemblies:
            for (int i = 0; i < implicitAssemblies.Count; i++)
            {
                bindingsOfAssemblyBeingBuilt.Add(new AssemblyReferenceBinding(implicitAssemblies[i].Identity, explicitAssemblyCount + i));
            }

            // add bindings for assemblies referenced by modules added to the compilation:
            bindingsOfAssemblyBeingBuilt.AddRange(referenceBindingsOfAssemblyBeingBuilt, explicitAssemblyCount - 1, referenceBindingsOfAssemblyBeingBuilt.Length - explicitAssemblyCount + 1);

            referenceBindings[0] = bindingsOfAssemblyBeingBuilt.ToArrayAndFree();
        }

        /// <summary>
        /// Resolve <paramref name="referenceIdentity"/> using a given <paramref name="resolver"/>.
        /// 
        /// We make sure not to query the resolver for the same identity multiple times (across submissions).
        /// Doing so ensures that we don't create multiple assembly symbols within the same chain of script compilations 
        /// for the same implicitly resolved identity. Failure to do so results in cast errors like "can't convert T to T".
        /// 
        /// The method only records successful resolution results by updating <paramref name="implicitReferenceResolutions"/>.
        /// Failures are only recorded after all resolution attempts have been completed.
        /// 
        /// This approach addresses teh following scenario. Consider a script:
        /// <code>
        ///   #r "dir1\a.dll"
        ///   #r "dir2\b.dll"
        /// </code>
        /// where both a.dll and b.dll reference x.dll, which is present only in dir2. Let's assume the resolver first 
        /// attempts to resolve "x" referenced from "dir1\a.dll". The resolver may fail to find the dependency if it only
        /// looks up the directory containing the referencing assembly (dir1). If we recorded and this failure immediately
        /// we would not call the resolver to resolve "x" within the context of "dir2\b.dll" (or any other referencing assembly). 
        /// 
        /// This behavior would ensure consistency and if the types from x.dll do leak thru to the script compilation, but it 
        /// would result in a missing assembly error. By recording the failure after all resolution attempts are complete
        /// we also achieve a consistent behavior but are able to bind the reference to "x.dll". Besides, this approach
        /// also avoids dependency on the order in which we evaluate the assembly references.
        /// </summary>
        private bool TryResolveMissingReference(
            MetadataReference requestingReference,
            AssemblyIdentity referenceIdentity,
            ref ImmutableDictionary<AssemblyIdentity, PortableExecutableReference> implicitReferenceResolutions,
            MetadataReferenceResolver resolver,
            DiagnosticBag resolutionDiagnostics,
            out AssemblyIdentity resolvedAssemblyIdentity,
            out AssemblyMetadata resolvedAssemblyMetadata,
            out PortableExecutableReference resolvedReference)
        {
            resolvedAssemblyIdentity = null;
            resolvedAssemblyMetadata = null;

            // Check if we have resolved a missing assembly of the given identity in a previous submission:
            if (implicitReferenceResolutions.TryGetValue(referenceIdentity, out var previouslyResolvedReference))
            {
                resolvedReference = previouslyResolvedReference;

                if (previouslyResolvedReference == null)
                {
                    return false;
                }

                resolvedAssemblyMetadata = GetAssemblyMetadata(previouslyResolvedReference, resolutionDiagnostics);
                resolvedAssemblyIdentity = referenceIdentity;
                return resolvedAssemblyMetadata != null;
            }

            // Use the resolver to find the missing reference.
            // Note that the resolver may return an assembly of a different identity than requested, e.g. a higher version.
            resolvedReference = resolver.ResolveMissingAssembly(requestingReference, referenceIdentity);
            if (resolvedReference == null)
            {
                return false;
            }

            resolvedAssemblyMetadata = GetAssemblyMetadata(resolvedReference, resolutionDiagnostics);
            if (resolvedAssemblyMetadata == null)
            {
                return false;
            }

            var resolvedAssembly = resolvedAssemblyMetadata.GetAssembly();

            // Allow reference and definition identities to differ in version, but not other properties:
            if (IdentityComparer.Compare(referenceIdentity, resolvedAssembly.Identity) == AssemblyIdentityComparer.ComparisonResult.NotEquivalent)
            {
                return false;
            }

            resolvedAssemblyIdentity = resolvedAssembly.Identity;

            // Check again if we have resolved a missing assembly with the same identity as the one returned by the resolver.
            // If so, use the one we already resolved.
            if (implicitReferenceResolutions.TryGetValue(resolvedAssemblyIdentity, out previouslyResolvedReference))
            {
                if (previouslyResolvedReference == null)
                {
                    return false;
                }

                resolvedAssemblyMetadata = GetAssemblyMetadata(previouslyResolvedReference, resolutionDiagnostics);
                resolvedReference = previouslyResolvedReference;
                return resolvedAssemblyMetadata != null;
            }

            implicitReferenceResolutions = implicitReferenceResolutions.Add(resolvedAssemblyIdentity, resolvedReference);
            return true;
        }

        private AssemblyData CreateAssemblyDataForResolvedMissingAssembly(
            AssemblyMetadata assemblyMetadata,
            PortableExecutableReference peReference,
            MetadataImportOptions importOptions)
        {
            return CreateAssemblyDataForFile(
                assemblyMetadata.GetAssembly(),
                assemblyMetadata.CachedSymbols,
                peReference.DocumentationProvider,
                SimpleAssemblyName,
                importOptions,
                peReference.Properties.EmbedInteropTypes);
        }

        private bool ReuseAssemblySymbolsWithNoPiaLocalTypes(BoundInputAssembly[] boundInputs, TAssemblySymbol[] candidateInputAssemblySymbols, ImmutableArray<AssemblyData> assemblies, int corLibraryIndex)
        {
            int totalAssemblies = assemblies.Length;
            for (int i = 1; i < totalAssemblies; i++)
            {
                if (!assemblies[i].ContainsNoPiaLocalTypes)
                {
                    continue;
                }

                foreach (TAssemblySymbol candidateAssembly in assemblies[i].AvailableSymbols)
                {
                    // Candidate should be referenced the same way (/r or /l) by the compilation, 
                    // which originated the symbols. We need this restriction in order to prevent 
                    // non-interface generic types closed over NoPia local types from crossing 
                    // assembly boundaries.
                    if (IsLinked(candidateAssembly) != assemblies[i].IsLinked)
                    {
                        continue;
                    }

                    ImmutableArray<TAssemblySymbol> resolutionAssemblies = GetNoPiaResolutionAssemblies(candidateAssembly);

                    if (resolutionAssemblies.IsDefault)
                    {
                        continue;
                    }

                    Array.Clear(candidateInputAssemblySymbols, 0, candidateInputAssemblySymbols.Length);

                    // In order to reuse candidateAssembly, we need to make sure that 
                    // 1) all assemblies in resolutionAssemblies are among assemblies represented
                    //    by assemblies array.
                    // 2) From assemblies represented by assemblies array all assemblies, except 
                    //    assemblyBeingBuilt are among resolutionAssemblies.
                    bool match = true;

                    foreach (TAssemblySymbol assembly in resolutionAssemblies)
                    {
                        match = false;

                        for (int j = 1; j < totalAssemblies; j++)
                        {
                            if (assemblies[j].IsMatchingAssembly(assembly) &&
                                IsLinked(assembly) == assemblies[j].IsLinked)
                            {
                                candidateInputAssemblySymbols[j] = assembly;
                                match = true;
                                // We could break out of the loop unless assemblies array
                                // can contain duplicate values. Let's play safe and loop
                                // through all items.
                            }
                        }

                        if (!match)
                        {
                            // Requirement #1 is not met.
                            break;
                        }
                    }

                    if (!match)
                    {
                        continue;
                    }

                    for (int j = 1; j < totalAssemblies; j++)
                    {
                        if (candidateInputAssemblySymbols[j] == null)
                        {
                            // Requirement #2 is not met.
                            match = false;
                            break;
                        }
                        else
                        {
                            // Let's check if different assembly is used as the COR library.
                            // It shouldn't be possible to get in this situation, but let's play safe.
                            if (corLibraryIndex < 0)
                            {
                                // we don't have COR library.
                                if (GetCorLibrary(candidateInputAssemblySymbols[j]) != null)
                                {
                                    // but this assembly has
                                    // I am leaving the Assert here because it will likely indicate a bug somewhere.
                                    Debug.Assert(GetCorLibrary(candidateInputAssemblySymbols[j]) == null);
                                    match = false;
                                    break;
                                }
                            }
                            else
                            {
                                // We can't be compiling corlib and have a corlib reference at the same time:
                                Debug.Assert(corLibraryIndex != 0);

                                // We have COR library, it should match COR library of the candidate.
                                if (!ReferenceEquals(candidateInputAssemblySymbols[corLibraryIndex], GetCorLibrary(candidateInputAssemblySymbols[j])))
                                {
                                    // I am leaving the Assert here because it will likely indicate a bug somewhere.
                                    Debug.Assert(candidateInputAssemblySymbols[corLibraryIndex] == null);
                                    match = false;
                                    break;
                                }
                            }
                        }
                    }

                    if (match)
                    {
                        // We found a match, use it.
                        for (int j = 1; j < totalAssemblies; j++)
                        {
                            Debug.Assert(candidateInputAssemblySymbols[j] != null);
                            boundInputs[j].AssemblySymbol = candidateInputAssemblySymbols[j];
                        }

                        return true;
                    }
                }

                // Prepare candidateMatchingSymbols for next operations.
                Array.Clear(candidateInputAssemblySymbols, 0, candidateInputAssemblySymbols.Length);

                // Why it doesn't make sense to examine other assemblies with local types?
                // Since we couldn't find a suitable match for this assembly,
                // we know that requirement #2 cannot be met for any other assembly
                // containing local types.
                break;
            }

            return false;
        }

        private void ReuseAssemblySymbols(BoundInputAssembly[] boundInputs, TAssemblySymbol[] candidateInputAssemblySymbols, ImmutableArray<AssemblyData> assemblies, int corLibraryIndex)
        {
            // Queue of references we need to examine for consistency
            Queue<AssemblyReferenceCandidate> candidatesToExamine = new Queue<AssemblyReferenceCandidate>();
            int totalAssemblies = assemblies.Length;

            for (int i = 1; i < totalAssemblies; i++)
            {
                // We could have a match already
                if (boundInputs[i].AssemblySymbol != null || assemblies[i].ContainsNoPiaLocalTypes)
                {
                    continue;
                }

                foreach (TAssemblySymbol candidateAssembly in assemblies[i].AvailableSymbols)
                {
                    bool match = true;

                    // We should examine this candidate, all its references that are supposed to 
                    // match one of the given assemblies and do the same for their references, etc. 
                    // The whole set of symbols we get at the end should be consistent with the set 
                    // of assemblies we are given. The whole set of symbols should be accepted or rejected.

                    // The set of symbols is accumulated in candidateInputAssemblySymbols. It is merged into 
                    // boundInputs after consistency is confirmed. 
                    Array.Clear(candidateInputAssemblySymbols, 0, candidateInputAssemblySymbols.Length);

                    // Symbols and index of the corresponding assembly to match against are accumulated in the
                    // candidatesToExamine queue. They are examined one by one. 
                    candidatesToExamine.Clear();

                    // This is a queue of symbols that we are picking up as a result of using
                    // symbols from candidateAssembly
                    candidatesToExamine.Enqueue(new AssemblyReferenceCandidate(i, candidateAssembly));

                    while (match && candidatesToExamine.Count > 0)
                    {
                        AssemblyReferenceCandidate candidate = candidatesToExamine.Dequeue();

                        Debug.Assert(candidate.DefinitionIndex >= 0);

                        int candidateIndex = candidate.DefinitionIndex;

                        // Have we already chosen symbols for the corresponding assembly?
                        Debug.Assert(boundInputs[candidateIndex].AssemblySymbol == null ||
                                              candidateInputAssemblySymbols[candidateIndex] == null);

                        TAssemblySymbol inputAssembly = boundInputs[candidateIndex].AssemblySymbol;
                        if (inputAssembly == null)
                        {
                            inputAssembly = candidateInputAssemblySymbols[candidateIndex];
                        }

                        if (inputAssembly != null)
                        {
                            if (Object.ReferenceEquals(inputAssembly, candidate.AssemblySymbol))
                            {
                                // We already checked this AssemblySymbol, no reason to check it again
                                continue; // Proceed with the next assembly in candidatesToExamine queue.
                            }

                            // We are using different AssemblySymbol for this assembly
                            match = false;
                            break; // Stop processing items from candidatesToExamine queue.
                        }

                        // Candidate should be referenced the same way (/r or /l) by the compilation, 
                        // which originated the symbols. We need this restriction in order to prevent 
                        // non-interface generic types closed over NoPia local types from crossing 
                        // assembly boundaries.
                        if (IsLinked(candidate.AssemblySymbol) != assemblies[candidateIndex].IsLinked)
                        {
                            match = false;
                            break; // Stop processing items from candidatesToExamine queue.
                        }

                        // Add symbols to the set at corresponding index
                        Debug.Assert(candidateInputAssemblySymbols[candidateIndex] == null);
                        candidateInputAssemblySymbols[candidateIndex] = candidate.AssemblySymbol;

                        // Now process references of the candidate.

                        // how we bound the candidate references for this compilation:
                        var candidateReferenceBinding = boundInputs[candidateIndex].ReferenceBinding;

                        // the AssemblySymbols the candidate symbol refers to:
                        TAssemblySymbol[] candidateReferencedSymbols = GetActualBoundReferencesUsedBy(candidate.AssemblySymbol);

                        Debug.Assert(candidateReferenceBinding.Length == candidateReferencedSymbols.Length);
                        int referencesCount = candidateReferencedSymbols.Length;

                        for (int k = 0; k < referencesCount; k++)
                        {
                            // All candidate's references that were /l-ed by the compilation, 
                            // which originated the symbols, must be /l-ed by this compilation and 
                            // other references must be either /r-ed or not referenced. 
                            // We need this restriction in order to prevent non-interface generic types 
                            // closed over NoPia local types from crossing assembly boundaries.

                            // if target reference isn't resolved against given assemblies, 
                            // we cannot accept a candidate that has the reference resolved.
                            if (!candidateReferenceBinding[k].IsBound)
                            {
                                if (candidateReferencedSymbols[k] != null)
                                {
                                    // can't use symbols 

                                    // If we decide do go back to accepting references like this,
                                    // we should still not do this if the reference is a /l-ed assembly.
                                    match = false;
                                    break; // Stop processing references.
                                }

                                continue; // Proceed with the next reference.
                            }

                            // We resolved the reference, candidate must have that reference resolved too.
                            if (candidateReferencedSymbols[k] == null)
                            {
                                // can't use symbols 
                                match = false;
                                break; // Stop processing references.
                            }

                            int definitionIndex = candidateReferenceBinding[k].DefinitionIndex;
                            if (definitionIndex == 0)
                            {
                                // We can't reuse any assembly that refers to the assembly being built.
                                match = false;
                                break;
                            }

                            // Make sure symbols represent the same assembly/binary
                            if (!assemblies[definitionIndex].IsMatchingAssembly(candidateReferencedSymbols[k]))
                            {
                                // Mismatch between versions?
                                match = false;
                                break; // Stop processing references.
                            }

                            if (assemblies[definitionIndex].ContainsNoPiaLocalTypes)
                            {
                                // We already know that we cannot reuse any existing symbols for 
                                // this assembly
                                match = false;
                                break; // Stop processing references.
                            }

                            if (IsLinked(candidateReferencedSymbols[k]) != assemblies[definitionIndex].IsLinked)
                            {
                                // Mismatch between reference kind.
                                match = false;
                                break; // Stop processing references.
                            }

                            // Add this reference to the queue so that we consider it as a candidate too 
                            candidatesToExamine.Enqueue(new AssemblyReferenceCandidate(definitionIndex, candidateReferencedSymbols[k]));
                        }

                        // Check that the COR library used by the candidate assembly symbol is the same as the one use by this compilation.
                        if (match)
                        {
                            TAssemblySymbol candidateCorLibrary = GetCorLibrary(candidate.AssemblySymbol);

                            if (candidateCorLibrary == null)
                            {
                                // If the candidate didn't have a COR library, that is fine as long as we don't have one either.
                                if (corLibraryIndex >= 0)
                                {
                                    match = false;
                                    break; // Stop processing references.
                                }
                            }
                            else
                            {
                                // We can't be compiling corlib and have a corlib reference at the same time:
                                Debug.Assert(corLibraryIndex != 0);

                                Debug.Assert(ReferenceEquals(candidateCorLibrary, GetCorLibrary(candidateCorLibrary)));

                                // Candidate has COR library, we should have one too.
                                if (corLibraryIndex < 0)
                                {
                                    match = false;
                                    break; // Stop processing references.
                                }

                                // Make sure candidate COR library represent the same assembly/binary
                                if (!assemblies[corLibraryIndex].IsMatchingAssembly(candidateCorLibrary))
                                {
                                    // Mismatch between versions?
                                    match = false;
                                    break; // Stop processing references.
                                }

                                Debug.Assert(!assemblies[corLibraryIndex].ContainsNoPiaLocalTypes);
                                Debug.Assert(!assemblies[corLibraryIndex].IsLinked);
                                Debug.Assert(!IsLinked(candidateCorLibrary));

                                // Add the candidate COR library to the queue so that we consider it as a candidate.
                                candidatesToExamine.Enqueue(new AssemblyReferenceCandidate(corLibraryIndex, candidateCorLibrary));
                            }
                        }
                    }

                    if (match)
                    {
                        // Merge the set of symbols into result
                        for (int k = 0; k < totalAssemblies; k++)
                        {
                            if (candidateInputAssemblySymbols[k] != null)
                            {
                                Debug.Assert(boundInputs[k].AssemblySymbol == null);
                                boundInputs[k].AssemblySymbol = candidateInputAssemblySymbols[k];
                            }
                        }

                        // No reason to examine other symbols for this assembly
                        break; // Stop processing assemblies[i].AvailableSymbols
                    }
                }
            }
        }

        private static bool CheckCircularReference(IReadOnlyList<AssemblyReferenceBinding[]> referenceBindings)
        {
            for (int i = 1; i < referenceBindings.Count; i++)
            {
                foreach (AssemblyReferenceBinding index in referenceBindings[i])
                {
                    if (index.BoundToAssemblyBeingBuilt)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsSuperseded(AssemblyIdentity identity, IReadOnlyDictionary<string, List<ReferencedAssemblyIdentity>> assemblyReferencesBySimpleName)
        {
            return assemblyReferencesBySimpleName[identity.Name][0].Identity.Version != identity.Version;
        }

        private static int IndexOfCorLibrary(ImmutableArray<AssemblyData> assemblies, IReadOnlyDictionary<string, List<ReferencedAssemblyIdentity>> assemblyReferencesBySimpleName, bool supersedeLowerVersions)
        {
            // Figure out COR library for this compilation.
            ArrayBuilder<int> corLibraryCandidates = null;

            for (int i = 1; i < assemblies.Length; i++)
            {
                var assembly = assemblies[i];

                // The logic about deciding what assembly is a candidate for being a Cor library here and in
                // Microsoft.CodeAnalysis.VisualBasic.CommandLineCompiler.ResolveMetadataReferencesFromArguments
                // should be equivalent.

                // Linked references cannot be used as COR library.
                // References containing NoPia local types also cannot be used as COR library.
                if (!assembly.IsLinked &&
                    assembly.AssemblyReferences.Length == 0 &&
                    !assembly.ContainsNoPiaLocalTypes &&
                    (!supersedeLowerVersions || !IsSuperseded(assembly.Identity, assemblyReferencesBySimpleName)))
                {
                    // We have referenced assembly that doesn't have assembly references,
                    // check if it declares baseless System.Object.

                    if (assembly.DeclaresTheObjectClass)
                    {
                        if (corLibraryCandidates == null)
                        {
                            corLibraryCandidates = ArrayBuilder<int>.GetInstance();
                        }

                        // This could be the COR library.
                        corLibraryCandidates.Add(i);
                    }
                }
            }

            // If there is an ambiguous match, pretend there is no COR library.
            // TODO: figure out if we need to be able to resolve this ambiguity. 
            if (corLibraryCandidates != null)
            {
                if (corLibraryCandidates.Count == 1)
                {
                    // TODO: need to make sure we error if such assembly declares local type in source.
                    int result = corLibraryCandidates[0];
                    corLibraryCandidates.Free();
                    return result;
                }
                else
                {
                    // TODO: C# seems to pick the first one (but produces warnings when looking up predefined types).
                    // See PredefinedTypes::Init(ErrorHandling*).
                    corLibraryCandidates.Free();
                }
            }

            // If we have assembly being built and no references, 
            // assume the assembly we are building is the COR library.
            if (assemblies.Length == 1 && assemblies[0].AssemblyReferences.Length == 0)
            {
                return 0;
            }

            return -1;
        }

        /// <summary>
        /// Determines if it is possible that <paramref name="assembly"/> gives internals
        /// access to assembly <paramref name="compilationName"/>. It does not make a conclusive
        /// determination of visibility because the compilation's strong name key is not supplied.
        /// </summary>
        static internal bool InternalsMayBeVisibleToAssemblyBeingCompiled(string compilationName, PEAssembly assembly)
        {
            return !assembly.GetInternalsVisibleToPublicKeys(compilationName).IsEmpty();
        }

        /// <summary>
        /// Return AssemblySymbols referenced by the input AssemblySymbol. The AssemblySymbols must correspond 
        /// to the AssemblyNames returned by AssemblyData.AssemblyReferences property. If reference is not 
        /// resolved, null reference should be returned in the corresponding item. 
        /// </summary>
        /// <param name="assemblySymbol"></param>
        /// The target AssemblySymbol instance.
        /// <returns>
        /// An array of AssemblySymbols referenced by the input AssemblySymbol.
        /// Implementers may return cached array, Binder does not mutate it.
        /// </returns>
        protected abstract TAssemblySymbol[] GetActualBoundReferencesUsedBy(TAssemblySymbol assemblySymbol);

        /// <summary>
        /// Return collection of assemblies involved in canonical type resolution of
        /// NoPia local types defined within target assembly. In other words, all 
        /// references used by previous compilation referencing the target assembly.
        /// </summary>
        protected abstract ImmutableArray<TAssemblySymbol> GetNoPiaResolutionAssemblies(TAssemblySymbol candidateAssembly);

        /// <summary>
        /// Assembly is /l-ed by compilation that is using it as a reference.
        /// </summary>
        protected abstract bool IsLinked(TAssemblySymbol candidateAssembly);

        /// <summary>
        /// Get Assembly used as COR library for the candidate.
        /// </summary>
        protected abstract TAssemblySymbol GetCorLibrary(TAssemblySymbol candidateAssembly);
    }
}
