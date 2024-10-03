// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

internal sealed partial class SolutionCompilationState
{
    private sealed partial class RegularCompilationTracker
    {
        /// <summary>
        /// The base type of all <see cref="RegularCompilationTracker"/> states. The state of a <see
        /// cref="RegularCompilationTracker" /> starts at null, and then will progress through the other states until it
        /// finally reaches <see cref="FinalCompilationTrackerState" />.
        /// </summary>
        private abstract class CompilationTrackerState
        {
            /// <summary>
            /// Whether the generated documents in <see cref="GeneratorInfo"/> are frozen and generators should
            /// never be ran again, ever, even if a document is later changed. This is used to ensure that when we
            /// produce a frozen solution for partial semantics, further downstream forking of that solution won't
            /// rerun generators. This is because of two reasons:
            /// <list type="number">
            /// <item>Generally once we've produced a frozen solution with partial semantics, we now want speed rather
            /// than accuracy; a generator running in a later path will still cause issues there.</item>
            /// <item>The frozen solution with partial semantics makes no guarantee that other syntax trees exist or
            /// whether we even have references -- it's pretty likely that running a generator might produce worse results
            /// than what we originally had.</item>
            /// </list>
            /// This also controls if we will generate skeleton references for cross-language P2P references when
            /// creating the compilation for a particular project.  When entirely frozen, we do not want to do this due
            /// to the enormous cost of emitting ref assemblies for cross language cases.
            /// </summary>
            public readonly CreationPolicy CreationPolicy;

            /// <summary>
            /// The best compilation that is available that source generators have not ran on. May be an
            /// in-progress, full declaration, a final compilation.
            /// </summary>
            public abstract Compilation CompilationWithoutGeneratedDocuments { get; }

            public CompilationTrackerGeneratorInfo GeneratorInfo { get; }

            protected CompilationTrackerState(
                CreationPolicy creationPolicy,
                CompilationTrackerGeneratorInfo generatorInfo)
            {
                CreationPolicy = creationPolicy;
                GeneratorInfo = generatorInfo;
            }
        }

        /// <summary>
        /// A state where we are holding onto a previously built compilation, and have a known set of transformations
        /// that could get us to a more final state.
        /// </summary>
        private sealed class InProgressState : CompilationTrackerState
        {
            public readonly Lazy<Compilation> LazyCompilationWithoutGeneratedDocuments;

            /// <summary>
            /// The result of taking the original completed compilation that had generated documents and updating
            /// them by apply the <see cref="TranslationAction" />; this is not a
            /// correct snapshot in that the generators have not been rerun, but may be reusable if the generators
            /// are later found to give the same output.
            /// </summary>
            public readonly CancellableLazy<Compilation?> LazyStaleCompilationWithGeneratedDocuments;

            /// <summary>
            /// The list of changes that have happened since we last computed a compilation. The oldState corresponds to
            /// the state of the project prior to the mutation.
            /// </summary>
            public ImmutableList<TranslationAction> PendingTranslationActions { get; }

            public override Compilation CompilationWithoutGeneratedDocuments => LazyCompilationWithoutGeneratedDocuments.Value;

            public InProgressState(
                CreationPolicy creationPolicy,
                Lazy<Compilation> compilationWithoutGeneratedDocuments,
                CompilationTrackerGeneratorInfo generatorInfo,
                CancellableLazy<Compilation?> staleCompilationWithGeneratedDocuments,
                ImmutableList<TranslationAction> pendingTranslationActions)
                : base(creationPolicy, generatorInfo)
            {
                // Note: Intermediate projects can be empty.
                Contract.ThrowIfTrue(pendingTranslationActions is null);

                LazyCompilationWithoutGeneratedDocuments = compilationWithoutGeneratedDocuments;
                LazyStaleCompilationWithGeneratedDocuments = staleCompilationWithGeneratedDocuments;
                PendingTranslationActions = pendingTranslationActions;

#if DEBUG
                // As a sanity check, we should never see the generated trees inside of the compilation that should
                // not have generated trees.
                foreach (var generatedDocument in generatorInfo.Documents.States.Values)
                {
                    Contract.ThrowIfTrue(this.CompilationWithoutGeneratedDocuments.SyntaxTrees.Contains(generatedDocument.GetSyntaxTree(CancellationToken.None)));
                }
#endif
            }

