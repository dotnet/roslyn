// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
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
        /// 
        /// build executable for the assembly being built. 
        /// 
        /// </summary>
        /// 
        /// <param name="assemblies">
        /// The set of AssemblyData objects describing assemblies, for which this method should
        /// resolve references and find suitable AssemblySymbols. This array is not modified by the
        /// method.
        /// </param>
        /// <param name="hasCircularReference">
        /// True if the assembly being compiled is indirectly referenced through some of its own references.
        /// </param>
        /// <param name="corLibraryIndex">
        /// The definition index of the COR library.
        /// </param>
        /// <returns>
        /// An array of Binding structures describing the result. It has the same amount of items as
        /// the input assemblies array, Binding structure for each input AssemblyData object resides
        /// at the same position.
        /// 
        /// Each Binding structure contains the following data:
        /// 
        /// -    Suitable AssemblySymbol instance for the corresponding assembly, 
        ///     null reference if none is available/found. Always null for the first element, which corresponds to the assembly being built.
        ///
        /// -    Result of resolving assembly references of the corresponding assembly 
        ///     against provided set of assembly definitions. Essentially, this is an array returned by
        ///     AssemblyData.BindAssemblyReferences method.
        /// </returns>
        internal BoundInputAssembly[] Bind(
            ImmutableArray<AssemblyData> assemblies,
            out bool hasCircularReference,
            out int corLibraryIndex)
        {
            Debug.Assert(!assemblies.IsDefault);

            int totalAssemblies = assemblies.Length;

            // This is the array where we store the result in.
            BoundInputAssembly[] boundInputs = new BoundInputAssembly[totalAssemblies];

            // Based on assembly identity, for each assembly, 
            // bind its references against the other assemblies we have.
            for (int i = 0; i < totalAssemblies; i++)
            {
                boundInputs[i].ReferenceBinding = assemblies[i].BindAssemblyReferences(assemblies, this.IdentityComparer);
            }

            // All assembly symbols should be uninitialized at this point:
            Debug.Assert(Array.TrueForAll(boundInputs, bi => bi.AssemblySymbol == null));

            hasCircularReference = CheckCircularReference(boundInputs);

            corLibraryIndex = IndexOfCorLibrary(assemblies);

            // For each assembly, locate AssemblySymbol with similar reference resolution
            // What does similar mean?
            // Similar means: 
            // 1) The same references are resolved against the assemblies that we are given.
            // 2) The same assembly is used as the COR library.

            TAssemblySymbol[] candidateInputAssemblySymbols = new TAssemblySymbol[totalAssemblies];

            // If any assembly from assemblies array refers back to assemblyBeingBuilt,
            // we know that we cannot reuse symbols for any assemblies containing NoPia
            // local types. Because we cannot reuse symbols for assembly referring back
            // to assemblyBeingBuilt.
            if (!hasCircularReference)
            {
                // Deal with assemblies containing NoPia local types.
                if (ReuseAssemblySymbolsWithNoPiaLocalTypes(boundInputs, candidateInputAssemblySymbols, assemblies, corLibraryIndex))
                {
                    return boundInputs;
                }
            }

            // NoPia shortcut either didn't apply or failed, go through general process 
            // of matching candidates.

            ReuseAssemblySymbols(boundInputs, candidateInputAssemblySymbols, assemblies, corLibraryIndex);

            return boundInputs;
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

        private static bool CheckCircularReference(BoundInputAssembly[] boundInputs)
        {
            for (int i = 1; i < boundInputs.Length; i++)
            {
                foreach (AssemblyReferenceBinding index in boundInputs[i].ReferenceBinding)
                {
                    if (index.BoundToAssemblyBeingBuilt)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static int IndexOfCorLibrary(ImmutableArray<AssemblyData> assemblies)
        {
            // Figure out COR library for this compilation.
            ArrayBuilder<int> corLibraryCandidates = null;

            for (int i = 1; i < assemblies.Length; i++)
            {
                // The logic about deciding what assembly is a candidate for being a Cor library here and in
                // Microsoft.CodeAnalysis.VisualBasic.CommandLineCompiler.ResolveMetadataReferencesFromArguments
                // should be equivalent.

                // Linked references cannot be used as COR library.
                // References containing NoPia local types also cannot be used as COR library.
                if (!assemblies[i].IsLinked && assemblies[i].AssemblyReferences.Length == 0 &&
                    !assemblies[i].ContainsNoPiaLocalTypes)
                {
                    // We have referenced assembly that doesn't have assembly references,
                    // check if it declares baseless System.Object.

                    if (assemblies[i].DeclaresTheObjectClass)
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