            public InProgressState(
                CreationPolicy creationPolicy,
                Compilation compilationWithoutGeneratedDocuments,
                CompilationTrackerGeneratorInfo generatorInfo,
                Compilation? staleCompilationWithGeneratedDocuments,
                ImmutableList<TranslationAction> pendingTranslationActions)
                : this(
                      creationPolicy,
                      new Lazy<Compilation>(() => compilationWithoutGeneratedDocuments),
                      generatorInfo,
                      // Extracted as a method call to prevent captures.
                      staleCompilationWithGeneratedDocuments is null ? s_lazyNullCompilation : CreateLazyCompilation(staleCompilationWithGeneratedDocuments),
                      pendingTranslationActions)
            {
            }

            private static CancellableLazy<Compilation?> CreateLazyCompilation(Compilation? staleCompilationWithGeneratedDocuments)
                => new(staleCompilationWithGeneratedDocuments);
        }

        /// <summary>
        /// The final state a compilation tracker reaches. At this point <see
        /// cref="FinalCompilationWithGeneratedDocuments"/> is now available. It is a requirement that any <see
        /// cref="Compilation"/> provided to any clients of the <see cref="SolutionState"/> (for example, through
        /// <see cref="Project.GetCompilationAsync"/> or <see cref="Project.TryGetCompilation"/> must be from a <see
        /// cref="FinalCompilationTrackerState"/>.  This is because <see cref="FinalCompilationTrackerState"/>
        /// stores extra information in it about that compilation that the <see cref="SolutionState"/> can be
        /// queried for (for example: <see cref="Solution.GetOriginatingProject(ISymbol)"/>.  If <see
        /// cref="Compilation"/>s from other <see cref="CompilationTrackerState"/>s are passed out, then these other
        /// APIs will not function correctly.
        /// </summary>
        private sealed class FinalCompilationTrackerState : CompilationTrackerState
        {
            /// <summary>
            /// Specifies whether <see cref="FinalCompilationWithGeneratedDocuments"/> and all compilations it
            /// depends on contain full information or not.
            /// </summary>
            public readonly bool HasSuccessfullyLoaded;

            /// <summary>
            /// Used to determine which project an assembly symbol came from after the fact.
            /// </summary>
            private SingleInitNullable<RootedSymbolSet> _rootedSymbolSet;

            /// <summary>
            /// The final compilation, with all references and source generators run. This is distinct from <see
            /// cref="Compilation"/>, which in the <see cref="FinalCompilationTrackerState"/> case will be the
            /// compilation before any source generators were ran. This ensures that a later invocation of the
            /// source generators consumes <see cref="Compilation"/> which will avoid generators being ran a second
            /// time on a compilation that already contains the output of other generators. If source generators are
            /// not active, this is equal to <see cref="Compilation"/>.
            /// </summary>
            public readonly Compilation FinalCompilationWithGeneratedDocuments;

            /// <summary>
            /// Whether or not this final compilation state *just* generated documents which exactly correspond to the
            /// state of the compilation.  False if the generated documents came from a point in the past, and are being
            /// carried forward until the next time we run generators.
            /// </summary>
            public readonly bool GeneratedDocumentsUpToDate;

            public override Compilation CompilationWithoutGeneratedDocuments { get; }

            private FinalCompilationTrackerState(
                CreationPolicy creationPolicy,
                bool generatedDocumentsUpToDate,
                Compilation finalCompilationWithGeneratedDocuments,
                Compilation compilationWithoutGeneratedDocuments,
                bool hasSuccessfullyLoaded,
                CompilationTrackerGeneratorInfo generatorInfo)
                : base(creationPolicy, generatorInfo)
            {
                Contract.ThrowIfNull(finalCompilationWithGeneratedDocuments);

                this.GeneratedDocumentsUpToDate = generatedDocumentsUpToDate;

                // As a policy, all partial-state projects are said to have incomplete references, since the
                // state has no guarantees.
                this.CompilationWithoutGeneratedDocuments = compilationWithoutGeneratedDocuments;
                HasSuccessfullyLoaded = hasSuccessfullyLoaded;
                FinalCompilationWithGeneratedDocuments = finalCompilationWithGeneratedDocuments;

                if (this.GeneratorInfo.Documents.IsEmpty)
                {
                    // If we have no generated files, the pre-generator compilation and post-generator compilation
                    // should be the exact same instance; that way we're not creating more compilations than
                    // necessary that would be unable to share source symbols.
                    Debug.Assert(object.ReferenceEquals(finalCompilationWithGeneratedDocuments, compilationWithoutGeneratedDocuments));
                }

#if DEBUG
                // As a sanity check, we should never see the generated trees inside of the compilation that should
                // not have generated trees.
                foreach (var generatedDocument in generatorInfo.Documents.States.Values)
                {
                    Contract.ThrowIfTrue(compilationWithoutGeneratedDocuments.SyntaxTrees.Contains(generatedDocument.GetSyntaxTree(CancellationToken.None)));
                }
#endif
            }

            /// <param name="projectId">Not held onto</param>
            /// <param name="metadataReferenceToProjectId">Not held onto</param>
            public static FinalCompilationTrackerState Create(
                CreationPolicy creationPolicy,
                bool generatedDocumentsUpToDate,
                Compilation finalCompilationWithGeneratedDocuments,
                Compilation compilationWithoutGeneratedDocuments,
                bool hasSuccessfullyLoaded,
                CompilationTrackerGeneratorInfo generatorInfo,
                ProjectId projectId,
                Dictionary<MetadataReference, ProjectId>? metadataReferenceToProjectId)
            {
                // Keep track of information about symbols from this Compilation.  This will help support other APIs
                // the solution exposes that allows the user to map back from symbols to project information.

                RecordAssemblySymbols(projectId, finalCompilationWithGeneratedDocuments, metadataReferenceToProjectId);

                return new FinalCompilationTrackerState(
                    creationPolicy,
                    generatedDocumentsUpToDate,
                    finalCompilationWithGeneratedDocuments,
                    compilationWithoutGeneratedDocuments,
                    hasSuccessfullyLoaded,
                    generatorInfo);
            }

            public RootedSymbolSet RootedSymbolSet => _rootedSymbolSet.Initialize(
                static finalCompilationWithGeneratedDocuments => RootedSymbolSet.Create(finalCompilationWithGeneratedDocuments),
                this.FinalCompilationWithGeneratedDocuments);

            public FinalCompilationTrackerState WithCreationPolicy(CreationPolicy creationPolicy)
                => creationPolicy == this.CreationPolicy
                    ? this
                    : new(creationPolicy,
                        GeneratedDocumentsUpToDate,
                        FinalCompilationWithGeneratedDocuments,
                        CompilationWithoutGeneratedDocuments,
                        HasSuccessfullyLoaded,
                        GeneratorInfo);

            private static void RecordAssemblySymbols(ProjectId projectId, Compilation compilation, Dictionary<MetadataReference, ProjectId>? metadataReferenceToProjectId)
            {
                RecordSourceOfAssemblySymbol(compilation.Assembly, projectId);

                if (metadataReferenceToProjectId != null)
                {
                    foreach (var (metadataReference, currentID) in metadataReferenceToProjectId)
                    {
                        var symbol = compilation.GetAssemblyOrModuleSymbol(metadataReference);
                        RecordSourceOfAssemblySymbol(symbol, currentID);
                    }
                }
            }

            private static void RecordSourceOfAssemblySymbol(ISymbol? assemblyOrModuleSymbol, ProjectId projectId)
            {
                // TODO: how would we ever get a null here?
                if (assemblyOrModuleSymbol == null)
                {
                    return;
                }

                Contract.ThrowIfNull(projectId);
                // remember which project is associated with this assembly
                if (!s_assemblyOrModuleSymbolToProjectMap.TryGetValue(assemblyOrModuleSymbol, out var tmp))
                {
                    // use GetValue to avoid race condition exceptions from Add.
                    // the first one to set the value wins.
                    s_assemblyOrModuleSymbolToProjectMap.GetValue(assemblyOrModuleSymbol, _ => projectId);
                }
                else
                {
                    // sanity check: this should always be true, no matter how many times
                    // we attempt to record the association.
                    Debug.Assert(tmp == projectId);
                }
            }
        }
    }
}
